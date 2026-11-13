using FluentValidation;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Elecciones;

namespace RabiGamaliel.Application.Elecciones;

public interface IEleccionService
{
    Task<IReadOnlyList<EleccionResponse>> ListarAsync(CancellationToken ct);

    Task<Result<EleccionResponse>> ObtenerAsync(Guid id, CancellationToken ct);

    Task<Result<EleccionResponse>> CrearAsync(GuardarEleccionRequest request, CancellationToken ct);

    Task<Result<EleccionResponse>> ActualizarAsync(Guid id, GuardarEleccionRequest request, CancellationToken ct);

    Task<Result<Unit>> EliminarAsync(Guid id, CancellationToken ct);

    Task<Result<IReadOnlyList<CandidatoResponse>>> CandidatosAsync(Guid id, CancellationToken ct);

    Task<Result<IReadOnlyList<AspiranteResponse>>> AspirantesAsync(Guid id, CargoEleccion cargo, string? buscar, CancellationToken ct);

    Task<Result<CandidatoResponse>> CrearCandidatoAsync(Guid id, CrearCandidatoRequest request, CancellationToken ct);

    Task<Result<CandidatoResponse>> ActualizarCandidatoAsync(
        Guid id, Guid candidatoId, ActualizarCandidatoRequest request, CancellationToken ct);

    Task<Result<Unit>> EliminarCandidatoAsync(Guid id, Guid candidatoId, CancellationToken ct);

    Task<Result<ResultadosResponse>> ResultadosAsync(Guid id, CancellationToken ct);
}

/// <summary>Elecciones escolares desde el panel: jornadas, candidatos con su perfil y resultados.</summary>
public sealed class EleccionService(
    IEleccionRepository repository,
    IMiembroCampusRepository miembros,
    IUnitOfWork unitOfWork,
    TimeProvider reloj,
    IValidator<GuardarEleccionRequest> eleccionValidator,
    IValidator<CrearCandidatoRequest> crearCandidatoValidator,
    IValidator<ActualizarCandidatoRequest> actualizarCandidatoValidator) : IEleccionService
{
    private const int MaximoAspirantes = 20;

    private static Error NoEncontrada(Guid id) =>
        Error.NoEncontrado("Eleccion.NoEncontrada", $"No existe una elección con id '{id}'.");

    private static Error CandidatoNoEncontrado(Guid id) =>
        Error.NoEncontrado("Candidato.NoEncontrado", $"No existe un candidato con id '{id}' en esta elección.");

    private static readonly Error ConVotos = Error.Conflicto(
        "Eleccion.ConVotos",
        "La elección ya tiene votos: no se pueden agregar ni quitar candidatos o cargos.");

    public async Task<IReadOnlyList<EleccionResponse>> ListarAsync(CancellationToken ct)
    {
        var lista = await repository.ListarAsync(ct);
        var resumen = (await repository.ResumirAsync([.. lista.Select(e => e.Id)], ct)).ToDictionary(r => r.EleccionId);
        var ahora = reloj.GetUtcNow();
        return
        [
            .. lista.Select(e => EleccionResponse.Desde(
                e, ahora, resumen.GetValueOrDefault(e.Id)?.Candidatos ?? 0, resumen.GetValueOrDefault(e.Id)?.Votantes ?? 0)),
        ];
    }

    public async Task<Result<EleccionResponse>> ObtenerAsync(Guid id, CancellationToken ct)
    {
        var eleccion = await repository.ObtenerPorIdAsync(id, ct);
        return eleccion is null ? NoEncontrada(id) : await ResponderAsync(eleccion, ct);
    }

    public async Task<Result<EleccionResponse>> CrearAsync(GuardarEleccionRequest request, CancellationToken ct)
    {
        if (await eleccionValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var eleccion = Eleccion.Crear(request.Nombre, request.Descripcion, request.Inicio, request.Fin, request.Cargos);
        repository.Agregar(eleccion);
        await unitOfWork.SaveChangesAsync(ct);
        return EleccionResponse.Desde(eleccion, reloj.GetUtcNow(), 0, 0);
    }

    public async Task<Result<EleccionResponse>> ActualizarAsync(Guid id, GuardarEleccionRequest request, CancellationToken ct)
    {
        if (await eleccionValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var eleccion = await repository.ObtenerPorIdAsync(id, ct);
        if (eleccion is null)
        {
            return NoEncontrada(id);
        }

        var cargosNuevos = request.Cargos.Distinct().Order().ToList();
        if (!cargosNuevos.SequenceEqual(eleccion.Cargos))
        {
            if (await repository.TieneVotosAsync(id, ct))
            {
                return ConVotos;
            }

            var quitados = eleccion.Cargos.Except(cargosNuevos).ToHashSet();
            if ((await repository.CandidatosAsync(id, ct)).Any(c => quitados.Contains(c.Cargo)))
            {
                return Error.Conflicto(
                    "Eleccion.CargoConCandidatos",
                    "Quite primero los candidatos de los cargos que va a retirar de la elección.");
            }
        }

        eleccion.Actualizar(request.Nombre, request.Descripcion, request.Inicio, request.Fin);
        eleccion.CambiarCargos(cargosNuevos);
        await unitOfWork.SaveChangesAsync(ct);
        return await ResponderAsync(eleccion, ct);
    }

    public async Task<Result<Unit>> EliminarAsync(Guid id, CancellationToken ct)
    {
        var eleccion = await repository.ObtenerPorIdAsync(id, ct);
        if (eleccion is null)
        {
            return NoEncontrada(id);
        }

        if (await repository.TieneVotosAsync(id, ct))
        {
            return Error.Conflicto("Eleccion.ConVotos", "Una elección con votos no se elimina: queda como registro.");
        }

        await repository.EliminarAsync(eleccion, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Result<IReadOnlyList<CandidatoResponse>>> CandidatosAsync(Guid id, CancellationToken ct)
    {
        if (await repository.ObtenerPorIdAsync(id, ct) is null)
        {
            return NoEncontrada(id);
        }

        return Ordenar(await repository.CandidatosAsync(id, ct)).ToList();
    }

    public async Task<Result<IReadOnlyList<AspiranteResponse>>> AspirantesAsync(
        Guid id, CargoEleccion cargo, string? buscar, CancellationToken ct)
    {
        if (await repository.ObtenerPorIdAsync(id, ct) is null)
        {
            return NoEncontrada(id);
        }

        var grados = Enumerable.Range(GradosEscolares.Transicion, GradosEscolares.Ultimo + 1)
            .Where(g => ReglasCargo.MotivoNoElegible(cargo, g, "A") is null)
            .ToList();
        var inscritos = (await repository.CandidatosAsync(id, ct)).Where(c => c.Cargo == cargo).Select(c => c.EstudianteId).ToHashSet();

        var encontrados = await miembros.BuscarEstudiantesAsync(buscar, grados, MaximoAspirantes + inscritos.Count, ct);
        return encontrados
            .Where(m => !inscritos.Contains(m.Id))
            .Take(MaximoAspirantes)
            .Select(m => new AspiranteResponse(m.Id, m.Documento, m.NombreCompleto, m.Grado!.Value, m.Grupo))
            .ToList();
    }

    public async Task<Result<CandidatoResponse>> CrearCandidatoAsync(Guid id, CrearCandidatoRequest request, CancellationToken ct)
    {
        if (await crearCandidatoValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var eleccion = await repository.ObtenerPorIdAsync(id, ct);
        if (eleccion is null)
        {
            return NoEncontrada(id);
        }

        if (!eleccion.Incluye(request.Cargo))
        {
            return Campo(nameof(request.Cargo), "Ese cargo no hace parte de esta elección.");
        }

        if (await repository.TieneVotosAsync(id, ct))
        {
            return ConVotos;
        }

        var estudiante = await miembros.ObtenerPorIdAsync(request.EstudianteId, ct);
        if (estudiante is not { Rol: RolCampus.Estudiante, Activo: true, Grado: { } grado })
        {
            return Campo(nameof(request.EstudianteId), "Elija un estudiante activo.");
        }

        if (ReglasCargo.MotivoNoElegible(request.Cargo, grado, estudiante.Grupo) is { } motivo)
        {
            return Campo(nameof(request.EstudianteId), motivo);
        }

        var candidatos = await repository.CandidatosAsync(id, ct);
        if (candidatos.Any(c => c.Cargo == request.Cargo && c.EstudianteId == estudiante.Id))
        {
            return Error.Conflicto("Candidato.Repetido", $"{estudiante.NombreCompleto} ya es candidato a este cargo.");
        }

        var ambito = ReglasCargo.Ambito(request.Cargo, grado, estudiante.Grupo);
        var usados = candidatos.Where(c => c.Cargo == request.Cargo && c.Ambito == ambito).Select(c => c.Numero).ToHashSet();
        var numero = request.Numero ?? Enumerable.Range(1, Candidato.NumeroMaximo).FirstOrDefault(n => !usados.Contains(n));
        if (numero == 0 || usados.Contains(numero))
        {
            return Campo(nameof(request.Numero), $"El número {numero} ya lo tiene otro candidato de la misma contienda.");
        }

        var candidato = Candidato.Crear(id, request.Cargo, estudiante.Id, estudiante.NombreCompleto, grado, estudiante.Grupo, numero);
        candidato.ActualizarPerfil(numero, request.Lema, request.Propuestas, request.FotoUrl);
        repository.AgregarCandidato(candidato);
        await unitOfWork.SaveChangesAsync(ct);
        return CandidatoResponse.Desde(candidato);
    }

    public async Task<Result<CandidatoResponse>> ActualizarCandidatoAsync(
        Guid id, Guid candidatoId, ActualizarCandidatoRequest request, CancellationToken ct)
    {
        if (await actualizarCandidatoValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var candidato = await repository.ObtenerCandidatoAsync(id, candidatoId, ct);
        if (candidato is null)
        {
            return CandidatoNoEncontrado(candidatoId);
        }

        if (request.Numero != candidato.Numero)
        {
            // Cambiar el número con votos ya emitidos confundiría a quien ya vio el tarjetón.
            if (await repository.TieneVotosAsync(id, ct))
            {
                return Campo(nameof(request.Numero), "La elección ya tiene votos: el número del tarjetón no cambia.");
            }

            var candidatos = await repository.CandidatosAsync(id, ct);
            if (candidatos.Any(c => c.Id != candidatoId && c.Cargo == candidato.Cargo && c.Ambito == candidato.Ambito && c.Numero == request.Numero))
            {
                return Campo(nameof(request.Numero), $"El número {request.Numero} ya lo tiene otro candidato de la misma contienda.");
            }
        }

        candidato.ActualizarPerfil(request.Numero, request.Lema, request.Propuestas, request.FotoUrl);
        await unitOfWork.SaveChangesAsync(ct);
        return CandidatoResponse.Desde(candidato);
    }

    public async Task<Result<Unit>> EliminarCandidatoAsync(Guid id, Guid candidatoId, CancellationToken ct)
    {
        var candidato = await repository.ObtenerCandidatoAsync(id, candidatoId, ct);
        if (candidato is null)
        {
            return CandidatoNoEncontrado(candidatoId);
        }

        if (await repository.TieneVotosAsync(id, ct))
        {
            return ConVotos;
        }

        repository.EliminarCandidato(candidato);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Result<ResultadosResponse>> ResultadosAsync(Guid id, CancellationToken ct)
    {
        var eleccion = await repository.ObtenerPorIdAsync(id, ct);
        if (eleccion is null)
        {
            return NoEncontrada(id);
        }

        // En el panel el conteo se ve en vivo, también durante la jornada.
        return await CalculoResultados.CalcularAsync(eleccion, incluirConteo: true, repository, miembros, reloj.GetUtcNow(), ct);
    }

    internal static IReadOnlyList<CandidatoResponse> Ordenar(IEnumerable<Candidato> candidatos) =>
    [
        .. candidatos
            .OrderBy(c => c.Cargo)
            .ThenBy(c => CalculoResultados.OrdenAmbito(c.Ambito)).ThenBy(c => c.Ambito, StringComparer.Ordinal)
            .ThenBy(c => c.Numero)
            .Select(CandidatoResponse.Desde),
    ];

    private async Task<EleccionResponse> ResponderAsync(Eleccion eleccion, CancellationToken ct)
    {
        var resumen = (await repository.ResumirAsync([eleccion.Id], ct)).FirstOrDefault();
        return EleccionResponse.Desde(eleccion, reloj.GetUtcNow(), resumen?.Candidatos ?? 0, resumen?.Votantes ?? 0);
    }

    private static Error Campo(string campo, string mensaje) =>
        Error.Validacion(KeyValuePair.Create(campo, new[] { mensaje }));
}
