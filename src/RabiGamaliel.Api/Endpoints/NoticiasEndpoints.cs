using RabiGamaliel.Api.Infrastructure;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Application.Noticias;
using RabiGamaliel.Domain.Noticias;
using RabiGamaliel.Domain.Usuarios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RabiGamaliel.Api.Endpoints;

/// <summary>
/// CRUD de noticias con Minimal APIs.
/// Cada handler devuelve <c>Results&lt;...&gt;</c> tipados: el compilador verifica las respuestas
/// posibles y OpenAPI las documenta automáticamente, sin atributos [ProducesResponseType].
/// </summary>
internal static class NoticiasEndpoints
{
    public static IEndpointRouteBuilder MapNoticiasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/noticias")
            .WithTags("Noticias");

        group.MapGet("/", ListarAsync)
            .WithName("ListarNoticias")
            .WithSummary("Lista noticias paginadas con filtros opcionales.");

        group.MapGet("/archivo", ArchivoAsync)
            .WithName("ArchivoNoticias")
            .WithSummary("Cantidad de noticias por año y mes, del más reciente al más antiguo.");

        group.MapGet("/{id:guid}", ObtenerAsync)
            .WithName("ObtenerNoticia")
            .WithSummary("Obtiene una noticia por su id.");

        // Escritura: según los permisos del usuario en el módulo Noticias.
        group.MapPost("/", CrearAsync)
            .WithName("CrearNoticia")
            .WithSummary("Crea una noticia.")
            .RequierePermiso(ModuloSistema.Noticias, AccionPermiso.Crear);

        group.MapPut("/{id:guid}", ActualizarAsync)
            .WithName("ActualizarNoticia")
            .WithSummary("Actualiza una noticia existente.")
            .RequierePermiso(ModuloSistema.Noticias, AccionPermiso.Editar);

        group.MapDelete("/{id:guid}", EliminarAsync)
            .WithName("EliminarNoticia")
            .WithSummary("Elimina una noticia.")
            .RequierePermiso(ModuloSistema.Noticias, AccionPermiso.Eliminar);

        return app;
    }

    /// <summary>El público solo ve noticias publicadas; los borradores exigen permiso para ver noticias en el panel.</summary>
    private static async Task<Ok<PagedResult<NoticiaResponse>>> ListarAsync(
        HttpContext httpContext,
        INoticiaService service,
        CancellationToken ct,
        string? buscar = null,
        CategoriaNoticia? categoria = null,
        bool soloPublicadas = false,
        int pagina = 1,
        int tamanoPagina = 10,
        DateOnly? desde = null,
        DateOnly? hasta = null,
        OrdenNoticias orden = OrdenNoticias.Recientes)
    {
        var soloVisibles = soloPublicadas || !await VeBorradoresAsync(httpContext);
        var consulta = new NoticiaQuery(buscar, categoria, soloVisibles, pagina, tamanoPagina, desde, hasta, orden);
        var resultado = await service.ListarAsync(consulta, ct);
        return TypedResults.Ok(resultado);
    }

    /// <summary>Igual que el listado: el público solo cuenta las noticias publicadas.</summary>
    private static async Task<Ok<IReadOnlyList<ArchivoNoticiasItem>>> ArchivoAsync(
        HttpContext httpContext, INoticiaService service, CancellationToken ct, bool soloPublicadas = false)
    {
        var soloVisibles = soloPublicadas || !await VeBorradoresAsync(httpContext);
        return TypedResults.Ok(await service.ArchivoAsync(soloVisibles, ct));
    }

    private static async Task<Results<Ok<NoticiaResponse>, ProblemHttpResult>> ObtenerAsync(
        Guid id, HttpContext httpContext, INoticiaService service, CancellationToken ct)
    {
        var resultado = await service.ObtenerAsync(id, ct);

        // Un borrador no existe para el público.
        if (resultado.IsSuccess && !resultado.Value.Publicada && !await VeBorradoresAsync(httpContext))
        {
            return Error.NoEncontrado("Noticia.NoEncontrada", $"No existe una noticia con id '{id}'.").ToProblem();
        }

        return resultado.Match<Results<Ok<NoticiaResponse>, ProblemHttpResult>>(
            noticia => TypedResults.Ok(noticia),
            error => error.ToProblem());
    }

    private static Task<bool> VeBorradoresAsync(HttpContext httpContext) =>
        Permisos.TieneAsync(httpContext, ModuloSistema.Noticias, AccionPermiso.Ver);

    private static async Task<Results<CreatedAtRoute<NoticiaResponse>, ProblemHttpResult>> CrearAsync(
        GuardarNoticiaRequest request, INoticiaService service, CancellationToken ct)
    {
        var resultado = await service.CrearAsync(request, ct);
        return resultado.Match<Results<CreatedAtRoute<NoticiaResponse>, ProblemHttpResult>>(
            noticia => TypedResults.CreatedAtRoute(noticia, "ObtenerNoticia", new { id = noticia.Id }),
            error => error.ToProblem());
    }

    private static async Task<Results<Ok<NoticiaResponse>, ProblemHttpResult>> ActualizarAsync(
        Guid id, GuardarNoticiaRequest request, INoticiaService service, CancellationToken ct)
    {
        var resultado = await service.ActualizarAsync(id, request, ct);
        return resultado.Match<Results<Ok<NoticiaResponse>, ProblemHttpResult>>(
            noticia => TypedResults.Ok(noticia),
            error => error.ToProblem());
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> EliminarAsync(
        Guid id, INoticiaService service, CancellationToken ct)
    {
        var resultado = await service.EliminarAsync(id, ct);
        return resultado.Match<Results<NoContent, ProblemHttpResult>>(
            _ => TypedResults.NoContent(),
            error => error.ToProblem());
    }
}
