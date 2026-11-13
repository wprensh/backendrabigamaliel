using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Avisos;
using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Avisos;

public interface IPushRepository
{
    Task<SuscripcionPush?> PorEndpointAsync(string endpoint, CancellationToken ct);

    Task<int> ConteoDeMiembroAsync(Guid miembroId, CancellationToken ct);

    void Agregar(SuscripcionPush suscripcion);

    void Eliminar(SuscripcionPush suscripcion);
}

public sealed record PushConfiguracionResponse(bool Habilitado, string? ClavePublica, int Dispositivos);

/// <summary>Cuerpo de la suscripción que entrega el navegador (PushSubscription.toJSON()).</summary>
public sealed record SuscribirPushRequest(string Endpoint, string P256dh, string Auth);

public interface IPushService
{
    Task<PushConfiguracionResponse> ConfiguracionAsync(MiembroCampus miembro, CancellationToken ct);

    Task<Result<PushConfiguracionResponse>> SuscribirAsync(MiembroCampus miembro, SuscribirPushRequest request, CancellationToken ct);

    Task<Result<Unit>> DesuscribirAsync(MiembroCampus miembro, string endpoint, CancellationToken ct);
}

/// <summary>Registro de los dispositivos que reciben notificaciones push del campus.</summary>
public sealed class PushService(IPushRepository repository, IColaPush cola, IUnitOfWork unitOfWork) : IPushService
{
    public async Task<PushConfiguracionResponse> ConfiguracionAsync(MiembroCampus miembro, CancellationToken ct) =>
        new(cola.Habilitado, cola.ClavePublica, await repository.ConteoDeMiembroAsync(miembro.Id, ct));

    public async Task<Result<PushConfiguracionResponse>> SuscribirAsync(MiembroCampus miembro, SuscribirPushRequest request, CancellationToken ct)
    {
        if (!cola.Habilitado)
        {
            return Error.Conflicto("Push.NoConfigurado", "Las notificaciones push aún no están configuradas en el servidor.");
        }

        if (!Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps
            || request.Endpoint.Length > SuscripcionPush.EndpointMaxLength
            || string.IsNullOrWhiteSpace(request.P256dh) || request.P256dh.Length > SuscripcionPush.ClaveMaxLength
            || string.IsNullOrWhiteSpace(request.Auth) || request.Auth.Length > SuscripcionPush.ClaveMaxLength)
        {
            return Error.Campo("Endpoint", "La suscripción del navegador no es válida.");
        }

        var existente = await repository.PorEndpointAsync(request.Endpoint.Trim(), ct);
        if (existente is null)
        {
            repository.Agregar(SuscripcionPush.Crear(miembro.Id, request.Endpoint, request.P256dh, request.Auth));
        }
        else
        {
            existente.Actualizar(miembro.Id, request.P256dh, request.Auth);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await ConfiguracionAsync(miembro, ct);
    }

    public async Task<Result<Unit>> DesuscribirAsync(MiembroCampus miembro, string endpoint, CancellationToken ct)
    {
        var existente = await repository.PorEndpointAsync(endpoint.Trim(), ct);
        if (existente is not null && existente.MiembroId == miembro.Id)
        {
            repository.Eliminar(existente);
            await unitOfWork.SaveChangesAsync(ct);
        }

        return Unit.Value;
    }
}
