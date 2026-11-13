using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Academico;

/// <summary>Asignatura del plan de estudios (p. ej. "Matemáticas", del área "Matemáticas").</summary>
public sealed class Asignatura : Entity
{
    public const int NombreMaxLength = 80;
    public const int AreaMaxLength = 80;

    private Asignatura()
    {
    }

    public string Nombre { get; private set; } = string.Empty;

    /// <summary>Área obligatoria a la que pertenece (Ley 115, art. 23), para agrupar el boletín.</summary>
    public string Area { get; private set; } = string.Empty;

    /// <summary>Posición en el boletín.</summary>
    public int Orden { get; private set; }

    public bool Activa { get; private set; } = true;

    public static Asignatura Crear(string nombre, string area, int orden)
    {
        var asignatura = new Asignatura();
        asignatura.Actualizar(nombre, area, orden, activa: true);
        return asignatura;
    }

    public void Actualizar(string nombre, string area, int orden, bool activa)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Nombre = nombre.Trim();
        Area = string.IsNullOrWhiteSpace(area) ? Nombre : area.Trim();
        Orden = orden;
        Activa = activa;
    }
}
