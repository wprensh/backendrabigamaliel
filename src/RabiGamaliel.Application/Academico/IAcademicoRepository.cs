using RabiGamaliel.Domain.Academico;

namespace RabiGamaliel.Application.Academico;

/// <summary>Puerto de persistencia del módulo académico.</summary>
public interface IAcademicoRepository
{
    Task<List<Asignatura>> AsignaturasAsync(bool soloActivas, CancellationToken ct);

    Task<Asignatura?> AsignaturaAsync(Guid id, CancellationToken ct);

    Task<bool> AsignaturaEnUsoAsync(Guid id, CancellationToken ct);

    void Agregar(Asignatura asignatura);

    void Eliminar(Asignatura asignatura);

    Task<List<PeriodoAcademico>> PeriodosAsync(int? anio, CancellationToken ct);

    Task<PeriodoAcademico?> PeriodoAsync(Guid id, CancellationToken ct);

    Task<bool> ExistePeriodoAsync(int anio, int numero, Guid? excluirId, CancellationToken ct);

    Task<bool> PeriodoConNotasAsync(Guid id, CancellationToken ct);

    void Agregar(PeriodoAcademico periodo);

    void Eliminar(PeriodoAcademico periodo);

    /// <summary>Carga académica del año. Con grado y grupo trae también las asignaciones de todo el grado (grupo null).</summary>
    Task<List<AsignacionDocente>> AsignacionesAsync(int anio, Guid? docenteId, int? grado, string? grupo, CancellationToken ct);

    Task<AsignacionDocente?> AsignacionAsync(Guid id, CancellationToken ct);

    Task<bool> ExisteAsignacionAsync(int anio, Guid asignaturaId, int grado, string? grupo, CancellationToken ct);

    Task<bool> AsignacionConRegistrosAsync(Guid id, CancellationToken ct);

    void Agregar(AsignacionDocente asignacion);

    void Eliminar(AsignacionDocente asignacion);

    /// <summary>Bloques del horario semanal de esas asignaciones.</summary>
    Task<List<BloqueHorario>> BloquesAsync(IReadOnlyCollection<Guid> asignacionIds, CancellationToken ct);

    Task<BloqueHorario?> BloqueAsync(Guid id, CancellationToken ct);

    void Agregar(BloqueHorario bloque);

    void Eliminar(BloqueHorario bloque);

    Task<List<Calificacion>> CalificacionesAsync(
        IReadOnlyCollection<Guid> estudianteIds, IReadOnlyCollection<Guid> periodoIds, Guid? asignaturaId, CancellationToken ct);

    void Agregar(Calificacion calificacion);

    /// <summary>Inasistencias (sin excusa) registradas por estudiante y asignatura entre dos fechas.</summary>
    Task<List<ConteoFallas>> FallasAsync(
        IReadOnlyCollection<Guid> estudianteIds, DateOnly desde, DateOnly hasta, Guid? asignaturaId, CancellationToken ct);

    /// <summary>Cursos (grado y grupo) con estudiantes activos.</summary>
    Task<List<CursoResponse>> CursosAsync(CancellationToken ct);
}
