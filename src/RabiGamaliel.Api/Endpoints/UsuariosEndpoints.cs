using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using RabiGamaliel.Api.Infrastructure;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Application.Usuarios;
using RabiGamaliel.Domain.Usuarios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RabiGamaliel.Api.Endpoints;

/// <summary>Módulo de usuarios: creación, permisos por módulo, restablecer contraseña y eliminación.</summary>
internal static class UsuariosEndpoints
{
    public static IEndpointRouteBuilder MapUsuariosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/usuarios").WithTags("Usuarios");

        group.MapGet("/", ListarAsync)
            .WithName("ListarUsuarios")
            .WithSummary("Lista los usuarios del panel.")
            .RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Ver);

        group.MapGet("/permisos", CatalogoPermisos)
            .WithName("CatalogoPermisos")
            .WithSummary("Módulos y acciones que se pueden asignar.")
            .RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Ver);

        group.MapGet("/{id:guid}", ObtenerAsync)
            .WithName("ObtenerUsuario")
            .WithSummary("Obtiene un usuario por su id.")
            .RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Ver);

        group.MapPost("/", CrearAsync)
            .WithName("CrearUsuario")
            .WithSummary("Crea un usuario con sus permisos.")
            .RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Crear);

        group.MapPut("/{id:guid}", ActualizarAsync)
            .WithName("ActualizarUsuario")
            .WithSummary("Actualiza los datos, el estado y los permisos de un usuario.")
            .RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Editar);

        group.MapPut("/{id:guid}/clave", RestablecerClaveAsync)
            .WithName("RestablecerClaveUsuario")
            .WithSummary("Asigna una contraseña nueva y cierra las sesiones del usuario.")
            .RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Editar);

        group.MapDelete("/{id:guid}", EliminarAsync)
            .WithName("EliminarUsuario")
            .WithSummary("Elimina un usuario.")
            .RequierePermiso(ModuloSistema.Usuarios, AccionPermiso.Eliminar);

        return app;
    }

    /// <summary>Id de quien hace la petición; vacío si entró con la clave de administración.</summary>
    private static Guid Actor(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : Guid.Empty;

    /// <summary>
    /// true si quien llama es superadministrador o usa la clave de administración (llave de emergencia).
    /// Rector, coordinador, secretaria y demás usuarios del panel no ven a los superadministradores.
    /// </summary>
    private static async Task<bool> EsSuperAsync(UsuarioActual usuarioActual, CancellationToken ct) =>
        await usuarioActual.ObtenerAsync(ct) is not { } usuario || usuario.EsSuperAdministrador;

    private static async Task<Ok<PagedResult<UsuarioResponse>>> ListarAsync(
        IUsuarioService service, UsuarioActual usuarioActual, CancellationToken ct,
        string? buscar = null, int pagina = 1, int tamanoPagina = 20)
    {
        var ocultar = !await EsSuperAsync(usuarioActual, ct);
        return TypedResults.Ok(await service.ListarAsync(new UsuarioQuery(buscar, pagina, tamanoPagina, ocultar), ct));
    }

    private static Ok<IReadOnlyList<ModuloPermisosResponse>> CatalogoPermisos(IUsuarioService service) =>
        TypedResults.Ok(service.CatalogoPermisos());

    private static async Task<Results<Ok<UsuarioResponse>, ProblemHttpResult>> ObtenerAsync(
        Guid id, IUsuarioService service, UsuarioActual usuarioActual, CancellationToken ct)
    {
        var resultado = await service.ObtenerAsync(id, ct, await EsSuperAsync(usuarioActual, ct));
        return resultado.Match<Results<Ok<UsuarioResponse>, ProblemHttpResult>>(
            usuario => TypedResults.Ok(usuario),
            error => error.ToProblem());
    }

    private static async Task<Results<CreatedAtRoute<UsuarioResponse>, ProblemHttpResult>> CrearAsync(
        CrearUsuarioRequest request, IUsuarioService service, UsuarioActual usuarioActual, CancellationToken ct)
    {
        var resultado = await service.CrearAsync(request, ct, await EsSuperAsync(usuarioActual, ct));
        return resultado.Match<Results<CreatedAtRoute<UsuarioResponse>, ProblemHttpResult>>(
            usuario => TypedResults.CreatedAtRoute(usuario, "ObtenerUsuario", new { id = usuario.Id }),
            error => error.ToProblem());
    }

    private static async Task<Results<Ok<UsuarioResponse>, ProblemHttpResult>> ActualizarAsync(
        Guid id, ActualizarUsuarioRequest request, ClaimsPrincipal principal, IUsuarioService service,
        UsuarioActual usuarioActual, CancellationToken ct)
    {
        var resultado = await service.ActualizarAsync(id, request, Actor(principal), ct, await EsSuperAsync(usuarioActual, ct));
        return resultado.Match<Results<Ok<UsuarioResponse>, ProblemHttpResult>>(
            usuario => TypedResults.Ok(usuario),
            error => error.ToProblem());
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> RestablecerClaveAsync(
        Guid id, RestablecerClaveRequest request, IUsuarioService service, UsuarioActual usuarioActual, CancellationToken ct)
    {
        var resultado = await service.RestablecerClaveAsync(id, request, ct, await EsSuperAsync(usuarioActual, ct));
        return resultado.Match<Results<NoContent, ProblemHttpResult>>(
            _ => TypedResults.NoContent(),
            error => error.ToProblem());
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> EliminarAsync(
        Guid id, ClaimsPrincipal principal, IUsuarioService service, UsuarioActual usuarioActual, CancellationToken ct)
    {
        var resultado = await service.EliminarAsync(id, Actor(principal), ct, await EsSuperAsync(usuarioActual, ct));
        return resultado.Match<Results<NoContent, ProblemHttpResult>>(
            _ => TypedResults.NoContent(),
            error => error.ToProblem());
    }
}
