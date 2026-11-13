using RabiGamaliel.Application.Campus;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Elecciones;

namespace RabiGamaliel.Application.Elecciones;

/// <summary>Arma los resultados por contienda y la participación por grado a partir de los conteos.</summary>
internal static class CalculoResultados
{
    public static async Task<ResultadosResponse> CalcularAsync(
        Eleccion eleccion,
        bool incluirConteo,
        IEleccionRepository elecciones,
        IMiembroCampusRepository miembros,
        DateTimeOffset ahora,
        CancellationToken ct)
    {
        var habilitados = await miembros.EstudiantesActivosPorGradoAsync(ct);
        var votantes = await elecciones.VotantesPorGradoAsync(eleccion.Id, ct);
        var grados = habilitados.Select(h => h.Grado).Union(votantes.Keys).Order();
        var porGrado = grados
            .Select(g => new ParticipacionGrado(
                g,
                GradosEscolares.Nombre(g),
                habilitados.FirstOrDefault(h => h.Grado == g)?.Total ?? 0,
                votantes.GetValueOrDefault(g)))
            .ToList();
        var participacion = new ParticipacionResponse(
            porGrado.Sum(p => p.Habilitados), porGrado.Sum(p => p.Votantes), porGrado);

        if (!incluirConteo)
        {
            return new ResultadosResponse(eleccion.Id, eleccion.Nombre, eleccion.Estado(ahora), false, [], participacion);
        }

        var candidatos = await elecciones.CandidatosAsync(eleccion.Id, ct);
        var conteos = await elecciones.ContarVotosAsync(eleccion.Id, ct);

        var contiendas = candidatos.Select(c => (c.Cargo, c.Ambito))
            .Union(conteos.Select(c => (c.Cargo, c.Ambito)))
            .Where(c => eleccion.Incluye(c.Cargo))
            .OrderBy(c => c.Cargo).ThenBy(c => OrdenAmbito(c.Ambito)).ThenBy(c => c.Ambito, StringComparer.Ordinal)
            .Select(c => Contienda(c.Cargo, c.Ambito, candidatos, conteos))
            .ToList();

        return new ResultadosResponse(eleccion.Id, eleccion.Nombre, eleccion.Estado(ahora), true, contiendas, participacion);
    }

    /// <summary>Orden natural de los ámbitos: institución, transición a 3°, 4°, 5°… 10°, 11°.</summary>
    public static int OrdenAmbito(string ambito) =>
        ambito == ReglasCargo.AmbitoInstitucion ? -2
        : ambito == ReglasCargo.AmbitoPrimerosGrados ? -1
        : int.TryParse(ambito.Split('-')[0], out var grado) ? grado
        : int.MaxValue;

    private static ContiendaResultado Contienda(
        CargoEleccion cargo, string ambito, List<Candidato> candidatos, List<ConteoVotos> conteos)
    {
        var votos = conteos.Where(c => c.Cargo == cargo && c.Ambito == ambito).ToList();
        var resultados = candidatos
            .Where(c => c.Cargo == cargo && c.Ambito == ambito)
            .Select(c => new ResultadoCandidato(
                c.Id, c.NombreCompleto, c.Numero, c.FotoUrl, votos.FirstOrDefault(v => v.CandidatoId == c.Id)?.Votos ?? 0))
            .OrderByDescending(r => r.Votos).ThenBy(r => r.Numero)
            .ToList();
        var enBlanco = votos.Where(v => v.CandidatoId is null).Sum(v => v.Votos);
        var total = votos.Sum(v => v.Votos);

        var maximo = resultados.Count > 0 ? resultados[0].Votos : 0;
        var empate = maximo > 0 && resultados.Count(r => r.Votos == maximo) > 1;
        // Si el voto en blanco supera a todos los candidatos, la contienda se repite (no hay ganador).
        var ganaBlanco = enBlanco > 0 && enBlanco > maximo;
        Guid? ganador = maximo > 0 && !empate && !ganaBlanco ? resultados[0].CandidatoId : null;

        return new ContiendaResultado(
            cargo, ReglasCargo.Nombre(cargo), ambito, ReglasCargo.NombreAmbito(ambito),
            resultados, enBlanco, total, ganador, empate, ganaBlanco);
    }
}
