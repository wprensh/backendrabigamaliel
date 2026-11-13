using System.Net;
using System.Text.Json;
using System.Threading.Channels;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;

namespace RabiGamaliel.Infrastructure.Push;

/// <summary>
/// Sección "Push": claves VAPID para las notificaciones al celular (app instalada) y al navegador.
/// Se generan una sola vez (por ejemplo con <c>npx web-push generate-vapid-keys</c>) y se guardan como variables de entorno
/// Push__ClavePublica, Push__ClavePrivada y Push__Asunto (mailto: del colegio).
/// </summary>
public sealed class OpcionesPush
{
    public const string Seccion = "Push";

    public string? ClavePublica { get; init; }

    public string? ClavePrivada { get; init; }

    /// <summary>Contacto del remitente que exige el estándar VAPID: "mailto:..." o la URL del sitio.</summary>
    public string Asunto { get; init; } = "https://wprensh.github.io/frontendrabigamaliel/";

    public bool Configurado => !string.IsNullOrWhiteSpace(ClavePublica) && !string.IsNullOrWhiteSpace(ClavePrivada);
}

/// <summary>Cola en memoria: el envío ocurre en <see cref="EnvioPushService"/> sin demorar la petición.</summary>
internal sealed class ColaPush(IOptions<OpcionesPush> opciones) : IColaPush
{
    private readonly Channel<PushSaliente> _canal = Channel.CreateBounded<PushSaliente>(
        new BoundedChannelOptions(2000) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    public ChannelReader<PushSaliente> Lector => _canal.Reader;

    public bool Habilitado => opciones.Value.Configurado;

    public string? ClavePublica => Habilitado ? opciones.Value.ClavePublica : null;

    public void Encolar(PushSaliente push)
    {
        if (Habilitado)
        {
            _canal.Writer.TryWrite(push);
        }
    }
}

/// <summary>Envía las notificaciones a cada dispositivo suscrito y borra las suscripciones que el navegador dio de baja.</summary>
internal sealed class EnvioPushService(
    ColaPush cola, IOptions<OpcionesPush> opciones, IServiceScopeFactory scopes, IHttpClientFactory http, ILogger<EnvioPushService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var push in cola.Lector.ReadAllAsync(stoppingToken))
        {
            try
            {
                await EnviarAsync(push, stoppingToken);
            }
            catch (Exception ex) when (ex is DbUpdateException or InvalidOperationException or HttpRequestException)
            {
                logger.LogWarning(ex, "No se pudo enviar la notificación push '{Titulo}'.", push.Titulo);
            }
        }
    }

    private async Task EnviarAsync(PushSaliente push, CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var suscripciones = await db.SuscripcionesPush.Where(s => push.MiembroIds.Contains(s.MiembroId)).ToListAsync(ct);
        if (suscripciones.Count == 0)
        {
            return;
        }

        var o = opciones.Value;
        var vapid = new VapidDetails(o.Asunto, o.ClavePublica, o.ClavePrivada);
        using var cliente = new WebPushClient(http.CreateClient("push"));
        var cuerpo = Carga(push);
        var vencidas = false;
        foreach (var s in suscripciones)
        {
            try
            {
                await cliente.SendNotificationAsync(new PushSubscription(s.Endpoint, s.P256dh, s.Auth), cuerpo, vapid, ct);
            }
            catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
            {
                db.SuscripcionesPush.Remove(s);
                vencidas = true;
            }
            catch (WebPushException ex)
            {
                logger.LogWarning(ex, "El servicio push respondió {Estado}.", ex.StatusCode);
            }
        }

        if (vencidas)
        {
            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Formato que entiende el service worker de Angular: muestra la notificación con el escudo y, al tocarla, abre la
    /// página del aviso (la URL es relativa al sitio, para que funcione con o sin subcarpeta).
    /// </summary>
    internal static string Carga(PushSaliente push)
    {
        var url = string.IsNullOrWhiteSpace(push.Enlace) ? "campus" : push.Enlace.TrimStart('/');
        return JsonSerializer.Serialize(new
        {
            notification = new
            {
                title = push.Titulo,
                body = push.Mensaje.Length > 240 ? push.Mensaje[..240] + "…" : push.Mensaje,
                icon = "img/icons/icon-192.png",
                badge = "img/icons/badge-72.png",
                lang = "es-CO",
                tag = url,
                data = new
                {
                    url,
                    onActionClick = new { @default = new { operation = "navigateLastFocusedOrOpen", url } },
                },
            },
        });
    }
}
