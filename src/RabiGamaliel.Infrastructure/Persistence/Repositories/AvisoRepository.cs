using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Domain.Avisos;
using Microsoft.EntityFrameworkCore;

namespace RabiGamaliel.Infrastructure.Persistence.Repositories;

internal sealed class AvisoRepository(ApplicationDbContext db) : IAvisoRepository
{
    public void Agregar(Aviso aviso) => db.Avisos.Add(aviso);

    public Task<List<Aviso>> DeMiembroAsync(Guid miembroId, int maximo, CancellationToken ct) =>
        db.Avisos.AsNoTracking().Where(a => a.DestinatarioId == miembroId).OrderByDescending(a => a.CreadoEn).Take(maximo).ToListAsync(ct);

    public Task<int> NoLeidosAsync(Guid miembroId, CancellationToken ct) =>
        db.Avisos.CountAsync(a => a.DestinatarioId == miembroId && a.LeidoEn == null, ct);

    public Task<List<Aviso>> NoLeidosParaMarcarAsync(Guid miembroId, IReadOnlyCollection<Guid>? ids, CancellationToken ct) =>
        db.Avisos.Where(a => a.DestinatarioId == miembroId && a.LeidoEn == null && (ids == null || ids.Contains(a.Id))).ToListAsync(ct);

    public async Task<bool> ExisteDesdeAsync(Guid destinatarioId, Guid estudianteId, TipoAviso tipo, DateTimeOffset desde, CancellationToken ct) =>
        db.ChangeTracker.Entries<Aviso>().Any(e => e.State == EntityState.Added && e.Entity.DestinatarioId == destinatarioId
                                                   && e.Entity.EstudianteId == estudianteId && e.Entity.Tipo == tipo)
        || await db.Avisos.AnyAsync(a => a.DestinatarioId == destinatarioId && a.EstudianteId == estudianteId && a.Tipo == tipo && a.CreadoEn >= desde, ct);
}
