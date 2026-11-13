using RabiGamaliel.Api.Infrastructure;
using RabiGamaliel.Application.Elecciones;
using RabiGamaliel.Domain.Elecciones;
using RabiGamaliel.Domain.Usuarios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RabiGamaliel.Api.Endpoints;

/// <summary>Módulo de elecciones del panel: jornadas, candidatos con su perfil y resultados en vivo.</summary>
internal static class EleccionesEndpoints
{
    public static IEndpointRouteBuilder MapEleccionesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/elecciones").WithTags("Elecciones");

        group.MapGet("/", ListarAsync)
            .WithName("ListarElecciones")
            .WithSummary("Lista las elecciones con su estado y el número de candidatos y votantes.")
            .RequierePermiso(ModuloSistema.Elecciones, AccionPermiso.Ver);

        group.MapGet("/{id:guid}", ObtenerAsync)
            .WithName("ObtenerEleccion")
            .WithSummary("Obtiene una elección.")
            .RequierePermiso(ModuloSistema.Elecciones, AccionPermiso.Ver);

        group.MapPost("/", CrearAsync)
            .WithName("CrearEleccion")
            .WithSummary("Crea una elección con sus cargos y su horario de votación.")
            .RequierePermiso(ModuloSistema.Elecciones, AccionPermiso.Crear);

        group.MapPut("/{id:guid}", ActualizarAsync)
            .WithName("ActualizarEleccion")
            .WithSummary("Actualiza nombre, horario y cargos (los cargos no cambian si ya hay votos).")
            .RequierePermiso(ModuloSistema.Elecciones, AccionPermiso.Editar);

        group.MapDelete("/{id:guid}", EliminarAsync)
            .WithName("EliminarEleccion")
            .WithSummary("Elimina una elección sin votos.")
            .RequierePermiso(ModuloSistema.Elecciones, AccionPermiso.Eliminar);

        group.MapGet("/{id:guid}/candidatos", CandidatosAsync)
            .WithName("ListarCandidatos")
            .WithSummary("Candidatos de la elección, por cargo y contienda.")
            .RequierePermiso(ModuloSistema.Elecciones, AccionPermiso.Ver);

        group.MapGet("/{id:guid}/aspirantes", AspirantesAsync)
            .WithName("BuscarAspirantes")
            .WithSummary("Estudiantes activos que pueden inscribirse al cargo.")
            .RequierePermiso(ModuloSistema.Elecciones, AccionPermiso.Crear);

        group.MapPost("/{id:guid}/candidatos", CrearCandidatoAsync)
            .WithName("CrearCandidato")
            .WithSummary("Inscribe un candidato con su perfil de campaña.")
            .RequierePermiso(ModuloSistema.Elecciones, AccionPermiso.Crear);

        group.MapPut("/{id:guid}/candidatos/{candidatoId:guid}", ActualizarCandidatoAsync)
            .WithName("ActualizarCandidato")
            .WithSummary("Actualiza el número, el lema, las propuestas y la foto del candidato.")
            .RequierePermiso(ModuloSistema.Elecciones, AccionPermiso.Editar);

        group.MapDelete("/{id:guid}/candidatos/{candidatoId:guid}", EliminarCandidatoAsync)
            .WithName("EliminarCandidato")
            .WithSummary("Retira un candidato (solo antes de que haya votos).")
            .RequierePermiso(ModuloSistema.Elecciones, AccionPermiso.Eliminar);

        group.MapGet("/{id:guid}/resultados", ResultadosAsync)
            .WithName("ResultadosEleccion")
            .WithSummary("Conteo por contienda y participación por grado, en vivo.")
            .RequierePermiso(ModuloSistema.Elecciones, AccionPermiso.Ver);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<EleccionResponse>>> ListarAsync(IEleccionService service, CancellationToken ct) =>
        TypedResults.Ok(await service.ListarAsync(ct));

    private static async Task<Results<Ok<EleccionResponse>, ProblemHttpResult>> ObtenerAsync(
        Guid id, IEleccionService service, CancellationToken ct) =>
        Responder(await service.ObtenerAsync(id, ct));

    private static async Task<Results<CreatedAtRoute<EleccionResponse>, ProblemHttpResult>> CrearAsync(
        GuardarEleccionRequest request, IEleccionService service, CancellationToken ct) =>
        (await service.CrearAsync(request, ct)).Match<Results<CreatedAtRoute<EleccionResponse>, ProblemHttpResult>>(
            eleccion => TypedResults.CreatedAtRoute(eleccion, "ObtenerEleccion", new { id = eleccion.Id }),
            error => error.ToProblem());

    private static async Task<Results<Ok<EleccionResponse>, ProblemHttpResult>> ActualizarAsync(
        Guid id, GuardarEleccionRequest request, IEleccionService service, CancellationToken ct) =>
        Responder(await service.ActualizarAsync(id, request, ct));

    private static async Task<Results<NoContent, ProblemHttpResult>> EliminarAsync(
        Guid id, IEleccionService service, CancellationToken ct) =>
        Vacio(await service.EliminarAsync(id, ct));

    private static async Task<Results<Ok<IReadOnlyList<CandidatoResponse>>, ProblemHttpResult>> CandidatosAsync(
        Guid id, IEleccionService service, CancellationToken ct) =>
        Responder(await service.CandidatosAsync(id, ct));

    private static async Task<Results<Ok<IReadOnlyList<AspiranteResponse>>, ProblemHttpResult>> AspirantesAsync(
        Guid id, CargoEleccion cargo, IEleccionService service, CancellationToken ct, string? buscar = null) =>
        Responder(await service.AspirantesAsync(id, cargo, buscar, ct));

    private static async Task<Results<Ok<CandidatoResponse>, ProblemHttpResult>> CrearCandidatoAsync(
        Guid id, CrearCandidatoRequest request, IEleccionService service, CancellationToken ct) =>
        Responder(await service.CrearCandidatoAsync(id, request, ct));

    private static async Task<Results<Ok<CandidatoResponse>, ProblemHttpResult>> ActualizarCandidatoAsync(
        Guid id, Guid candidatoId, ActualizarCandidatoRequest request, IEleccionService service, CancellationToken ct) =>
        Responder(await service.ActualizarCandidatoAsync(id, candidatoId, request, ct));

    private static async Task<Results<NoContent, ProblemHttpResult>> EliminarCandidatoAsync(
        Guid id, Guid candidatoId, IEleccionService service, CancellationToken ct) =>
        Vacio(await service.EliminarCandidatoAsync(id, candidatoId, ct));

    private static async Task<Results<Ok<ResultadosResponse>, ProblemHttpResult>> ResultadosAsync(
        Guid id, IEleccionService service, CancellationToken ct) =>
        Responder(await service.ResultadosAsync(id, ct));

    internal static Results<Ok<T>, ProblemHttpResult> Responder<T>(Application.Common.Result<T> resultado) =>
        resultado.Match<Results<Ok<T>, ProblemHttpResult>>(valor => TypedResults.Ok(valor), error => error.ToProblem());

    internal static Results<NoContent, ProblemHttpResult> Vacio<T>(Application.Common.Result<T> resultado) =>
        resultado.Match<Results<NoContent, ProblemHttpResult>>(_ => TypedResults.NoContent(), error => error.ToProblem());
}
