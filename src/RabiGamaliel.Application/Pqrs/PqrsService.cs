using FluentValidation;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Pqrs;

namespace RabiGamaliel.Application.Pqrs;

public interface IPqrsService
{
    Task<Result<PqrsRadicadaResponse>> RadicarAsync(RadicarPqrsRequest request, CancellationToken ct);

    Task<PagedResult<PqrsResponse>> ListarAsync(PqrsQuery query, CancellationToken ct);

    Task<Result<PqrsResponse>> ResponderAsync(Guid id, ResponderPqrsRequest request, CancellationToken ct);
}

public sealed class PqrsService(
    IPqrsRepository repository,
    IUnitOfWork unitOfWork,
    IValidator<RadicarPqrsRequest> radicarValidator,
    IValidator<ResponderPqrsRequest> responderValidator,
    TimeProvider timeProvider) : IPqrsService
{
    public async Task<Result<PqrsRadicadaResponse>> RadicarAsync(RadicarPqrsRequest request, CancellationToken ct)
    {
        if (await radicarValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var ahora = timeProvider.GetUtcNow();
        var solicitud = SolicitudPqrs.Radicar(request.Tipo, request.NombreCompleto, request.Correo, request.Mensaje, ahora);

        repository.Agregar(solicitud);
        await unitOfWork.SaveChangesAsync(ct);

        return new PqrsRadicadaResponse(solicitud.Id, solicitud.Radicado, solicitud.CreadoEn);
    }

    public async Task<PagedResult<PqrsResponse>> ListarAsync(PqrsQuery query, CancellationToken ct)
    {
        var normalizada = query with
        {
            Pagina = Math.Max(1, query.Pagina),
            TamanoPagina = Math.Clamp(query.TamanoPagina, 1, 100),
        };

        var pagina = await repository.ListarAsync(normalizada, ct);
        return pagina.Map(PqrsResponse.Desde);
    }

    public async Task<Result<PqrsResponse>> ResponderAsync(Guid id, ResponderPqrsRequest request, CancellationToken ct)
    {
        if (await responderValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var solicitud = await repository.ObtenerPorIdAsync(id, ct);
        if (solicitud is null)
        {
            return Error.NoEncontrado("Pqrs.NoEncontrada", $"No existe una solicitud con id '{id}'.");
        }

        if (solicitud.Estado == EstadoPqrs.Cerrada)
        {
            return Error.Conflicto("Pqrs.Cerrada", "La solicitud ya está cerrada.");
        }

        solicitud.Responder(request.Respuesta);
        await unitOfWork.SaveChangesAsync(ct);
        return PqrsResponse.Desde(solicitud);
    }
}
