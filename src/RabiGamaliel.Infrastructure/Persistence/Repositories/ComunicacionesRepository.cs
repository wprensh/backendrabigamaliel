using RabiGamaliel.Application.Comunicaciones;
using RabiGamaliel.Domain.Comunicaciones;
using Microsoft.EntityFrameworkCore;

namespace RabiGamaliel.Infrastructure.Persistence.Repositories;

internal sealed class ComunicacionesRepository(ApplicationDbContext db) : IComunicacionesRepository
{
    public Task<List<Circular>> CircularesAsync(bool soloPublicadas, CancellationToken ct) =>
        db.Circulares.Where(c => !soloPublicadas || c.PublicadaEn != null)
            .OrderByDescending(c => c.Anio).ThenByDescending(c => c.Numero).ToListAsync(ct);

    public Task<Circular?> CircularAsync(Guid id, CancellationToken ct) => db.Circulares.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<int> SiguienteNumeroAsync(int anio, CancellationToken ct) =>
        (await db.Circulares.Where(c => c.Anio == anio).MaxAsync(c => (int?)c.Numero, ct) ?? 0) + 1;

    public void Agregar(Circular circular) => db.Circulares.Add(circular);

    public void Eliminar(Circular circular) => db.Circulares.Remove(circular);

    public Task<List<ConteoLecturas>> ConteoLecturasAsync(CancellationToken ct) =>
        db.LecturasCircular.GroupBy(l => l.CircularId)
            .Select(g => new ConteoLecturas(g.Key, g.Count(), g.Count(l => l.ConfirmadaEn != null)))
            .ToListAsync(ct);

    public Task<List<LecturaCircular>> LecturasAsync(Guid circularId, CancellationToken ct) =>
        db.LecturasCircular.AsNoTracking().Where(l => l.CircularId == circularId).ToListAsync(ct);

    public Task<List<LecturaCircular>> LecturasDeMiembroAsync(Guid miembroId, CancellationToken ct) =>
        db.LecturasCircular.Where(l => l.MiembroId == miembroId).ToListAsync(ct);

    public void Agregar(LecturaCircular lectura) => db.LecturasCircular.Add(lectura);

    public Task<List<EventoCalendario>> EventosAsync(DateOnly desde, DateOnly hasta, bool soloPublicos, CancellationToken ct) =>
        db.EventosCalendario.Where(e => e.Fin >= desde && e.Inicio <= hasta && (!soloPublicos || e.Publico))
            .OrderBy(e => e.Inicio).ThenBy(e => e.Titulo).ToListAsync(ct);

    public Task<EventoCalendario?> EventoAsync(Guid id, CancellationToken ct) => db.EventosCalendario.FirstOrDefaultAsync(e => e.Id == id, ct);

    public void Agregar(EventoCalendario evento) => db.EventosCalendario.Add(evento);

    public void Eliminar(EventoCalendario evento) => db.EventosCalendario.Remove(evento);
}
