using RabiGamaliel.Application.Common;
using RabiGamaliel.Application.Simulacros;
using RabiGamaliel.Domain.Simulacros;
using RabiGamaliel.Infrastructure.Persistence.Seed;

namespace RabiGamaliel.Application.Tests;

public sealed class BancoSaberYRefuerzosTests
{
    [Fact]
    public void El_banco_inicial_cubre_todas_las_areas_con_preguntas_validas()
    {
        var preguntas = BancoSaber.Faltantes(new HashSet<string>()).ToList();

        Assert.Equal(BancoSaber.Total, preguntas.Count);
        foreach (var area in Enum.GetValues<AreaEvaluada>().Where(a => a != AreaEvaluada.Otra))
        {
            Assert.True(preguntas.Count(p => p.Area == area) >= 6, $"Faltan preguntas de {area}");
        }

        Assert.All(preguntas, p =>
        {
            Assert.Equal(4, p.Opciones.Count);
            Assert.InRange(p.Correcta, 0, 3);
            Assert.False(string.IsNullOrWhiteSpace(p.Competencia));
            Assert.False(string.IsNullOrWhiteSpace(p.Explicacion));
        });

        // La respuesta correcta no queda siempre en la misma letra.
        Assert.True(preguntas.Select(p => p.Correcta).Distinct().Count() >= 3);
    }

    [Fact]
    public void Volver_a_sembrar_no_duplica_preguntas()
    {
        var existentes = BancoSaber.Faltantes(new HashSet<string>())
            .Select(p => BancoSaber.Clave(p.Area, p.Contexto, p.Enunciado)).ToHashSet();

        Assert.Empty(BancoSaber.Faltantes(existentes));
    }

    [Fact]
    public void El_simulacro_tiene_20_preguntas_4_por_area_de_Saber_11()
    {
        var banco = BancoSaber.Faltantes(new HashSet<string>()).ToList();
        var elegidas = BancoSaber.DelSimulacro;

        Assert.Equal(20, elegidas.Count);
        Assert.Equal(20, elegidas.Distinct().Count());
        foreach (var area in AreasPrueba.De(TipoPrueba.Saber11))
        {
            Assert.Equal(4, elegidas.Count(e => e.Area == area));
        }

        Assert.All(elegidas, e => Assert.Contains(banco, p => p.Area == e.Area && p.Contexto == e.Contexto && p.Enunciado == e.Enunciado));
    }

    [Fact]
    public void Recomienda_refuerzo_solo_en_las_areas_y_competencias_con_bajo_resultado()
    {
        var simulacroId = Guid.CreateVersion7();
        var preguntas = new List<PreguntaSimulacro>
        {
            Pregunta(simulacroId, 1, AreaEvaluada.LecturaCritica, CompetenciasSaber.LcIdentificar),
            Pregunta(simulacroId, 2, AreaEvaluada.LecturaCritica, CompetenciasSaber.LcReflexionar),
            Pregunta(simulacroId, 3, AreaEvaluada.Matematicas, CompetenciasSaber.MatFormular),
            Pregunta(simulacroId, 4, AreaEvaluada.Matematicas, CompetenciasSaber.MatFormular),
            Pregunta(simulacroId, 5, AreaEvaluada.Ingles, CompetenciasSaber.InGramatica),
            Pregunta(simulacroId, 6, AreaEvaluada.Ingles, CompetenciasSaber.InLectura),
        };

        // Lectura crítica: todo bien. Matemáticas: todo mal. Inglés: 1 de 2 (50 %).
        var respuestas = new Dictionary<Guid, int>
        {
            [preguntas[0].Id] = 0, [preguntas[1].Id] = 0,
            [preguntas[2].Id] = 1, [preguntas[3].Id] = 1,
            [preguntas[4].Id] = 0, [preguntas[5].Id] = 1,
        };

        var recomendaciones = RecomendacionesSimulacro.Calcular(preguntas, respuestas);

        Assert.DoesNotContain(recomendaciones, r => r.Area == AreaEvaluada.LecturaCritica);
        var matematicas = Assert.Single(recomendaciones, r => r.Area == AreaEvaluada.Matematicas);
        Assert.Equal("Alta", matematicas.Prioridad);
        Assert.Equal([3, 4], matematicas.PreguntasFalladas);
        var formular = Assert.Single(matematicas.Competencias);
        Assert.Equal(CompetenciasSaber.MatFormular, formular.Competencia);
        Assert.Contains(formular.Temas, t => t.Contains("Porcentajes"));

        var ingles = Assert.Single(recomendaciones, r => r.Area == AreaEvaluada.Ingles);
        Assert.Equal("Media", ingles.Prioridad);
        Assert.Equal(CompetenciasSaber.InLectura, Assert.Single(ingles.Competencias).Competencia);

        // La de menor puntaje va primero.
        Assert.Equal(AreaEvaluada.Matematicas, recomendaciones[0].Area);
    }

    private static PreguntaSimulacro Pregunta(Guid simulacroId, int orden, AreaEvaluada area, string competencia) =>
        PreguntaSimulacro.Crear(simulacroId, orden, area, competencia, null, $"Pregunta {orden}", null, ["A", "B", "C", "D"], 0, null);
}
