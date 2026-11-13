using RabiGamaliel.Domain.Simulacros;

namespace RabiGamaliel.Application.Simulacros;

/// <summary>Calificación de un intento por área y competencia.</summary>
public static class CalculoSimulacro
{
    public static IReadOnlyList<PuntajeArea> PorArea(IReadOnlyList<PreguntaSimulacro> preguntas, IReadOnlyDictionary<Guid, int> respuestas) =>
    [
        .. preguntas.GroupBy(p => p.Area).OrderBy(g => g.Key).Select(g =>
        {
            var aciertos = g.Count(p => Acerto(p, respuestas));
            return new PuntajeArea(g.Key, aciertos, g.Count(), PuntajeSaber.Porcentaje(aciertos, g.Count()));
        }),
    ];

    public static IReadOnlyList<PuntajeCompetencia> PorCompetencia(IReadOnlyList<PreguntaSimulacro> preguntas, IReadOnlyDictionary<Guid, int> respuestas) =>
    [
        .. preguntas.Where(p => p.Competencia is not null)
            .GroupBy(p => (p.Area, Competencia: p.Competencia!))
            .OrderBy(g => g.Key.Area).ThenBy(g => g.Key.Competencia)
            .Select(g =>
            {
                var aciertos = g.Count(p => Acerto(p, respuestas));
                return new PuntajeCompetencia(g.Key.Area, g.Key.Competencia, aciertos, g.Count(), PuntajeSaber.Porcentaje(aciertos, g.Count()));
            }),
    ];

    public static int? Global(TipoPrueba tipo, IReadOnlyList<PuntajeArea> porArea) =>
        tipo == TipoPrueba.Saber11 ? PuntajeSaber.GlobalSaber11(porArea.ToDictionary(a => a.Area, a => a.Porcentaje)) : null;

    public static bool Acerto(PreguntaSimulacro pregunta, IReadOnlyDictionary<Guid, int> respuestas) =>
        respuestas.TryGetValue(pregunta.Id, out var r) && r == pregunta.Correcta;
}
