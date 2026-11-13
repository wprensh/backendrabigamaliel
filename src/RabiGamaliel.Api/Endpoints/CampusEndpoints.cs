using RabiGamaliel.Api.Infrastructure;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Elecciones;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RabiGamaliel.Api.Endpoints;

/// <summary>Campus virtual: ingreso de estudiantes y docentes, tarjetón, voto y resultados.</summary>
internal static class CampusEndpoints
{
    public const string RateLimitPolicy = "inicio-sesion-campus";

    public static IEndpointRouteBuilder MapCampusEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/campus").WithTags("Campus virtual");

        group.MapPost("/iniciar-sesion", IniciarSesionAsync)
            .WithName("IniciarSesionCampus")
            .WithSummary("Ingreso con número de documento y clave de acceso.")
            .RequireRateLimiting(RateLimitPolicy);

        var sesion = group.MapGroup("/").RequiereSesionCampus();

        sesion.MapGet("/perfil", PerfilAsync)
            .WithName("PerfilCampus")
            .WithSummary("Datos de quien inició sesión en el campus.");

        sesion.MapGet("/elecciones", EleccionesAsync)
            .WithName("EleccionesCampus")
            .WithSummary("Elecciones recientes y si el estudiante ya votó.");

        sesion.MapGet("/elecciones/{id:guid}/tarjeton", TarjetonAsync)
            .WithName("TarjetonCampus")
            .WithSummary("Candidatos de las contiendas en las que vota el estudiante (todas, para un docente).");

        sesion.MapPost("/elecciones/{id:guid}/votar", VotarAsync)
            .WithName("VotarCampus")
            .WithSummary("Registra el voto del estudiante: uno por cargo, o en blanco.");

        sesion.MapGet("/elecciones/{id:guid}/resultados", ResultadosAsync)
            .WithName("ResultadosCampus")
            .WithSummary("Resultados al cerrar la votación; durante la jornada, los docentes ven la participación.");

        return app;
    }

    private static async Task<Results<Ok<SesionCampusResponse>, ProblemHttpResult>> IniciarSesionAsync(
        IniciarSesionCampusRequest request, ICampusAutenticacionService service, CancellationToken ct) =>
        EleccionesEndpoints.Responder(await service.IniciarSesionAsync(request, ct));

    private static async Task<Ok<PerfilCampusResponse>> PerfilAsync(MiembroActual miembroActual, IAcudientesService acudientes, CancellationToken ct) =>
        TypedResults.Ok(await acudientes.PerfilAsync((await miembroActual.ObtenerAsync(ct))!, ct));

    private static async Task<Ok<IReadOnlyList<EleccionCampusResponse>>> EleccionesAsync(
        MiembroActual miembroActual, IVotacionService service, CancellationToken ct) =>
        TypedResults.Ok(await service.EleccionesAsync(await miembroActual.VotanteAsync(ct), ct));

    private static async Task<Results<Ok<TarjetonResponse>, ProblemHttpResult>> TarjetonAsync(
        Guid id, MiembroActual miembroActual, IVotacionService service, CancellationToken ct) =>
        EleccionesEndpoints.Responder(await service.TarjetonAsync(id, await miembroActual.VotanteAsync(ct), ct));

    private static async Task<Results<NoContent, ProblemHttpResult>> VotarAsync(
        Guid id, VotarRequest request, MiembroActual miembroActual, IVotacionService service, CancellationToken ct) =>
        EleccionesEndpoints.Vacio(await service.VotarAsync(id, await miembroActual.VotanteAsync(ct), request, ct));

    private static async Task<Results<Ok<ResultadosResponse>, ProblemHttpResult>> ResultadosAsync(
        Guid id, MiembroActual miembroActual, IVotacionService service, CancellationToken ct) =>
        EleccionesEndpoints.Responder(await service.ResultadosAsync(id, await miembroActual.VotanteAsync(ct), ct));
}
