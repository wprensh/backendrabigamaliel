using RabiGamaliel.Api.Infrastructure;
using RabiGamaliel.Application.Aula;
using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Application.Evaluacion;
using RabiGamaliel.Application.Indicadores;
using RabiGamaliel.Application.Mensajeria;
using RabiGamaliel.Application.Planeacion;
using RabiGamaliel.Application.Reportes;
using RabiGamaliel.Domain.Aula;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Planeacion;
using RabiGamaliel.Domain.Usuarios;
using Microsoft.AspNetCore.Http.HttpResults;
using static RabiGamaliel.Api.Endpoints.EleccionesEndpoints;

namespace RabiGamaliel.Api.Endpoints;

/// <summary>
/// Notificaciones push, alerta temprana, aula virtual, planeador docente, recuperaciones y comisiones de evaluación,
/// reportes oficiales y mensajería docente–familia.
/// </summary>
internal static class MejorasEndpoints
{
    public static IEndpointRouteBuilder MapMejorasEndpoints(this IEndpointRouteBuilder app)
    {
        MapCampus(app.MapGroup("/api/campus").WithTags("Campus virtual").RequiereSesionCampus());
        MapPanel(app);
        return app;
    }

    private static void MapCampus(RouteGroupBuilder campus)
    {
        // ---------- Notificaciones push ----------
        campus.MapGet("/push", async (MiembroActual m, IPushService s, CancellationToken ct) => TypedResults.Ok(await s.ConfiguracionAsync(await Yo(m, ct), ct)))
            .WithName("ConfiguracionPush").WithSummary("Clave pública VAPID y cuántos dispositivos del miembro reciben notificaciones.");
        campus.MapPost("/push", async (SuscribirPushRequest r, MiembroActual m, IPushService s, CancellationToken ct) =>
                Responder(await s.SuscribirAsync(await Yo(m, ct), r, ct)))
            .WithName("SuscribirPush").WithSummary("Registra este dispositivo para recibir notificaciones push.");
        campus.MapDelete("/push", async (string endpoint, MiembroActual m, IPushService s, CancellationToken ct) =>
                Vacio(await s.DesuscribirAsync(await Yo(m, ct), endpoint, ct)))
            .WithName("DesuscribirPush").WithSummary("Deja de enviar notificaciones a este dispositivo.");

        // ---------- Aula virtual ----------
        campus.MapGet("/aula", async (Guid? estudianteId, MiembroActual m, IAulaService s, CancellationToken ct) =>
                Responder(await s.MiAulaAsync(await Yo(m, ct), estudianteId, ct)))
            .WithName("MiAula").WithSummary("Material y tareas de las clases del estudiante, con su entrega y nota.");
        campus.MapPost("/aula/actividades/{id:guid}/entrega", async (Guid id, EntregarRequest r, MiembroActual m, IAulaService s, CancellationToken ct) =>
                Responder(await s.EntregarAsync(await Yo(m, ct), id, r, ct)))
            .WithName("EntregarTarea").WithSummary("El estudiante entrega (o reemplaza) su tarea.");
        campus.MapPost("/aula/archivos", SubirArchivoAsync)
            .WithName("SubirArchivoAula").WithSummary("Sube un PDF, imagen u Office de máximo 5 MB (guía del docente o entrega).")
            .DisableAntiforgery();
        campus.MapGet("/aula/archivos/{id:guid}", DescargarArchivoAsync)
            .WithName("DescargarArchivoAula").WithSummary("Descarga un archivo del aula (solo quien puede ver la actividad o la entrega).");

        var docente = campus.MapGroup("/docente");
        docente.MapGet("/clases/{asignacionId:guid}/aula", async (Guid asignacionId, MiembroActual m, IAulaService s, CancellationToken ct) =>
                Responder(await s.ActividadesClaseAsync(await Yo(m, ct), asignacionId, ct)))
            .WithName("ActividadesClase").WithSummary("Material y tareas publicados en una clase del docente.");
        docente.MapPost("/clases/{asignacionId:guid}/aula", async (Guid asignacionId, GuardarActividadRequest r, MiembroActual m, IAulaService s, CancellationToken ct) =>
                Responder(await s.CrearAsync(await Yo(m, ct), asignacionId, r, ct)))
            .WithName("CrearActividad").WithSummary("Publica material o una tarea y avisa a estudiantes y acudientes.");
        docente.MapPut("/aula/{id:guid}", async (Guid id, GuardarActividadRequest r, MiembroActual m, IAulaService s, CancellationToken ct) =>
                Responder(await s.ActualizarAsync(await Yo(m, ct), id, r, ct)))
            .WithName("ActualizarActividad");
        docente.MapDelete("/aula/{id:guid}", async (Guid id, MiembroActual m, IAulaService s, CancellationToken ct) =>
                Vacio(await s.EliminarAsync(await Yo(m, ct), id, ct)))
            .WithName("EliminarActividad");
        docente.MapGet("/aula/{id:guid}/entregas", async (Guid id, MiembroActual m, IAulaService s, CancellationToken ct) =>
                Responder(await s.EntregasAsync(await Yo(m, ct), id, ct)))
            .WithName("EntregasActividad").WithSummary("Entregas de todos los estudiantes del curso.");
        docente.MapPut("/aula/{id:guid}/entregas", async (Guid id, CalificarEntregasRequest r, MiembroActual m, IAulaService s, CancellationToken ct) =>
                Responder(await s.CalificarAsync(await Yo(m, ct), id, r, ct)))
            .WithName("CalificarEntregas").WithSummary("Califica y comenta las entregas (avisa a la familia).");
        docente.MapPost("/clases/{asignacionId:guid}/aula/planilla/{periodoId:guid}",
                async (Guid asignacionId, Guid periodoId, MiembroActual m, IAulaService s, CancellationToken ct) =>
                    Responder(await s.PasarAPlanillaAsync(await Yo(m, ct), asignacionId, periodoId, ct)))
            .WithName("AulaAPlanilla").WithSummary("Lleva a la planilla de notas el promedio de las tareas calificables del periodo.");

        // ---------- Planeador ----------
        docente.MapGet("/planeaciones", async (MiembroActual m, IPlaneacionService s, CancellationToken ct) =>
                Responder(await s.MisPlaneacionesAsync(await Yo(m, ct), ct)))
            .WithName("MisPlaneaciones");
        docente.MapPost("/planeaciones", async (GuardarPlaneacionRequest r, MiembroActual m, IPlaneacionService s, CancellationToken ct) =>
                Responder(await s.GuardarAsync(await Yo(m, ct), null, r, ct)))
            .WithName("CrearPlaneacion").WithSummary("Guarda una planeación en borrador o la envía a coordinación.");
        docente.MapPut("/planeaciones/{id:guid}", async (Guid id, GuardarPlaneacionRequest r, MiembroActual m, IPlaneacionService s, CancellationToken ct) =>
                Responder(await s.GuardarAsync(await Yo(m, ct), id, r, ct)))
            .WithName("ActualizarPlaneacion");
        docente.MapDelete("/planeaciones/{id:guid}", async (Guid id, MiembroActual m, IPlaneacionService s, CancellationToken ct) =>
                Vacio(await s.EliminarAsync(await Yo(m, ct), id, ct)))
            .WithName("EliminarPlaneacion");
        docente.MapGet("/clases/{asignacionId:guid}/malla", async (Guid asignacionId, MiembroActual m, IPlaneacionService s, CancellationToken ct) =>
                Responder(await s.MallaAsync(await Yo(m, ct), asignacionId, ct)))
            .WithName("MallaClase").WithSummary("Plan de estudios (estándares, DBA, contenidos) de la asignatura y el grado.");

        // ---------- Recuperaciones ----------
        docente.MapGet("/clases/{asignacionId:guid}/recuperaciones/{periodoId:guid}",
                async (Guid asignacionId, Guid periodoId, MiembroActual m, IEvaluacionService s, CancellationToken ct) =>
                    Responder(await s.RecuperacionesClaseAsync(await Yo(m, ct), asignacionId, periodoId, ct)))
            .WithName("RecuperacionesClase").WithSummary("Estudiantes que perdieron el periodo y su plan de mejoramiento.");
        docente.MapPost("/clases/{asignacionId:guid}/recuperaciones/{periodoId:guid}",
                async (Guid asignacionId, Guid periodoId, AsignarPlanRequest r, MiembroActual m, IEvaluacionService s, CancellationToken ct) =>
                    Responder(await s.AsignarPlanesAsync(await Yo(m, ct), asignacionId, periodoId, r, ct)))
            .WithName("AsignarPlanMejoramiento").WithSummary("Asigna el plan de mejoramiento y avisa a la familia.");
        docente.MapPut("/recuperaciones/{id:guid}", async (Guid id, CalificarRecuperacionRequest r, MiembroActual m, IEvaluacionService s, CancellationToken ct) =>
                Responder(await s.CalificarAsync(await Yo(m, ct), id, r, ct)))
            .WithName("CalificarRecuperacion").WithSummary("Registra la nota de la recuperación y actualiza la nota del periodo (máximo 3.0 si aprueba).");
        campus.MapGet("/recuperaciones", async (Guid? estudianteId, MiembroActual m, IEvaluacionService s, CancellationToken ct) =>
                Responder(await s.MisRecuperacionesAsync(await Yo(m, ct), estudianteId, ct)))
            .WithName("MisRecuperaciones").WithSummary("Planes de mejoramiento y recuperaciones del estudiante.");

        // ---------- Mensajería ----------
        campus.MapGet("/mensajes", async (MiembroActual m, IMensajeriaService s, CancellationToken ct) => TypedResults.Ok(await s.BandejaAsync(await Yo(m, ct), ct)))
            .WithName("BandejaMensajes");
        campus.MapGet("/mensajes/contactos", async (MiembroActual m, IMensajeriaService s, CancellationToken ct) =>
                TypedResults.Ok(await s.ContactosAsync(await Yo(m, ct), ct)))
            .WithName("ContactosMensajes").WithSummary("Docentes del estudiante (familias) o familias de los cursos (docentes).");
        campus.MapPost("/mensajes", async (IniciarConversacionRequest r, MiembroActual m, IMensajeriaService s, CancellationToken ct) =>
                Responder(await s.IniciarAsync(await Yo(m, ct), r, ct)))
            .WithName("IniciarConversacion").RequireRateLimiting(CertificadosEndpoints.RateLimitPolicy);
        campus.MapGet("/mensajes/{id:guid}", async (Guid id, MiembroActual m, IMensajeriaService s, CancellationToken ct) =>
                Responder(await s.LeerAsync(await Yo(m, ct), id, ct)))
            .WithName("LeerConversacion");
        campus.MapPost("/mensajes/{id:guid}", async (Guid id, EnviarMensajeRequest r, MiembroActual m, IMensajeriaService s, CancellationToken ct) =>
                Responder(await s.ResponderAsync(await Yo(m, ct), id, r, ct)))
            .WithName("ResponderConversacion");
        campus.MapPost("/mensajes/{id:guid}/cerrar", async (Guid id, MiembroActual m, IMensajeriaService s, CancellationToken ct) =>
                Responder(await s.CerrarAsync(await Yo(m, ct), id, ct)))
            .WithName("CerrarConversacion");
    }

    private static void MapPanel(IEndpointRouteBuilder app)
    {
        // ---------- Alerta temprana ----------
        app.MapGet("/api/indicadores/alertas", async (int? grado, string? grupo, IAlertasService s, CancellationToken ct) =>
                TypedResults.Ok(await s.CalcularAsync(grado, grupo, ct)))
            .WithTags("Indicadores").WithName("AlertasTempranas").WithSummary("Estudiantes en riesgo de deserción o de perder el año.")
            .RequierePermisoEnAlguno(AccionPermiso.Ver, ModuloSistema.Indicadores, ModuloSistema.Convivencia, ModuloSistema.Evaluacion);

        // ---------- Plan de estudios y revisión de planeaciones ----------
        var planeacion = app.MapGroup("/api/planeacion").WithTags("Planeación");
        const ModuloSistema P = ModuloSistema.Planeacion;
        planeacion.MapGet("/planes", async (Guid? asignaturaId, int? grado, IPlaneacionService s, CancellationToken ct) =>
                TypedResults.Ok(await s.PlanesAsync(asignaturaId, grado, ct)))
            .WithName("ListarPlanesEstudio").RequierePermiso(P, AccionPermiso.Ver);
        planeacion.MapPut("/planes", async (GuardarPlanEstudiosRequest r, HttpContext http, IPlaneacionService s, CancellationToken ct) =>
                Responder(await s.GuardarPlanAsync(r, await PanelAcademicoEndpoints.AutorAsync(http, ct), ct)))
            .WithName("GuardarPlanEstudios").WithSummary("Crea o actualiza la malla de una asignatura, grado y periodo.").RequierePermiso(P, AccionPermiso.Editar);
        planeacion.MapDelete("/planes/{id:guid}", async (Guid id, IPlaneacionService s, CancellationToken ct) => Vacio(await s.EliminarPlanAsync(id, ct)))
            .WithName("EliminarPlanEstudios").RequierePermiso(P, AccionPermiso.Eliminar);
        planeacion.MapGet("/planeaciones", async (EstadoPlaneacion? estado, Guid? docenteId, IPlaneacionService s, CancellationToken ct) =>
                TypedResults.Ok(await s.PlaneacionesAsync(estado, docenteId, ct)))
            .WithName("ListarPlaneaciones").RequierePermiso(P, AccionPermiso.Ver);
        planeacion.MapPost("/planeaciones/{id:guid}/revision", async (Guid id, RevisarPlaneacionRequest r, HttpContext http, IPlaneacionService s, CancellationToken ct) =>
                Responder(await s.RevisarAsync(id, r, await PanelAcademicoEndpoints.AutorAsync(http, ct), ct)))
            .WithName("RevisarPlaneacion").WithSummary("Aprueba o devuelve con observaciones (avisa al docente).").RequierePermiso(P, AccionPermiso.Editar);

        // ---------- Recuperaciones y comisión de evaluación ----------
        var evaluacion = app.MapGroup("/api/evaluacion").WithTags("Evaluación y promoción");
        const ModuloSistema E = ModuloSistema.Evaluacion;
        evaluacion.MapGet("/recuperaciones", async (int anio, Guid? periodoId, int? grado, string? grupo, IEvaluacionService s, CancellationToken ct) =>
                TypedResults.Ok(await s.RecuperacionesAsync(anio, periodoId, grado, grupo, ct)))
            .WithName("ListarRecuperaciones").RequierePermiso(E, AccionPermiso.Ver);
        evaluacion.MapGet("/candidatos", async (int anio, Guid? periodoId, int grado, string? grupo, IEvaluacionService s, CancellationToken ct) =>
                Responder(await s.CandidatosAsync(anio, periodoId, grado, grupo, ct)))
            .WithName("CandidatosComision").WithSummary("Estudiantes del curso con asignaturas perdidas, fallas y decisión sugerida.")
            .RequierePermiso(E, AccionPermiso.Ver);
        evaluacion.MapGet("/actas", async (int anio, IEvaluacionService s, CancellationToken ct) => TypedResults.Ok(await s.ActasAsync(anio, ct)))
            .WithName("ListarActasComision").RequierePermiso(E, AccionPermiso.Ver);
        evaluacion.MapGet("/actas/{id:guid}", async (Guid id, IEvaluacionService s, CancellationToken ct) => Responder(await s.ActaAsync(id, ct)))
            .WithName("ObtenerActaComision").RequierePermiso(E, AccionPermiso.Ver);
        evaluacion.MapPost("/actas", async (GuardarActaRequest r, IEvaluacionService s, CancellationToken ct) => Responder(await s.GuardarActaAsync(null, r, ct)))
            .WithName("CrearActaComision").RequierePermiso(E, AccionPermiso.Crear);
        evaluacion.MapPut("/actas/{id:guid}", async (Guid id, GuardarActaRequest r, IEvaluacionService s, CancellationToken ct) =>
                Responder(await s.GuardarActaAsync(id, r, ct)))
            .WithName("ActualizarActaComision").RequierePermiso(E, AccionPermiso.Editar);
        evaluacion.MapDelete("/actas/{id:guid}", async (Guid id, IEvaluacionService s, CancellationToken ct) => Vacio(await s.EliminarActaAsync(id, ct)))
            .WithName("EliminarActaComision").RequierePermiso(E, AccionPermiso.Eliminar);

        // ---------- Reportes oficiales ----------
        var reportes = app.MapGroup("/api/reportes").WithTags("Reportes oficiales");
        const ModuloSistema R = ModuloSistema.Reportes;
        reportes.MapGet("/simat", async (int? grado, IReportesService s, CancellationToken ct) => TypedResults.Ok(await s.SimatAsync(grado, ct)))
            .WithName("ReporteSimat").WithSummary("Matrícula en formato tipo SIMAT para conciliar con la Secretaría de Educación.")
            .RequierePermiso(R, AccionPermiso.Ver);
        reportes.MapGet("/libro-final", async (int anio, int grado, string? grupo, IReportesService s, CancellationToken ct) =>
                Responder(await s.LibroFinalAsync(anio, grado, grupo, ct)))
            .WithName("LibroFinal").WithSummary("Libro final de calificaciones del curso.").RequierePermiso(R, AccionPermiso.Ver);
        reportes.MapPost("/certificado-estudios", async (CertificadoEstudiosRequest r, IReportesService s, CancellationToken ct) =>
                Responder(await s.CertificadoEstudiosAsync(r, ct)))
            .WithName("CertificadoEstudios").WithSummary("Expide el certificado de estudios con las notas del año y código de verificación.")
            .RequierePermiso(R, AccionPermiso.Crear);
    }

    private static async Task<Results<Ok<ArchivoResponse>, ProblemHttpResult>> SubirArchivoAsync(
        IFormFile archivo, MiembroActual m, IAulaService s, CancellationToken ct)
    {
        if (archivo.Length is 0 or > ArchivoAula.TamanoMaximoBytes)
        {
            return Error.Campo("Archivo", "El archivo debe pesar como máximo 5 MB.").ToProblem();
        }

        using var memoria = new MemoryStream();
        await archivo.CopyToAsync(memoria, ct);
        return Responder(await s.SubirArchivoAsync(
            await Yo(m, ct), new ArchivoSubido(archivo.FileName, archivo.ContentType ?? string.Empty, memoria.ToArray()), ct));
    }

    private static async Task<Results<FileContentHttpResult, ProblemHttpResult>> DescargarArchivoAsync(
        Guid id, MiembroActual m, IAulaService s, CancellationToken ct) =>
        (await s.DescargarAsync(await Yo(m, ct), id, ct)).Match<Results<FileContentHttpResult, ProblemHttpResult>>(
            archivo => TypedResults.File(archivo.Contenido, archivo.TipoContenido, archivo.NombreArchivo),
            error => error.ToProblem());

    private static async Task<MiembroCampus> Yo(MiembroActual miembro, CancellationToken ct) => (await miembro.ObtenerAsync(ct))!;
}
