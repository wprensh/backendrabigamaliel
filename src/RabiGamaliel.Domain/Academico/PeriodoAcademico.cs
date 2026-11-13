using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Academico;

/// <summary>
/// Periodo del año lectivo (p. ej. "Primer periodo 2027"). Mientras las notas están abiertas los docentes
/// las registran; al publicarlo, estudiantes y acudientes ven el boletín en el campus.
/// </summary>
public sealed class PeriodoAcademico : Entity
{
    public const int NombreMaxLength = 60;
    public const int NumeroMaximo = 6;

    private PeriodoAcademico()
    {
    }

    public int Anio { get; private set; }

    public int Numero { get; private set; }

    public string Nombre { get; private set; } = string.Empty;

    public DateOnly Inicio { get; private set; }

    public DateOnly Fin { get; private set; }

    /// <summary>Peso del periodo en la nota definitiva del año (los porcentajes del año suman 100).</summary>
    public int Porcentaje { get; private set; }

    /// <summary>Los docentes pueden registrar o corregir notas.</summary>
    public bool NotasAbiertas { get; private set; }

    /// <summary>El boletín es visible para estudiantes y acudientes.</summary>
    public bool Publicado { get; private set; }

    public static PeriodoAcademico Crear(int anio, int numero, string? nombre, DateOnly inicio, DateOnly fin, int porcentaje)
    {
        var periodo = new PeriodoAcademico { Anio = anio, Numero = numero };
        periodo.Actualizar(nombre, inicio, fin, porcentaje, notasAbiertas: true, publicado: false);
        return periodo;
    }

    public void Actualizar(string? nombre, DateOnly inicio, DateOnly fin, int porcentaje, bool notasAbiertas, bool publicado)
    {
        if (fin < inicio)
        {
            throw new ArgumentException("El periodo debe terminar después de empezar.", nameof(fin));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(porcentaje);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(porcentaje, 100);
        Nombre = string.IsNullOrWhiteSpace(nombre) ? NombrePorDefecto(Numero, Anio) : nombre.Trim();
        Inicio = inicio;
        Fin = fin;
        Porcentaje = porcentaje;
        NotasAbiertas = notasAbiertas;
        Publicado = publicado;
    }

    public bool Contiene(DateOnly fecha) => fecha >= Inicio && fecha <= Fin;

    public static string NombrePorDefecto(int numero, int anio) => numero switch
    {
        1 => $"Primer periodo {anio}",
        2 => $"Segundo periodo {anio}",
        3 => $"Tercer periodo {anio}",
        4 => $"Cuarto periodo {anio}",
        _ => $"Periodo {numero} de {anio}",
    };
}
