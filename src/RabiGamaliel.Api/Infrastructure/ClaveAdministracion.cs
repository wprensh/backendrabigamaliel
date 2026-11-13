using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace RabiGamaliel.Api.Infrastructure;

/// <summary>Configuración de la sección "Administracion" (variable de entorno Administracion__ClaveApi).</summary>
internal sealed class OpcionesAdministracion
{
    public const string Seccion = "Administracion";

    /// <summary>
    /// Clave de emergencia con acceso total (mínimo 16 caracteres). También se pide una sola vez
    /// para crear el primer superadministrador.
    /// </summary>
    public string? ClaveApi { get; init; }
}

/// <summary>
/// Clave compartida en el encabezado <c>X-Clave-Admin</c>. El panel ahora usa usuarios con permisos
/// (<see cref="Permisos"/>); la clave queda como llave de emergencia y para la configuración inicial.
/// Si la clave no está configurada, nadie es administrador (falla cerrada).
/// </summary>
internal static class ClaveAdministracion
{
    public const string Encabezado = "X-Clave-Admin";
    private const int LongitudMinima = 16;

    public static bool EsAdministrador(HttpContext httpContext)
    {
        var clave = httpContext.RequestServices.GetRequiredService<IOptions<OpcionesAdministracion>>().Value.ClaveApi;
        if (string.IsNullOrWhiteSpace(clave) || clave.Length < LongitudMinima)
        {
            return false;
        }

        if (!httpContext.Request.Headers.TryGetValue(Encabezado, out var enviada))
        {
            return false;
        }

        // Comparación en tiempo constante: no revela por tiempos cuántos caracteres coinciden.
        var esperada = Encoding.UTF8.GetBytes(clave);
        var recibida = Encoding.UTF8.GetBytes(enviada.ToString());
        return esperada.Length == recibida.Length && CryptographicOperations.FixedTimeEquals(esperada, recibida);
    }
}
