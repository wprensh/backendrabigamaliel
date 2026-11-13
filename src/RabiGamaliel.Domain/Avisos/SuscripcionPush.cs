using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Avisos;

/// <summary>
/// Suscripción a notificaciones push (Web Push) de un navegador o de la app instalada (PWA) de un miembro del campus.
/// Un miembro puede tener varias: el celular, el computador de la casa...
/// </summary>
public sealed class SuscripcionPush : Entity
{
    public const int EndpointMaxLength = 800;
    public const int ClaveMaxLength = 200;

    private SuscripcionPush()
    {
    }

    public Guid MiembroId { get; private set; }

    /// <summary>URL del servicio push del navegador (FCM, Mozilla, Apple...).</summary>
    public string Endpoint { get; private set; } = string.Empty;

    /// <summary>Clave pública del navegador (base64url).</summary>
    public string P256dh { get; private set; } = string.Empty;

    /// <summary>Secreto de autenticación del navegador (base64url).</summary>
    public string Auth { get; private set; } = string.Empty;

    public static SuscripcionPush Crear(Guid miembroId, string endpoint, string p256dh, string auth) => new()
    {
        MiembroId = miembroId,
        Endpoint = endpoint.Trim(),
        P256dh = p256dh.Trim(),
        Auth = auth.Trim(),
    };

    /// <summary>El navegador renovó las claves o el dispositivo cambió de dueño.</summary>
    public void Actualizar(Guid miembroId, string p256dh, string auth)
    {
        MiembroId = miembroId;
        P256dh = p256dh.Trim();
        Auth = auth.Trim();
    }
}
