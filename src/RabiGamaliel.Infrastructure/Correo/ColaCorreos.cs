using System.Net;
using System.Net.Mail;
using System.Threading.Channels;
using RabiGamaliel.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RabiGamaliel.Infrastructure.Correo;

/// <summary>
/// Sección "Correo": servidor SMTP para los avisos a las familias (p. ej. Gmail con una contraseña de aplicación,
/// Brevo o Mailjet). Sin servidor configurado, los avisos solo se ven en el campus.
/// </summary>
public sealed class OpcionesCorreo
{
    public const string Seccion = "Correo";

    public string? Servidor { get; init; }

    public int Puerto { get; init; } = 587;

    public string? Usuario { get; init; }

    public string? Clave { get; init; }

    /// <summary>Dirección que aparece como remitente.</summary>
    public string? Remitente { get; init; }

    public string NombreRemitente { get; init; } = "I.E. Rabí Gamaliel";

    public bool Configurado => !string.IsNullOrWhiteSpace(Servidor) && !string.IsNullOrWhiteSpace(Remitente);
}

/// <summary>Cola en memoria: el envío ocurre en <see cref="EnvioCorreosService"/> sin demorar la petición.</summary>
internal sealed class ColaCorreos(IOptions<OpcionesCorreo> opciones) : IColaCorreos
{
    private const int Capacidad = 5000;

    private readonly Channel<CorreoSaliente> _canal = Channel.CreateBounded<CorreoSaliente>(
        new BoundedChannelOptions(Capacidad) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public ChannelReader<CorreoSaliente> Lector => _canal.Reader;

    public void Encolar(CorreoSaliente correo)
    {
        // Sin servidor configurado el aviso queda solo en el campus.
        if (opciones.Value.Configurado)
        {
            _canal.Writer.TryWrite(correo);
        }
    }
}

/// <summary>Envía por SMTP los correos de la cola, uno a uno.</summary>
internal sealed class EnvioCorreosService(ColaCorreos cola, IOptions<OpcionesCorreo> opciones, ILogger<EnvioCorreosService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var correo in cola.Lector.ReadAllAsync(stoppingToken))
        {
            try
            {
                var o = opciones.Value;
                using var mensaje = new MailMessage(new MailAddress(o.Remitente!, o.NombreRemitente), new MailAddress(correo.Para))
                {
                    Subject = correo.Asunto,
                    Body = correo.Cuerpo,
                };
                using var cliente = new SmtpClient(o.Servidor, o.Puerto)
                {
                    EnableSsl = true,
                    Credentials = string.IsNullOrEmpty(o.Usuario) ? null : new NetworkCredential(o.Usuario, o.Clave),
                };
                await cliente.SendMailAsync(mensaje, stoppingToken);
            }
            catch (Exception ex) when (ex is SmtpException or FormatException or InvalidOperationException)
            {
                logger.LogWarning(ex, "No se pudo enviar el correo '{Asunto}'.", correo.Asunto);
            }
        }
    }
}
