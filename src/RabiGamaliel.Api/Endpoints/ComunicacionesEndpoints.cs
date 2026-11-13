using RabiGamaliel.Api.Infrastructure;
using RabiGamaliel.Application.Admisiones;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Application.Comunicaciones;
using RabiGamaliel.Domain.Admisiones;
using RabiGamaliel.Domain.Usuarios;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using static RabiGamaliel.Api.Endpoints.EleccionesEndpoints;

namespace RabiGamaliel.Api.Endpoints;

/// <summary>Circulares, calendario escolar y pre-matrícula en línea.</summary>
internal static class ComunicacionesEndpoints
{
    public static IEndpointRouteBuilder MapComunicacionesEndpoints(this IEndpointRouteBuilder app)
    {
        const ModuloSistema C = ModuloSistema.Circulares;
        const ModuloSistema E = ModuloSistema.Calendario;

        var circulares = app.MapGroup("/api/circulares").WithTags("Circulares");
        circulares.MapGet("/", async (IComunicacionesService s, CancellationToken ct) => TypedResults.Ok(await s.CircularesAsync(ct)))
            .WithName("ListarCirculares").RequierePermiso(C, AccionPermiso.Ver);
        circulares.MapGet("/{id:guid}", async (Guid id, IComunicacionesService s, CancellationToken ct) => Responder(await s.CircularAsync(id, ct)))
            .WithName("ObtenerCircular").RequierePermiso(C, AccionPermiso.Ver);
        circulares.MapGet("/{id:guid}/pendientes", async (Guid id, IComunicacionesService s, CancellationToken ct) => Responder(await s.PendientesAsync(id, ct)))
            .WithName("PendientesCircular").WithSummary("Destinatarios que no la han leído o confirmado.").RequierePermiso(C, AccionPermiso.Ver);
        circulares.MapPost("/", async (GuardarCircularRequest r, IComunicacionesService s, CancellationToken ct) => Responder(await s.CrearCircularAsync(r, ct)))
            .WithName("CrearCircular").RequierePermiso(C, AccionPermiso.Crear);
        circulares.MapPut("/{id:guid}", async (Guid id, GuardarCircularRequest r, IComunicacionesService s, CancellationToken ct) =>
                Responder(await s.ActualizarCircularAsync(id, r, ct)))
            .WithName("ActualizarCircular").RequierePermiso(C, AccionPermiso.Editar);
        circulares.MapPost("/{id:guid}/publicacion", async (Guid id, IComunicacionesService s, CancellationToken ct) => Responder(await s.PublicarAsync(id, ct)))
            .WithName("PublicarCircular").WithSummary("Publica la circular en el campus y la envía por correo.").RequierePermiso(C, AccionPermiso.Editar);
        circulares.MapDelete("/{id:guid}", async (Guid id, IComunicacionesService s, CancellationToken ct) => Vacio(await s.EliminarCircularAsync(id, ct)))
            .WithName("EliminarCircular").RequierePermiso(C, AccionPermiso.Eliminar);

        var calendario = app.MapGroup("/api/calendario").WithTags("Calendario escolar");
        calendario.MapGet("/", async (DateOnly? desde, DateOnly? hasta, IComunicacionesService s, TimeProvider reloj, CancellationToken ct) =>
            {
                var inicio = desde ?? FechaColombia.Hoy(reloj).AddDays(-7);
                return TypedResults.Ok(await s.EventosAsync(inicio, hasta ?? inicio.AddDays(120), soloPublicos: true, ct));
            })
            .WithName("CalendarioPublico").WithSummary("Eventos públicos del calendario escolar.");
        calendario.MapGet("/eventos", async (DateOnly desde, DateOnly hasta, IComunicacionesService s, CancellationToken ct) =>
                TypedResults.Ok(await s.EventosAsync(desde, hasta, soloPublicos: false, ct)))
            .WithName("ListarEventos").RequierePermiso(E, AccionPermiso.Ver);
        calendario.MapPost("/eventos", async (GuardarEventoRequest r, IComunicacionesService s, CancellationToken ct) => Responder(await s.CrearEventoAsync(r, ct)))
            .WithName("CrearEvento").RequierePermiso(E, AccionPermiso.Crear);
        calendario.MapPut("/eventos/{id:guid}", async (Guid id, GuardarEventoRequest r, IComunicacionesService s, CancellationToken ct) =>
                Responder(await s.ActualizarEventoAsync(id, r, ct)))
            .WithName("ActualizarEvento").RequierePermiso(E, AccionPermiso.Editar);
        calendario.MapDelete("/eventos/{id:guid}", async (Guid id, IComunicacionesService s, CancellationToken ct) => Vacio(await s.EliminarEventoAsync(id, ct)))
            .WithName("EliminarEvento").RequierePermiso(E, AccionPermiso.Eliminar);

        MapAdmisiones(app.MapGroup("/api/admisiones").WithTags("Admisiones"));
        return app;
    }

    private static void MapAdmisiones(RouteGroupBuilder g)
    {
        const ModuloSistema A = ModuloSistema.Admisiones;

        // Públicos, limitados por IP.
        g.MapPost("/", async (RadicarAdmisionRequest r, IAdmisionesService s, CancellationToken ct) => Responder(await s.RadicarAsync(r, ct)))
            .WithName("RadicarAdmision").WithSummary("Solicitud de cupo desde el sitio web.").RequireRateLimiting(PqrsEndpoints.RateLimitPolicy);
        g.MapGet("/estado", async (string radicado, string documento, IAdmisionesService s, CancellationToken ct) =>
                Responder(await s.ConsultarAsync(radicado, documento, ct)))
            .WithName("EstadoAdmision").WithSummary("Estado de la solicitud por radicado y documento del aspirante.")
            .RequireRateLimiting(CertificadosEndpoints.RateLimitPolicy);
        g.MapPost("/documentos", SubirDocumentoAsync)
            .WithName("SubirDocumentoAdmision").WithSummary("Carga un PDF o una imagen (máx. 3 MB) a la solicitud.")
            .DisableAntiforgery()
            .RequireRateLimiting(CertificadosEndpoints.RateLimitPolicy);

        g.MapGet("/", async (
                IAdmisionesService s, CancellationToken ct, EstadoAdmision? estado = null, int? anioLectivo = null, int? grado = null,
                string? buscar = null, int pagina = 1, int tamanoPagina = 20) =>
                TypedResults.Ok(await s.ListarAsync(new AdmisionQuery(estado, anioLectivo, grado, buscar, pagina, tamanoPagina), ct)))
            .WithName("ListarAdmisiones").RequierePermiso(A, AccionPermiso.Ver);
        g.MapGet("/{id:guid}", async (Guid id, IAdmisionesService s, CancellationToken ct) => Responder(await s.ObtenerAsync(id, ct)))
            .WithName("ObtenerAdmision").RequierePermiso(A, AccionPermiso.Ver);
        g.MapGet("/{id:guid}/documentos/{documentoId:guid}", DescargarAsync)
            .WithName("DescargarDocumentoAdmision").RequierePermiso(A, AccionPermiso.Ver);
        g.MapPut("/{id:guid}/estado", async (Guid id, CambiarEstadoAdmisionRequest r, IAdmisionesService s, CancellationToken ct) =>
                Responder(await s.CambiarEstadoAsync(id, r, ct)))
            .WithName("CambiarEstadoAdmision").WithSummary("Cambia el estado y avisa a la familia por correo.").RequierePermiso(A, AccionPermiso.Editar);
        g.MapPost("/{id:guid}/matricula", async (Guid id, MatricularRequest r, IAdmisionesService s, CancellationToken ct) =>
                Responder(await s.MatricularAsync(id, r, ct)))
            .WithName("MatricularAdmision").WithSummary("Crea al estudiante y al acudiente en el campus.").RequierePermiso(A, AccionPermiso.Crear);
        g.MapDelete("/{id:guid}", async (Guid id, IAdmisionesService s, CancellationToken ct) => Vacio(await s.EliminarAsync(id, ct)))
            .WithName("EliminarAdmision").RequierePermiso(A, AccionPermiso.Eliminar);
    }

    private static async Task<Results<Ok<DocumentoAdmisionResponse>, ProblemHttpResult>> SubirDocumentoAsync(
        [FromForm] string radicado, [FromForm] string documento, [FromForm] string tipo, IFormFile archivo,
        IAdmisionesService s, CancellationToken ct)
    {
        if (!Enum.TryParse<TipoDocumentoAdmision>(tipo, ignoreCase: true, out var tipoDocumento) || !Enum.IsDefined(tipoDocumento))
        {
            return Error.Campo("Tipo", "El tipo de documento no es válido.").ToProblem();
        }

        if (archivo.Length is 0 or > DocumentoAdmision.TamanoMaximoBytes)
        {
            return Error.Campo("Archivo", "El archivo debe pesar como máximo 3 MB.").ToProblem();
        }

        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria, ct);
        return Responder(await s.SubirDocumentoAsync(
            radicado, documento, tipoDocumento, new ArchivoAdmision(archivo.FileName, archivo.ContentType ?? string.Empty, memoria.ToArray()), ct));
    }

    private static async Task<Results<FileContentHttpResult, ProblemHttpResult>> DescargarAsync(
        Guid id, Guid documentoId, IAdmisionesService s, CancellationToken ct) =>
        (await s.DescargarAsync(id, documentoId, ct)).Match<Results<FileContentHttpResult, ProblemHttpResult>>(
            archivo => TypedResults.File(archivo.Contenido, archivo.TipoContenido, archivo.NombreArchivo),
            error => error.ToProblem());
}
