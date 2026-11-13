using RabiGamaliel.Api.Infrastructure;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Usuarios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace RabiGamaliel.Api.Endpoints;

/// <summary>Módulo de comunidad del panel: estudiantes y docentes del campus y sus claves de acceso.</summary>
internal static class ComunidadEndpoints
{
    public static IEndpointRouteBuilder MapComunidadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/comunidad").WithTags("Comunidad del campus");

        group.MapGet("/", ListarAsync)
            .WithName("ListarMiembrosCampus")
            .WithSummary("Lista estudiantes y docentes, con filtros por rol, grado y grupo.")
            .RequierePermisoEnAlguno(AccionPermiso.Ver, ModuloSistema.Campus, ModuloSistema.Carnets);

        group.MapGet("/{id:guid}", ObtenerAsync)
            .WithName("ObtenerMiembroCampus")
            .WithSummary("Obtiene un estudiante o docente por su id.")
            .RequierePermiso(ModuloSistema.Campus, AccionPermiso.Ver);

        group.MapPost("/", CrearAsync)
            .WithName("CrearMiembroCampus")
            .WithSummary("Registra un estudiante o docente.")
            .RequierePermiso(ModuloSistema.Campus, AccionPermiso.Crear);

        group.MapPost("/importar", ImportarAsync)
            .WithName("ImportarMiembrosCampus")
            .WithSummary("Crea o actualiza estudiantes o docentes desde una lista.")
            .RequierePermiso(ModuloSistema.Campus, AccionPermiso.Crear);

        group.MapPut("/{id:guid}", ActualizarAsync)
            .WithName("ActualizarMiembroCampus")
            .WithSummary("Actualiza nombre, grado, grupo y estado.")
            .RequierePermiso(ModuloSistema.Campus, AccionPermiso.Editar);

        group.MapPost("/claves", GenerarClavesAsync)
            .WithName("GenerarClavesCampus")
            .WithSummary("Genera claves de acceso nuevas y las devuelve una sola vez para imprimirlas.")
            .RequierePermiso(ModuloSistema.Campus, AccionPermiso.Editar);

        group.MapGet("/{id:guid}/vinculos", async (Guid id, IAcudientesService s, CancellationToken ct) =>
                EleccionesEndpoints.Responder(await s.VinculosAsync(id, ct)))
            .WithName("VinculosMiembro")
            .WithSummary("Estudiantes de un acudiente, o acudientes de un estudiante.")
            .RequierePermiso(ModuloSistema.Campus, AccionPermiso.Ver);

        group.MapPost("/{id:guid}/vinculos", async (Guid id, VincularRequest r, IAcudientesService s, CancellationToken ct) =>
                EleccionesEndpoints.Responder(await s.VincularAsync(id, r, ct)))
            .WithName("VincularMiembro")
            .WithSummary("Vincula un acudiente con un estudiante por número de documento.")
            .RequierePermiso(ModuloSistema.Campus, AccionPermiso.Editar);

        group.MapDelete("/{id:guid}/vinculos/{vinculoId:guid}", async (Guid id, Guid vinculoId, IAcudientesService s, CancellationToken ct) =>
                EleccionesEndpoints.Vacio(await s.DesvincularAsync(id, vinculoId, ct)))
            .WithName("DesvincularMiembro")
            .RequierePermiso(ModuloSistema.Campus, AccionPermiso.Editar);

        group.MapPost("/acudientes/importar", async (ImportarAcudientesRequest r, IAcudientesService s, CancellationToken ct) =>
                EleccionesEndpoints.Responder(await s.ImportarAsync(r, ct)))
            .WithName("ImportarAcudientes")
            .WithSummary("Crea acudientes y los vincula con sus estudiantes desde una lista.")
            .RequierePermiso(ModuloSistema.Campus, AccionPermiso.Crear);

        group.MapDelete("/{id:guid}", EliminarAsync)
            .WithName("EliminarMiembroCampus")
            .WithSummary("Elimina un estudiante o docente sin historial electoral.")
            .RequierePermiso(ModuloSistema.Campus, AccionPermiso.Eliminar);

        return app;
    }

    private static async Task<Ok<PagedResult<MiembroCampusResponse>>> ListarAsync(
        IMiembroCampusService service,
        CancellationToken ct,
        string? buscar = null,
        RolCampus? rol = null,
        int? grado = null,
        string? grupo = null,
        int pagina = 1,
        int tamanoPagina = 25) =>
        TypedResults.Ok(await service.ListarAsync(new MiembroQuery(buscar, rol, grado, grupo, pagina, tamanoPagina), ct));

    private static async Task<Results<Ok<MiembroCampusResponse>, ProblemHttpResult>> ObtenerAsync(
        Guid id, IMiembroCampusService service, CancellationToken ct) =>
        (await service.ObtenerAsync(id, ct)).Match<Results<Ok<MiembroCampusResponse>, ProblemHttpResult>>(
            miembro => TypedResults.Ok(miembro),
            error => error.ToProblem());

    private static async Task<Results<CreatedAtRoute<MiembroCampusResponse>, ProblemHttpResult>> CrearAsync(
        CrearMiembroRequest request, IMiembroCampusService service, CancellationToken ct) =>
        (await service.CrearAsync(request, ct)).Match<Results<CreatedAtRoute<MiembroCampusResponse>, ProblemHttpResult>>(
            miembro => TypedResults.CreatedAtRoute(miembro, "ObtenerMiembroCampus", new { id = miembro.Id }),
            error => error.ToProblem());

    private static async Task<Results<Ok<ImportacionResponse>, ProblemHttpResult>> ImportarAsync(
        ImportarMiembrosRequest request, IMiembroCampusService service, CancellationToken ct) =>
        (await service.ImportarAsync(request, ct)).Match<Results<Ok<ImportacionResponse>, ProblemHttpResult>>(
            resultado => TypedResults.Ok(resultado),
            error => error.ToProblem());

    private static async Task<Results<Ok<MiembroCampusResponse>, ProblemHttpResult>> ActualizarAsync(
        Guid id, ActualizarMiembroRequest request, IMiembroCampusService service, CancellationToken ct) =>
        (await service.ActualizarAsync(id, request, ct)).Match<Results<Ok<MiembroCampusResponse>, ProblemHttpResult>>(
            miembro => TypedResults.Ok(miembro),
            error => error.ToProblem());

    private static async Task<Results<Ok<IReadOnlyList<ClaveGenerada>>, ProblemHttpResult>> GenerarClavesAsync(
        GenerarClavesRequest request, IMiembroCampusService service, CancellationToken ct) =>
        (await service.GenerarClavesAsync(request, ct)).Match<Results<Ok<IReadOnlyList<ClaveGenerada>>, ProblemHttpResult>>(
            claves => TypedResults.Ok(claves),
            error => error.ToProblem());

    private static async Task<Results<NoContent, ProblemHttpResult>> EliminarAsync(
        Guid id, IMiembroCampusService service, CancellationToken ct) =>
        (await service.EliminarAsync(id, ct)).Match<Results<NoContent, ProblemHttpResult>>(
            _ => TypedResults.NoContent(),
            error => error.ToProblem());
}
