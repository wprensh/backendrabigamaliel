using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Elecciones;
using RabiGamaliel.Application.Usuarios;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Usuarios;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace RabiGamaliel.Api.Infrastructure;

/// <summary>Configuración de la sección "Autenticacion".</summary>
internal sealed class OpcionesAutenticacion
{
    public const string Seccion = "Autenticacion";
    public const string Emisor = "RabiGamaliel.Api";
    public const string Audiencia = "panel-administrativo";

    /// <summary>Los tokens del campus llevan otra audiencia: no abren el panel, y los del panel no abren el campus.</summary>
    public const string AudienciaCampus = "campus-virtual";

    /// <summary>
    /// Clave para firmar los tokens (Autenticacion__ClaveFirma, mínimo 32 caracteres). Si no se configura
    /// se deriva de Administracion__ClaveApi, así el despliegue actual funciona sin variables nuevas.
    /// </summary>
    public string? ClaveFirma { get; init; }

    /// <summary>Horas que dura una sesión antes de pedir de nuevo la contraseña.</summary>
    public int DuracionSesionHoras { get; init; } = 8;

    /// <summary>Minutos que dura una sesión del campus (en la sala de informática los equipos se comparten).</summary>
    public int DuracionSesionCampusMinutos { get; init; } = 60;

    public static SymmetricSecurityKey ClaveDeFirma(IConfiguration configuracion)
    {
        var propia = configuracion[$"{Seccion}:ClaveFirma"];
        if (!string.IsNullOrWhiteSpace(propia) && propia.Length >= 32)
        {
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(propia));
        }

        var claveApi = configuracion[$"{OpcionesAdministracion.Seccion}:ClaveApi"];
        if (!string.IsNullOrWhiteSpace(claveApi) && claveApi.Length >= 16)
        {
            // Derivada con HMAC para no reutilizar la clave de administración tal cual.
            return new SymmetricSecurityKey(
                HMACSHA256.HashData(Encoding.UTF8.GetBytes(claveApi), "firma-tokens-panel"u8));
        }

        // Sin ninguna clave configurada: una aleatoria. Las sesiones se pierden al reiniciar, pero nunca
        // se firma con un valor predecible (falla cerrada).
        return new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32));
    }
}

/// <summary>Emite tokens JWT firmados con HMAC-SHA256.</summary>
internal sealed class EmisorTokensJwt(
    SymmetricSecurityKey clave, IOptions<OpcionesAutenticacion> opciones, TimeProvider reloj) : IEmisorTokens
{
    public const string ClaimSello = "sello";

    public const string ClaimRol = "rol";

    public TokenEmitido Emitir(Usuario usuario) =>
        Firmar(
            OpcionesAutenticacion.Audiencia,
            TimeSpan.FromHours(Math.Clamp(opciones.Value.DuracionSesionHoras, 1, 24 * 7)),
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, usuario.NombreUsuario),
                new Claim(ClaimSello, usuario.SelloSeguridad.ToString()),
            ]);

    public TokenEmitido EmitirCampus(MiembroCampus miembro) =>
        Firmar(
            OpcionesAutenticacion.AudienciaCampus,
            TimeSpan.FromMinutes(Math.Clamp(opciones.Value.DuracionSesionCampusMinutos, 10, 24 * 60)),
            [
                new Claim(JwtRegisteredClaimNames.Sub, miembro.Id.ToString()),
                new Claim(ClaimRol, miembro.Rol.ToString()),
                new Claim(ClaimSello, miembro.SelloSeguridad.ToString()),
            ]);

    private TokenEmitido Firmar(string audiencia, TimeSpan duracion, IEnumerable<Claim> claims)
    {
        var ahora = reloj.GetUtcNow();
        var expira = ahora + duracion;

        var token = new JwtSecurityToken(
            issuer: OpcionesAutenticacion.Emisor,
            audience: audiencia,
            claims: claims,
            notBefore: ahora.UtcDateTime,
            expires: expira.UtcDateTime,
            signingCredentials: new SigningCredentials(clave, SecurityAlgorithms.HmacSha256));

        return new TokenEmitido(new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}

/// <summary>
/// Usuario de la petición actual. Se lee de la base de datos en cada petición (una sola vez):
/// así un usuario desactivado, con permisos cambiados o con la clave restablecida pierde el acceso
/// de inmediato, sin esperar a que venza su token.
/// </summary>
internal sealed class UsuarioActual(IHttpContextAccessor accesor, IUsuarioRepository repository)
{
    private Task<Usuario?>? _usuario;

    public Task<Usuario?> ObtenerAsync(CancellationToken ct) => _usuario ??= CargarAsync(ct);

    private async Task<Usuario?> CargarAsync(CancellationToken ct)
    {
        if (!Sesiones.Leer(accesor.HttpContext?.User, OpcionesAutenticacion.Audiencia, out var id, out var sello))
        {
            return null;
        }

        var usuario = await repository.ObtenerPorIdAsync(id, ct);
        return usuario is { Activo: true } && usuario.SelloSeguridad == sello ? usuario : null;
    }
}

/// <summary>Estudiante o docente de la sesión del campus; se lee de la base de datos una vez por petición.</summary>
internal sealed class MiembroActual(IHttpContextAccessor accesor, IMiembroCampusRepository repository)
{
    private Task<MiembroCampus?>? _miembro;

    public Task<MiembroCampus?> ObtenerAsync(CancellationToken ct) => _miembro ??= CargarAsync(ct);

    /// <summary>Datos de votante de la sesión (ya validada por <see cref="Sesiones.RequiereSesionCampus"/>).</summary>
    public async Task<VotanteActual> VotanteAsync(CancellationToken ct)
    {
        var miembro = (await ObtenerAsync(ct))!;
        return new VotanteActual(miembro.Id, miembro.Rol, miembro.Grado, miembro.Grupo);
    }

    private async Task<MiembroCampus?> CargarAsync(CancellationToken ct)
    {
        if (!Sesiones.Leer(accesor.HttpContext?.User, OpcionesAutenticacion.AudienciaCampus, out var id, out var sello))
        {
            return null;
        }

        var miembro = await repository.ObtenerPorIdAsync(id, ct);
        return miembro is { Activo: true } && miembro.SelloSeguridad == sello ? miembro : null;
    }
}

internal static class Sesiones
{
    /// <summary>Id y sello de un token válido de la audiencia indicada.</summary>
    public static bool Leer(ClaimsPrincipal? principal, string audiencia, out Guid id, out Guid sello)
    {
        id = sello = Guid.Empty;
        return principal?.Identity?.IsAuthenticated == true
               && principal.FindFirstValue(JwtRegisteredClaimNames.Aud) == audiencia
               && Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out id)
               && Guid.TryParse(principal.FindFirstValue(EmisorTokensJwt.ClaimSello), out sello);
    }

    /// <summary>Exige una sesión válida del campus (estudiante o docente activo).</summary>
    public static TBuilder RequiereSesionCampus<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(async (contexto, siguiente) =>
        {
            var httpContext = contexto.HttpContext;
            var miembro = await httpContext.RequestServices.GetRequiredService<MiembroActual>()
                .ObtenerAsync(httpContext.RequestAborted);
            return miembro is null
                ? TypedResults.Problem(
                    title: "Su sesión del campus terminó. Ingrese de nuevo.",
                    statusCode: StatusCodes.Status401Unauthorized)
                : await siguiente(contexto);
        });
}

internal static class Permisos
{
    /// <summary>
    /// ¿Puede quien hace la petición realizar <paramref name="accion"/> en <paramref name="modulo"/>?
    /// La clave de administración (X-Clave-Admin) sigue dando acceso total como llave de emergencia.
    /// </summary>
    public static async Task<bool> TieneAsync(HttpContext httpContext, ModuloSistema modulo, AccionPermiso accion)
    {
        if (ClaveAdministracion.EsAdministrador(httpContext))
        {
            return true;
        }

        var usuario = await httpContext.RequestServices.GetRequiredService<UsuarioActual>()
            .ObtenerAsync(httpContext.RequestAborted);
        return usuario?.Puede(modulo, accion) == true;
    }

    /// <summary>Responde 401 sin sesión válida y 403 si el usuario no tiene el permiso.</summary>
    public static TBuilder RequierePermiso<TBuilder>(this TBuilder builder, ModuloSistema modulo, AccionPermiso accion)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequierePermisoEnAlguno(accion, modulo);

    /// <summary>Exige <paramref name="accion"/> en al menos uno de los <paramref name="modulos"/> (p. ej. una lista que usan dos módulos).</summary>
    public static TBuilder RequierePermisoEnAlguno<TBuilder>(this TBuilder builder, AccionPermiso accion, params ModuloSistema[] modulos)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(async (contexto, siguiente) =>
        {
            var httpContext = contexto.HttpContext;
            foreach (var modulo in modulos)
            {
                if (await TieneAsync(httpContext, modulo, accion))
                {
                    return await siguiente(contexto);
                }
            }

            var usuario = await httpContext.RequestServices.GetRequiredService<UsuarioActual>()
                .ObtenerAsync(httpContext.RequestAborted);
            return usuario is null
                ? SinSesion()
                : TypedResults.Problem(
                    title: $"Su usuario no tiene permiso para {Verbo(accion)} en {string.Join(" ni en ", modulos)}.",
                    statusCode: StatusCodes.Status403Forbidden);
        });

    /// <summary>Exige una sesión de usuario válida (no basta la clave de administración).</summary>
    public static TBuilder RequiereSesion<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter(async (contexto, siguiente) =>
        {
            var httpContext = contexto.HttpContext;
            var usuario = await httpContext.RequestServices.GetRequiredService<UsuarioActual>()
                .ObtenerAsync(httpContext.RequestAborted);
            return usuario is null ? SinSesion() : await siguiente(contexto);
        });

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult SinSesion() =>
        TypedResults.Problem(
            title: "Su sesión terminó. Inicie sesión de nuevo.",
            statusCode: StatusCodes.Status401Unauthorized);

    private static string Verbo(AccionPermiso accion) => accion switch
    {
        AccionPermiso.Ver => "ver registros",
        AccionPermiso.Crear => "crear registros",
        AccionPermiso.Editar => "editar registros",
        _ => "eliminar registros",
    };
}
