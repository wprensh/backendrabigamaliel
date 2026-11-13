using RabiGamaliel.Application.Academico;
using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Asistencia;
using RabiGamaliel.Domain.Campus;
using Microsoft.EntityFrameworkCore;

namespace RabiGamaliel.Infrastructure.Persistence.Repositories;

internal sealed class AcademicoRepository(ApplicationDbContext db) : IAcademicoRepository
{
    public Task<List<Asignatura>> AsignaturasAsync(bool soloActivas, CancellationToken ct) =>
        db.Asignaturas.Where(a => !soloActivas || a.Activa).OrderBy(a => a.Orden).ThenBy(a => a.Nombre).ToListAsync(ct);

    public Task<Asignatura?> AsignaturaAsync(Guid id, CancellationToken ct) => db.Asignaturas.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<bool> AsignaturaEnUsoAsync(Guid id, CancellationToken ct) =>
        await db.AsignacionesDocentes.AnyAsync(a => a.AsignaturaId == id, ct) || await db.Calificaciones.AnyAsync(c => c.AsignaturaId == id, ct);

    public void Agregar(Asignatura asignatura) => db.Asignaturas.Add(asignatura);

    public void Eliminar(Asignatura asignatura) => db.Asignaturas.Remove(asignatura);

    public Task<List<PeriodoAcademico>> PeriodosAsync(int? anio, CancellationToken ct) =>
        db.PeriodosAcademicos.Where(p => anio == null || p.Anio == anio).OrderByDescending(p => p.Anio).ThenBy(p => p.Numero).ToListAsync(ct);

    public Task<PeriodoAcademico?> PeriodoAsync(Guid id, CancellationToken ct) => db.PeriodosAcademicos.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExistePeriodoAsync(int anio, int numero, Guid? excluirId, CancellationToken ct) =>
        db.PeriodosAcademicos.AnyAsync(p => p.Anio == anio && p.Numero == numero && p.Id != excluirId, ct);

    public Task<bool> PeriodoConNotasAsync(Guid id, CancellationToken ct) => db.Calificaciones.AnyAsync(c => c.PeriodoId == id, ct);

    public void Agregar(PeriodoAcademico periodo) => db.PeriodosAcademicos.Add(periodo);

    public void Eliminar(PeriodoAcademico periodo) => db.PeriodosAcademicos.Remove(periodo);

    public Task<List<AsignacionDocente>> AsignacionesAsync(int anio, Guid? docenteId, int? grado, string? grupo, CancellationToken ct)
    {
        var consulta = db.AsignacionesDocentes.Where(a => a.Anio == anio);
        if (docenteId is { } d)
        {
            consulta = consulta.Where(a => a.DocenteId == d);
        }

        if (grado is { } g)
        {
            consulta = consulta.Where(a => a.Grado == g);
            if (grupo is not null)
            {
                consulta = consulta.Where(a => a.Grupo == null || a.Grupo == grupo);
            }
        }

        return consulta.OrderBy(a => a.Grado).ThenBy(a => a.Grupo).ToListAsync(ct);
    }

    public Task<AsignacionDocente?> AsignacionAsync(Guid id, CancellationToken ct) => db.AsignacionesDocentes.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<bool> ExisteAsignacionAsync(int anio, Guid asignaturaId, int grado, string? grupo, CancellationToken ct) =>
        db.AsignacionesDocentes.AnyAsync(a => a.Anio == anio && a.AsignaturaId == asignaturaId && a.Grado == grado && a.Grupo == grupo, ct);

    public Task<bool> AsignacionConRegistrosAsync(Guid id, CancellationToken ct) => db.TomasAsistencia.AnyAsync(t => t.AsignacionId == id, ct);

    public void Agregar(AsignacionDocente asignacion) => db.AsignacionesDocentes.Add(asignacion);

    public void Eliminar(AsignacionDocente asignacion) => db.AsignacionesDocentes.Remove(asignacion);

    public Task<List<BloqueHorario>> BloquesAsync(IReadOnlyCollection<Guid> asignacionIds, CancellationToken ct) =>
        asignacionIds.Count == 0
            ? Task.FromResult(new List<BloqueHorario>())
            : db.BloquesHorario.Where(b => asignacionIds.Contains(b.AsignacionId)).OrderBy(b => b.Dia).ThenBy(b => b.Inicio).ToListAsync(ct);

    public Task<BloqueHorario?> BloqueAsync(Guid id, CancellationToken ct) => db.BloquesHorario.FirstOrDefaultAsync(b => b.Id == id, ct);

    public void Agregar(BloqueHorario bloque) => db.BloquesHorario.Add(bloque);

    public void Eliminar(BloqueHorario bloque) => db.BloquesHorario.Remove(bloque);

    public Task<List<Calificacion>> CalificacionesAsync(
        IReadOnlyCollection<Guid> estudianteIds, IReadOnlyCollection<Guid> periodoIds, Guid? asignaturaId, CancellationToken ct)
    {
        if (estudianteIds.Count == 0 || periodoIds.Count == 0)
        {
            return Task.FromResult(new List<Calificacion>());
        }

        var consulta = db.Calificaciones.Where(c => estudianteIds.Contains(c.EstudianteId) && periodoIds.Contains(c.PeriodoId));
        if (asignaturaId is { } a)
        {
            consulta = consulta.Where(c => c.AsignaturaId == a);
        }

        return consulta.ToListAsync(ct);
    }

    public void Agregar(Calificacion calificacion) => db.Calificaciones.Add(calificacion);

    public Task<List<ConteoFallas>> FallasAsync(
        IReadOnlyCollection<Guid> estudianteIds, DateOnly desde, DateOnly hasta, Guid? asignaturaId, CancellationToken ct)
    {
        if (estudianteIds.Count == 0)
        {
            return Task.FromResult(new List<ConteoFallas>());
        }

        var consulta = from n in db.NovedadesAsistencia
                       join t in db.TomasAsistencia on n.TomaId equals t.Id
                       where n.Estado == EstadoAsistencia.Ausente && estudianteIds.Contains(n.EstudianteId) && t.Fecha >= desde && t.Fecha <= hasta
                       select new { n.EstudianteId, t.AsignaturaId };
        if (asignaturaId is { } a)
        {
            consulta = consulta.Where(x => x.AsignaturaId == a);
        }

        return consulta.GroupBy(x => new { x.EstudianteId, x.AsignaturaId })
            .Select(g => new ConteoFallas(g.Key.EstudianteId, g.Key.AsignaturaId, g.Count()))
            .ToListAsync(ct);
    }

    public async Task<List<CursoResponse>> CursosAsync(CancellationToken ct)
    {
        var cursos = await db.MiembrosCampus
            .Where(m => m.Activo && m.Rol == RolCampus.Estudiante && m.Grado != null)
            .GroupBy(m => new { Grado = m.Grado!.Value, m.Grupo })
            .Select(g => new { g.Key.Grado, g.Key.Grupo, Total = g.Count() })
            .ToListAsync(ct);
        return [.. cursos.OrderBy(c => c.Grado).ThenBy(c => c.Grupo).Select(c => new CursoResponse(c.Grado, c.Grupo, c.Total))];
    }
}
