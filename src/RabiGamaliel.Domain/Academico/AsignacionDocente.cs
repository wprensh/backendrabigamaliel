using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Academico;

/// <summary>
/// Carga académica: qué docente dicta qué asignatura en qué curso durante un año. Con ella el docente
/// registra notas, toma asistencia y hace anotaciones en el observador de sus estudiantes.
/// </summary>
public sealed class AsignacionDocente : Entity
{
    private AsignacionDocente()
    {
    }

    public int Anio { get; private set; }

    public Guid DocenteId { get; private set; }

    public Guid AsignaturaId { get; private set; }

    public int Grado { get; private set; }

    /// <summary>Grupo del curso; null si el grado tiene un solo grupo sin letra.</summary>
    public string? Grupo { get; private set; }

    /// <summary>Horas semanales; aparece en el boletín.</summary>
    public int? IntensidadHoraria { get; private set; }

    public static AsignacionDocente Crear(int anio, Guid docenteId, Guid asignaturaId, int grado, string? grupo, int? intensidadHoraria) => new()
    {
        Anio = anio,
        DocenteId = docenteId,
        AsignaturaId = asignaturaId,
        Grado = grado,
        Grupo = string.IsNullOrWhiteSpace(grupo) ? null : grupo.Trim().ToUpperInvariant(),
        IntensidadHoraria = intensidadHoraria is > 0 ? intensidadHoraria : null,
    };

    public void CambiarDocente(Guid docenteId, int? intensidadHoraria)
    {
        DocenteId = docenteId;
        IntensidadHoraria = intensidadHoraria is > 0 ? intensidadHoraria : null;
    }

    /// <summary>¿El estudiante de ese grado y grupo está en este curso?</summary>
    public bool Incluye(int? grado, string? grupo) => grado == Grado && (Grupo is null || Grupo == grupo);
}
