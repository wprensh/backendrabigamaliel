using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using RabiGamaliel.Api.Infrastructure;
using RabiGamaliel.Application.Usuarios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RabiGamaliel.Api.Endpoints;

/// <summary>Inicio de sesión del panel administrativo.</summary>
internal static class AutenticacionEndpoints
{
    public const string RateLimitPolicy = "inicio-sesion";

    public static IEndpointRouteBuilder MapAutenticacionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Autenticación");

        group.MapGet("/estado", EstadoAsync)
            .WithName("EstadoAutenticacion")
            .WithSummary("Indica si falta crear el primer superadministrador.");

        group.MapPost("/iniciar-sesion", IniciarSesionAsync)
            .WithName("IniciarSesion")
            .WithSummary("Inicia sesión con usuario y contraseña y devuelve el token.")
            .RequireRateLimiting(RateLimitPolicy);

        // Solo mientras no exista ningún usuario, y con la clave de administración de Render.
        group.MapPost("/configuracion-inicial", ConfigurarAsync)
            .WithName("ConfiguracionInicial")
            .WithSummary("Crea el primer superadministrador.")
            .RequireRateLimiting(RateLimitPolicy);

        group.MapGet("/perfil", PerfilAsync)
            .WithName("PerfilActual")
            .WithSummary("Datos y permisos del usuario de la sesión.")
            .RequiereSesion();

        group.MapPut("/perfil/clave", CambiarClaveAsync)
            .WithName("CambiarMiClave")
            .WithSummary("Cambia la contraseña del usuario de la sesión.")
            .RequiereSesion();

        return app;
    }

    private static async Task<Ok<EstadoAutenticacionResponse>> EstadoAsync(IAutenticacionService service, CancellationToken ct) =>
        TypedResults.Ok(await service.EstadoAsync(ct));

    private static async Task<Results<Ok<SesionResponse>, ProblemHttpResult>> IniciarSesionAsync(
        IniciarSesionRequest request, IAutenticacionService service, CancellationToken ct)
    {
        var resultado = await service.IniciarSesionAsync(request, ct);
        return resultado.Match<Results<Ok<SesionResponse>, ProblemHttpResult>>(
            sesion => TypedResults.Ok(sesion),
            error => error.ToProblem());
    }

    private static async Task<Results<Ok<SesionResponse>, ProblemHttpResult>> ConfigurarAsync(
        ConfiguracionInicialRequest request, HttpContext httpContext, IAutenticacionService service, CancellationToken ct)
    {
        if (!ClaveAdministracion.EsAdministrador(httpContext))
        {
            return TypedResults.Problem(
                title: "La clave de administración no es correcta.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var resultado = await service.ConfigurarAsync(request, ct);
        return resultado.Match<Results<Ok<SesionResponse>, ProblemHttpResult>>(
            sesion => TypedResults.Ok(sesion),
            error => error.ToProblem());
    }

    private static async Task<Ok<PerfilResponse>> PerfilAsync(UsuarioActual usuarioActual, CancellationToken ct) =>
        TypedResults.Ok(PerfilResponse.Desde((await usuarioActual.ObtenerAsync(ct))!));

    private static async Task<Results<Ok<SesionResponse>, ProblemHttpResult>> CambiarClaveAsync(
        CambiarMiClaveRequest request, ClaimsPrincipal principal, IAutenticacionService service, CancellationToken ct)
    {
        var id = Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        var resultado = await service.CambiarMiClaveAsync(id, request, ct);
        return resultado.Match<Results<Ok<SesionResponse>, ProblemHttpResult>>(
            sesion => TypedResults.Ok(sesion),
            error => error.ToProblem());
    }
}
