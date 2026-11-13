using RabiGamaliel.Application.Asistencia;
using RabiGamaliel.Domain.Asistencia;
using Microsoft.EntityFrameworkCore;

namespace RabiGamaliel.Infrastructure.Persistence.Repositories;

internal sealed class AsistenciaRepository(ApplicationDbContext db) : IAsistenciaRepository
{
    public Task<TomaAsistencia?> TomaAsync(Guid asignacionId, DateOnly fecha, CancellationToken ct) =>
        db.TomasAsistencia.Include(t => t.Novedades).FirstOrDefaultAsync(t => t.AsignacionId == asignacionId && t.Fecha == fecha, ct);

    public void Agregar(TomaAsistencia toma) => db.TomasAsistencia.Add(toma);

    public Task<List<NovedadDetalle>> NovedadesDeEstudianteAsync(Guid estudianteId, DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        (from n in db.NovedadesAsistencia
         join t in db.TomasAsistencia on n.TomaId equals t.Id
         where n.EstudianteId == estudianteId && t.Fecha >= desde && t.Fecha <= hasta
         select new NovedadDetalle(t.Fecha, t.AsignaturaId, n.Estado, n.Observacion))
        .ToListAsync(ct);

    public Task<List<ConteoNovedad>> ConteosAsync(IReadOnlyCollection<Guid> estudianteIds, DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        (from n in db.NovedadesAsistencia
         join t in db.TomasAsistencia on n.TomaId equals t.Id
         where estudianteIds.Contains(n.EstudianteId) && t.Fecha >= desde && t.Fecha <= hasta
         group n by new { n.EstudianteId, n.Estado } into g
         select new ConteoNovedad(g.Key.EstudianteId, g.Key.Estado, g.Count()))
        .ToListAsync(ct);

    public Task<int> TomasAsync(int grado, string? grupo, DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        db.TomasAsistencia.CountAsync(t => t.Grado == grado && (grupo == null || t.Grupo == null || t.Grupo == grupo) && t.Fecha >= desde && t.Fecha <= hasta, ct);
}
