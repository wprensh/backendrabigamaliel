using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Elecciones;

namespace RabiGamaliel.Application.Elecciones;

public interface IVotacionService
{
    Task<IReadOnlyList<EleccionCampusResponse>> EleccionesAsync(VotanteActual votante, CancellationToken ct);

    Task<Result<TarjetonResponse>> TarjetonAsync(Guid id, VotanteActual votante, CancellationToken ct);

    Task<Result<Unit>> VotarAsync(Guid id, VotanteActual votante, VotarRequest request, CancellationToken ct);

    Task<Result<ResultadosResponse>> ResultadosAsync(Guid id, VotanteActual votante, CancellationToken ct);
}

/// <summary>
/// Votación desde el campus virtual. Cada estudiante vota una sola vez por elección, con un voto por cargo
/// (o en blanco), y solo en las contiendas que le corresponden: su grado para vocero, su curso para representante.
/// </summary>
public sealed class VotacionService(
    IEleccionRepository repository,
    IMiembroCampusRepository miembros,
    TimeProvider reloj) : IVotacionService
{
    private const int EleccionesVisibles = 10;

    private static Error NoEncontrada(Guid id) =>
        Error.NoEncontrado("Eleccion.NoEncontrada", $"No existe una elección con id '{id}'.");

    private static readonly string[] MarqueCadaCargo = ["Marque una opción en cada cargo del tarjetón."];
    private static readonly string[] CandidatoAjeno = ["El candidato elegido no es de esta contienda."];

    private static readonly Error YaVoto =
        Error.Conflicto("Votacion.YaVoto", "Usted ya votó en esta elección. Cada estudiante vota una sola vez.");

    public async Task<IReadOnlyList<EleccionCampusResponse>> EleccionesAsync(VotanteActual votante, CancellationToken ct)
    {
        var elecciones = (await repository.ListarAsync(ct)).Take(EleccionesVisibles).ToList();
        var votadas = EsEstudiante(votante)
            ? await repository.EleccionesVotadasAsync(votante.Id, [.. elecciones.Select(e => e.Id)], ct)
            : [];
        var ahora = reloj.GetUtcNow();
        return
        [
            .. elecciones.Select(e => new EleccionCampusResponse(
                e.Id, e.Nombre, e.Descripcion, e.Inicio, e.Fin, e.Estado(ahora), e.Cargos,
                EsEstudiante(votante) ? votadas.Contains(e.Id) : null)),
        ];
    }

    public async Task<Result<TarjetonResponse>> TarjetonAsync(Guid id, VotanteActual votante, CancellationToken ct)
    {
        var eleccion = await repository.ObtenerPorIdAsync(id, ct);
        if (eleccion is null)
        {
            return NoEncontrada(id);
        }

        var contiendas = await ContiendasAsync(eleccion, votante, ct);
        var ahora = reloj.GetUtcNow();
        bool? yaVoto = EsEstudiante(votante) ? await repository.YaVotoAsync(id, votante.Id, ct) : null;
        var puedeVotar = yaVoto == false && eleccion.Estado(ahora) == EstadoEleccion.Abierta && contiendas.Count > 0;

        return new TarjetonResponse(
            eleccion.Id, eleccion.Nombre, eleccion.Estado(ahora), eleccion.Inicio, eleccion.Fin, puedeVotar, yaVoto, contiendas);
    }

    public async Task<Result<Unit>> VotarAsync(Guid id, VotanteActual votante, VotarRequest request, CancellationToken ct)
    {
        if (!EsEstudiante(votante))
        {
            return Error.Conflicto("Votacion.SoloEstudiantes", "Solo los estudiantes votan en las elecciones escolares.");
        }

        var eleccion = await repository.ObtenerPorIdAsync(id, ct);
        if (eleccion is null)
        {
            return NoEncontrada(id);
        }

        var estado = eleccion.Estado(reloj.GetUtcNow());
        if (estado != EstadoEleccion.Abierta)
        {
            return Error.Conflicto(
                "Votacion.NoAbierta",
                estado == EstadoEleccion.Programada ? "La votación todavía no ha empezado." : "La votación ya cerró.");
        }

        if (await repository.YaVotoAsync(id, votante.Id, ct))
        {
            return YaVoto;
        }

        var contiendas = await ContiendasAsync(eleccion, votante, ct);
        if (contiendas.Count == 0)
        {
            return Error.Conflicto("Votacion.SinCandidatos", "No hay candidatos inscritos para su grado en esta elección.");
        }

        // Exactamente un voto por cada contienda del estudiante: un candidato de esa contienda o en blanco.
        var votos = request.Votos ?? [];
        var errores = new List<KeyValuePair<string, string[]>>();
        if (votos.Count != contiendas.Count || votos.Select(v => v.Cargo).Distinct().Count() != votos.Count)
        {
            errores.Add(KeyValuePair.Create(nameof(request.Votos), MarqueCadaCargo));
        }

        var emitidos = new List<Voto>(contiendas.Count);
        foreach (var contienda in contiendas)
        {
            var voto = votos.FirstOrDefault(v => v.Cargo == contienda.Cargo);
            if (voto is null)
            {
                continue;
            }

            if (voto.CandidatoId is { } candidatoId && contienda.Candidatos.All(c => c.Id != candidatoId))
            {
                errores.Add(KeyValuePair.Create(contienda.Cargo.ToString(), CandidatoAjeno));
                continue;
            }

            emitidos.Add(Voto.Emitir(eleccion.Id, contienda.Cargo, contienda.Ambito, voto.CandidatoId));
        }

        if (errores.Count > 0)
        {
            return Error.Validacion(errores);
        }

        var participacion = ParticipacionVoto.Registrar(eleccion.Id, votante.Id, votante.Grado!.Value);
        // Los votos se guardan en orden aleatorio, para que su posición tampoco delate a quién pertenecen.
        return await repository.RegistrarVotoAsync(participacion, [.. emitidos.OrderBy(v => v.Id)], ct)
            ? Unit.Value
            : YaVoto;
    }

    public async Task<Result<ResultadosResponse>> ResultadosAsync(Guid id, VotanteActual votante, CancellationToken ct)
    {
        var eleccion = await repository.ObtenerPorIdAsync(id, ct);
        if (eleccion is null)
        {
            return NoEncontrada(id);
        }

        var ahora = reloj.GetUtcNow();
        var cerrada = eleccion.Estado(ahora) == EstadoEleccion.Cerrada;
        if (!cerrada && votante.Rol != RolCampus.Docente)
        {
            return Error.Conflicto("Votacion.ResultadosNoDisponibles", "Los resultados se publican cuando cierre la votación.");
        }

        // Durante la jornada los docentes ven cuántos han votado por grado, pero no el conteo.
        return await CalculoResultados.CalcularAsync(eleccion, incluirConteo: cerrada, repository, miembros, ahora, ct);
    }

    private static bool EsEstudiante(VotanteActual votante) => votante is { Rol: RolCampus.Estudiante, Grado: not null };

    /// <summary>Contiendas con candidatos: las del estudiante, o todas si es docente.</summary>
    private async Task<List<ContiendaTarjeton>> ContiendasAsync(Eleccion eleccion, VotanteActual votante, CancellationToken ct)
    {
        var candidatos = EleccionService.Ordenar(await repository.CandidatosAsync(eleccion.Id, ct));
        return
        [
            .. candidatos
                .Where(c => eleccion.Incluye(c.Cargo))
                .Where(c => !EsEstudiante(votante) || c.Ambito == ReglasCargo.Ambito(c.Cargo, votante.Grado!.Value, votante.Grupo))
                .GroupBy(c => (c.Cargo, c.Ambito))
                .Select(g => new ContiendaTarjeton(
                    g.Key.Cargo, ReglasCargo.Nombre(g.Key.Cargo), g.Key.Ambito, ReglasCargo.NombreAmbito(g.Key.Ambito), [.. g])),
        ];
    }
}
