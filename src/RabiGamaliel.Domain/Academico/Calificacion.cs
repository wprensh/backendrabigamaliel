using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Academico;

/// <summary>Nota de un estudiante en una asignatura durante un periodo (escala de 1.0 a 5.0).</summary>
public sealed class Calificacion : Entity
{
    public const int ObservacionMaxLength = 400;
    public const int RegistradaPorMaxLength = 120;

    private Calificacion()
    {
    }

    public Guid EstudianteId { get; private set; }

    public Guid AsignaturaId { get; private set; }

    public Guid PeriodoId { get; private set; }

    public decimal? Nota { get; private set; }

    /// <summary>Fallas del periodo; si queda vacío, el boletín cuenta las inasistencias registradas.</summary>
    public int? Fallas { get; private set; }

    /// <summary>Fortalezas, dificultades y recomendaciones para el boletín.</summary>
    public string? Observacion { get; private set; }

    public string RegistradaPor { get; private set; } = string.Empty;

    public static Calificacion Crear(Guid estudianteId, Guid asignaturaId, Guid periodoId) => new()
    {
        EstudianteId = estudianteId,
        AsignaturaId = asignaturaId,
        PeriodoId = periodoId,
    };

    public void Registrar(decimal? nota, int? fallas, string? observacion, string registradaPor)
    {
        if (nota is { } n && !EscalaValorativa.EsValida(n))
        {
            throw new ArgumentOutOfRangeException(nameof(nota), $"La nota va de {EscalaValorativa.Minima} a {EscalaValorativa.Maxima}.");
        }

        Nota = nota is { } valor ? Math.Round(valor, 1, MidpointRounding.AwayFromZero) : null;
        Fallas = fallas is >= 0 ? fallas : null;
        Observacion = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim();
        RegistradaPor = registradaPor.Length > RegistradaPorMaxLength ? registradaPor[..RegistradaPorMaxLength] : registradaPor;
    }

    public bool EstaVacia => Nota is null && Fallas is null && Observacion is null;
}

/// <summary>Desempeños de la escala nacional (Decreto 1290 de 2009).</summary>
public enum Desempeno
{
    Bajo = 1,
    Basico,
    Alto,
    Superior,
}

/// <summary>
/// Escala valorativa institucional de 1.0 a 5.0 y su equivalencia con la escala nacional.
/// Los rangos siguen la práctica común en Colombia; si el SIEE del colegio define otros, se cambian aquí.
/// </summary>
public static class EscalaValorativa
{
    public const decimal Minima = 1.0m;
    public const decimal Maxima = 5.0m;
    public const decimal Aprobatoria = 3.0m;
    public const decimal DesdeAlto = 4.0m;
    public const decimal DesdeSuperior = 4.6m;

    public static bool EsValida(decimal nota) => nota is >= Minima and <= Maxima;

    public static Desempeno Desempeno(decimal nota) =>
        nota >= DesdeSuperior ? Academico.Desempeno.Superior
        : nota >= DesdeAlto ? Academico.Desempeno.Alto
        : nota >= Aprobatoria ? Academico.Desempeno.Basico
        : Academico.Desempeno.Bajo;
}
