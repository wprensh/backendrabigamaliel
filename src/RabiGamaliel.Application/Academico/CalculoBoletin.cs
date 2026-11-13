using RabiGamaliel.Domain.Academico;

namespace RabiGamaliel.Application.Academico;

/// <summary>Cálculos del boletín: promedio, nota acumulada ponderada y puesto en el curso.</summary>
public static class CalculoBoletin
{
    public static decimal? Promedio(IEnumerable<decimal?> notas)
    {
        var validas = notas.Where(n => n is not null).Select(n => n!.Value).ToList();
        return validas.Count == 0 ? null : Math.Round(validas.Average(), 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Nota acumulada del año: promedio de los periodos con nota ponderado por su porcentaje. Si ningún periodo tiene
    /// porcentaje, se usa el promedio simple.
    /// </summary>
    public static decimal? Acumulada(IReadOnlyList<(decimal? Nota, int Porcentaje)> periodos)
    {
        var conNota = periodos.Where(p => p.Nota is not null).ToList();
        if (conNota.Count == 0)
        {
            return null;
        }

        var pesos = conNota.Sum(p => p.Porcentaje);
        var valor = pesos == 0
            ? conNota.Average(p => p.Nota!.Value)
            : conNota.Sum(p => p.Nota!.Value * p.Porcentaje) / pesos;
        return Math.Round(valor, 1, MidpointRounding.AwayFromZero);
    }

    /// <summary>Puesto por promedio, de mayor a menor; los empates comparten puesto (1, 2, 2, 4).</summary>
    public static Dictionary<Guid, int> Puestos(IReadOnlyDictionary<Guid, decimal?> promedios)
    {
        var ordenados = promedios.Where(p => p.Value is not null).OrderByDescending(p => p.Value).ToList();
        var puestos = new Dictionary<Guid, int>();
        for (var i = 0; i < ordenados.Count; i++)
        {
            puestos[ordenados[i].Key] = i > 0 && ordenados[i].Value == ordenados[i - 1].Value
                ? puestos[ordenados[i - 1].Key]
                : i + 1;
        }

        return puestos;
    }

    public static bool Perdida(decimal? nota) => nota is { } n && n < EscalaValorativa.Aprobatoria;

    public static Desempeno? Desempeno(decimal? nota) => nota is { } n ? EscalaValorativa.Desempeno(n) : null;
}
