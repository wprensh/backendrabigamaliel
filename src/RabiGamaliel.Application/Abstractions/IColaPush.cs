namespace RabiGamaliel.Application.Abstractions;

/// <summary>Notificación push para los dispositivos suscritos de varios miembros del campus.</summary>
public sealed record PushSaliente(IReadOnlyCollection<Guid> MiembroIds, string Titulo, string Mensaje, string? Enlace);

/// <summary>
/// Cola de notificaciones push (Web Push con claves VAPID). Se envían en segundo plano; si las claves no están
/// configuradas, la cola descarta las notificaciones sin error.
/// </summary>
public interface IColaPush
{
    bool Habilitado { get; }

    /// <summary>Clave pública VAPID (base64url) que el navegador necesita para suscribirse.</summary>
    string? ClavePublica { get; }

    void Encolar(PushSaliente push);
}
