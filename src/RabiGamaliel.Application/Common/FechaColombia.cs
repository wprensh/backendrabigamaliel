namespace RabiGamaliel.Application.Common;

/// <summary>Fecha y año lectivo en la hora de Colombia (UTC-5, sin horario de verano).</summary>
public static class FechaColombia
{
    public static readonly TimeSpan Desfase = TimeSpan.FromHours(-5);

    public static DateTimeOffset Ahora(TimeProvider reloj) => reloj.GetUtcNow().ToOffset(Desfase);

    public static DateOnly Hoy(TimeProvider reloj) => DateOnly.FromDateTime(Ahora(reloj).DateTime);

    /// <summary>Calendario A: el año lectivo coincide con el año calendario.</summary>
    public static int AnioLectivo(TimeProvider reloj) => Hoy(reloj).Year;
}
