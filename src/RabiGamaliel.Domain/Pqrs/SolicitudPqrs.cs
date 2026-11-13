using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Pqrs;

/// <summary>
/// Petición, queja, reclamo o sugerencia enviada desde el formulario de la sección "Contacto".
/// </summary>
public sealed class SolicitudPqrs : Entity
{
    public const int NombreMaxLength = 120;
    public const int CorreoMaxLength = 180;
    public const int MensajeMaxLength = 2000;
    public const int RadicadoMaxLength = 20;
    public const int RespuestaMaxLength = 2000;

    private SolicitudPqrs()
    {
    }

    /// <summary>Número de radicado legible para el ciudadano, p. ej. "PQRS-2026-0A1B2C".</summary>
    public string Radicado { get; private set; } = string.Empty;

    public TipoPqrs Tipo { get; private set; }

    public string NombreCompleto { get; private set; } = string.Empty;

    public string Correo { get; private set; } = string.Empty;

    public string Mensaje { get; private set; } = string.Empty;

    public EstadoPqrs Estado { get; private set; } = EstadoPqrs.Radicada;

    public string? Respuesta { get; private set; }

    public static SolicitudPqrs Radicar(TipoPqrs tipo, string nombreCompleto, string correo, string mensaje, DateTimeOffset ahora)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreCompleto);
        ArgumentException.ThrowIfNullOrWhiteSpace(correo);
        ArgumentException.ThrowIfNullOrWhiteSpace(mensaje);

        var solicitud = new SolicitudPqrs
        {
            Tipo = tipo,
            NombreCompleto = nombreCompleto.Trim(),
            Correo = correo.Trim().ToLowerInvariant(),
            Mensaje = mensaje.Trim(),
        };

        // Los últimos 6 caracteres del Guid v7 son aleatorios: sirven como sufijo único y corto.
        solicitud.Radicado = $"PQRS-{ahora.Year}-{solicitud.Id.ToString("N")[^6..].ToUpperInvariant()}";
        return solicitud;
    }

    public void Responder(string respuesta)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(respuesta);

        if (Estado == EstadoPqrs.Cerrada)
        {
            throw new InvalidOperationException("No se puede responder una solicitud cerrada.");
        }

        Respuesta = respuesta.Trim();
        Estado = EstadoPqrs.Respondida;
    }

    public void Cerrar() => Estado = EstadoPqrs.Cerrada;
}
