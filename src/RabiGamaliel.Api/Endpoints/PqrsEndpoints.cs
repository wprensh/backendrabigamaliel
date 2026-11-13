using RabiGamaliel.Api.Infrastructure;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Application.Pqrs;
using RabiGamaliel.Domain.Pqrs;
using RabiGamaliel.Domain.Usuarios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RabiGamaliel.Api.Endpoints;

/// <summary>Formulario PQRS de la sección "Contacto" y su gestión administrativa.</summary>
internal static class PqrsEndpoints
{
    public const string RateLimitPolicy = "pqrs-publico";

    public static IEndpointRouteBuilder MapPqrsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pqrs").WithTags("PQRS");

        // Endpoint público: limitado por IP para evitar spam del formulario.
        group.MapPost("/", RadicarAsync)
            .WithName("RadicarPqrs")
            .WithSummary("Radica una petición, queja, reclamo o sugerencia.")
            .RequireRateLimiting(RateLimitPolicy);

        // Contienen datos personales (Ley 1581): según los permisos del módulo PQRS.
        group.MapGet("/", ListarAsync)
            .WithName("ListarPqrs")
            .WithSummary("Lista las solicitudes PQRS (panel administrativo).")
            .RequierePermiso(ModuloSistema.Pqrs, AccionPermiso.Ver);

        group.MapPut("/{id:guid}/respuesta", ResponderAsync)
            .WithName("ResponderPqrs")
            .WithSummary("Registra la respuesta a una solicitud.")
            .RequierePermiso(ModuloSistema.Pqrs, AccionPermiso.Editar);

        return app;
    }

    private static async Task<Results<Created<PqrsRadicadaResponse>, ProblemHttpResult>> RadicarAsync(
        RadicarPqrsRequest request, IPqrsService service, CancellationToken ct)
    {
        var resultado = await service.RadicarAsync(request, ct);
        return resultado.Match<Results<Created<PqrsRadicadaResponse>, ProblemHttpResult>>(
            radicada => TypedResults.Created($"/api/pqrs/{radicada.Id}", radicada),
            error => error.ToProblem());
    }

    private static async Task<Ok<PagedResult<PqrsResponse>>> ListarAsync(
        IPqrsService service, CancellationToken ct, EstadoPqrs? estado = null, int pagina = 1, int tamanoPagina = 20) =>
        TypedResults.Ok(await service.ListarAsync(new PqrsQuery(estado, pagina, tamanoPagina), ct));

    private static async Task<Results<Ok<PqrsResponse>, ProblemHttpResult>> ResponderAsync(
        Guid id, ResponderPqrsRequest request, IPqrsService service, CancellationToken ct)
    {
        var resultado = await service.ResponderAsync(id, request, ct);
        return resultado.Match<Results<Ok<PqrsResponse>, ProblemHttpResult>>(
            pqrs => TypedResults.Ok(pqrs),
            error => error.ToProblem());
    }
}
