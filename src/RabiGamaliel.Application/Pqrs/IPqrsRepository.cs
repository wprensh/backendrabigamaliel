using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Pqrs;

namespace RabiGamaliel.Application.Pqrs;

public interface IPqrsRepository
{
    Task<SolicitudPqrs?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<PagedResult<SolicitudPqrs>> ListarAsync(PqrsQuery query, CancellationToken ct);

    void Agregar(SolicitudPqrs solicitud);
}
