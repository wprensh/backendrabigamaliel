using RabiGamaliel.Application.Common;
using RabiGamaliel.Application.Pqrs;
using RabiGamaliel.Domain.Pqrs;
using Microsoft.EntityFrameworkCore;

namespace RabiGamaliel.Infrastructure.Persistence.Repositories;

internal sealed class PqrsRepository(ApplicationDbContext db) : IPqrsRepository
{
    public Task<SolicitudPqrs?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        db.SolicitudesPqrs.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<PagedResult<SolicitudPqrs>> ListarAsync(PqrsQuery query, CancellationToken ct)
    {
        var consulta = db.SolicitudesPqrs.AsNoTracking();

        if (query.Estado is { } estado)
        {
            consulta = consulta.Where(s => s.Estado == estado);
        }

        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .OrderByDescending(s => s.CreadoEn)
            .Skip((query.Pagina - 1) * query.TamanoPagina)
            .Take(query.TamanoPagina)
            .ToListAsync(ct);

        return new PagedResult<SolicitudPqrs>(items, query.Pagina, query.TamanoPagina, total);
    }

    public void Agregar(SolicitudPqrs solicitud) => db.SolicitudesPqrs.Add(solicitud);
}
