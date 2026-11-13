using RabiGamaliel.Api.Infrastructure;
using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Asistencia;
using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Application.Comunicaciones;
using RabiGamaliel.Application.Convivencia;
using RabiGamaliel.Application.Simulacros;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Convivencia;
using Microsoft.AspNetCore.Http.HttpResults;
using static RabiGamaliel.Api.Endpoints.EleccionesEndpoints;

namespace RabiGamaliel.Api.Endpoints;

/// <summary>
/// Campus virtual para estudiantes, acudientes y docentes: avisos, boletines, asistencia, observador, simulacros,
/// circulares y calendario; y las planillas de notas y asistencia del docente.
/// </summary>
internal static class CampusAcademicoEndpoints
{
    public static IEndpointRouteBuilder MapCampusAcademicoEndpoints(this IEndpointRouteBuilder app)
    {
        var campus = app.MapGroup("/api/campus").WithTags("Campus virtual").RequiereSesionCampus();

        // ---------- Avisos ----------
        campus.MapGet("/avisos", async (MiembroActual m, IAvisosService s, CancellationToken ct) =>
                TypedResults.Ok(await s.MisAvisosAsync(await Yo(m, ct), ct)))
            .WithName("AvisosCampus").WithSummary("Avisos recientes y cuántos faltan por leer.");
        campus.MapPost("/avisos/leidos", async (MarcarAvisosRequest r, MiembroActual m, IAvisosService s, CancellationToken ct) =>
                Vacio(await s.MarcarLeidosAsync(await Yo(m, ct), r, ct)))
            .WithName("MarcarAvisosLeidos").WithSummary("Marca como leídos los avisos indicados (o todos).");

        // ---------- Horario ----------
        campus.MapGet("/horario", async (Guid? estudianteId, MiembroActual m, IHorarioService s, CancellationToken ct) =>
                Responder(await s.MiHorarioAsync(await Yo(m, ct), estudianteId, ct)))
            .WithName("MiHorario").WithSummary("Horario de clases: el del docente, el del curso del estudiante o el del estudiante del acudiente.");

        // ---------- Boletines ----------
        campus.MapGet("/boletines", async (MiembroActual m, IBoletinService s, CancellationToken ct) =>
                TypedResults.Ok(await s.DisponiblesAsync(await Yo(m, ct), ct)))
            .WithName("BoletinesDisponibles").WithSummary("Estudiantes y periodos con boletín publicado.");
        campus.MapGet("/boletines/{estudianteId:guid}/{periodoId:guid}",
                async (Guid estudianteId, Guid periodoId, MiembroActual m, IBoletinService s, CancellationToken ct) =>
                    Responder(await s.BoletinAsync(await Yo(m, ct), estudianteId, periodoId, ct)))
            .WithName("BoletinCampus").WithSummary("Boletín de un periodo con notas, desempeños, fallas y puesto.");

        // ---------- Asistencia y observador ----------
        campus.MapGet("/asistencia/{estudianteId:guid}", async (Guid estudianteId, MiembroActual m, IAsistenciaService s, CancellationToken ct) =>
                Responder(await s.ResumenAsync(await Yo(m, ct), estudianteId, ct)))
            .WithName("AsistenciaEstudiante").WithSummary("Inasistencias, llegadas tarde y excusas del año.");
        campus.MapGet("/observador/{estudianteId:guid}", async (Guid estudianteId, MiembroActual m, IConvivenciaService s, CancellationToken ct) =>
                Responder(await s.ObservadorAsync(await Yo(m, ct), estudianteId, ct)))
            .WithName("ObservadorCampus").WithSummary("Observador del estudiante.");
        campus.MapPost("/observador/{anotacionId:guid}/enterado", async (Guid anotacionId, MiembroActual m, IConvivenciaService s, CancellationToken ct) =>
                Responder(await s.MarcarEnteradoAsync(await Yo(m, ct), anotacionId, ct)))
            .WithName("AnotacionEnterado").WithSummary("El acudiente confirma que leyó la anotación.");

        // ---------- Simulacros ----------
        campus.MapGet("/simulacros", async (MiembroActual m, IPresentacionService s, CancellationToken ct) =>
                TypedResults.Ok(await s.DisponiblesAsync(await Yo(m, ct), ct)))
            .WithName("SimulacrosCampus").WithSummary("Simulacros del grado del estudiante (o de los cursos del docente).");
        campus.MapPost("/simulacros/{id:guid}/iniciar", async (Guid id, MiembroActual m, IPresentacionService s, CancellationToken ct) =>
                Responder(await s.IniciarAsync(await Yo(m, ct), id, ct)))
            .WithName("IniciarSimulacro").WithSummary("Empieza (o retoma) el simulacro y devuelve las preguntas sin respuestas.");
        campus.MapPut("/simulacros/{id:guid}/respuestas", async (Guid id, ResponderRequest r, MiembroActual m, IPresentacionService s, CancellationToken ct) =>
                Vacio(await s.ResponderAsync(await Yo(m, ct), id, r, ct)))
            .WithName("ResponderSimulacro").WithSummary("Guarda la respuesta de una pregunta.");
        campus.MapPost("/simulacros/{id:guid}/entregar", async (Guid id, MiembroActual m, IPresentacionService s, CancellationToken ct) =>
                Responder(await s.EntregarAsync(await Yo(m, ct), id, ct)))
            .WithName("EntregarSimulacro").WithSummary("Entrega y califica el simulacro.");
        campus.MapGet("/simulacros/{id:guid}/resultado", async (Guid id, Guid? estudianteId, MiembroActual m, IPresentacionService s, CancellationToken ct) =>
                Responder(await s.ResultadoAsync(await Yo(m, ct), id, estudianteId, ct)))
            .WithName("ResultadoSimulacro").WithSummary("Resultado por área y competencia de un estudiante.");
        campus.MapGet("/simulacros/{id:guid}/resultados", async (Guid id, MiembroActual m, IPresentacionService s, CancellationToken ct) =>
                Responder(await s.ResultadosDocenteAsync(await Yo(m, ct), id, ct)))
            .WithName("ResultadosSimulacroDocente").WithSummary("Resultados de los cursos del docente.");

        // ---------- Circulares y calendario ----------
        campus.MapGet("/circulares", async (MiembroActual m, IComunicacionesService s, CancellationToken ct) =>
                TypedResults.Ok(await s.MisCircularesAsync(await Yo(m, ct), ct)))
            .WithName("CircularesCampus").WithSummary("Circulares publicadas para quien inició sesión.");
        campus.MapPost("/circulares/{id:guid}/lectura", async (Guid id, bool? confirmar, MiembroActual m, IComunicacionesService s, CancellationToken ct) =>
                Responder(await s.LeerAsync(await Yo(m, ct), id, confirmar == true, ct)))
            .WithName("LeerCircular").WithSummary("Registra la lectura y, si se pide, la confirmación.");
        campus.MapGet("/calendario", async (DateOnly? desde, DateOnly? hasta, IComunicacionesService s, TimeProvider reloj, CancellationToken ct) =>
            {
                var inicio = desde ?? FechaColombia.Hoy(reloj).AddDays(-7);
                return TypedResults.Ok(await s.EventosAsync(inicio, hasta ?? inicio.AddDays(90), soloPublicos: false, ct));
            })
            .WithName("CalendarioCampus").WithSummary("Eventos del calendario escolar.");

        // ---------- Docente ----------
        var docente = campus.MapGroup("/docente");
        docente.MapGet("/cursos", async (MiembroActual m, ICalificacionesService s, CancellationToken ct) =>
                Responder(await s.MisCursosAsync(await Yo(m, ct), ct)))
            .WithName("CursosDocente").WithSummary("Carga académica del docente y periodos del año.");
        docente.MapGet("/clases/{asignacionId:guid}/estudiantes", async (Guid asignacionId, MiembroActual m, ICalificacionesService s, CancellationToken ct) =>
                Responder(await s.EstudiantesAsync(await Yo(m, ct), asignacionId, ct)))
            .WithName("EstudiantesClase").WithSummary("Estudiantes de una clase del docente.");
        docente.MapGet("/clases/{asignacionId:guid}/notas/{periodoId:guid}",
                async (Guid asignacionId, Guid periodoId, MiembroActual m, ICalificacionesService s, CancellationToken ct) =>
                    Responder(await s.PlanillaAsync(await Yo(m, ct), asignacionId, periodoId, ct)))
            .WithName("PlanillaNotas").WithSummary("Planilla de notas de la clase en el periodo.");
        docente.MapPut("/clases/{asignacionId:guid}/notas/{periodoId:guid}",
                async (Guid asignacionId, Guid periodoId, GuardarNotasRequest r, MiembroActual m, ICalificacionesService s, CancellationToken ct) =>
                    Responder(await s.GuardarAsync(await Yo(m, ct), asignacionId, periodoId, r, ct)))
            .WithName("GuardarNotas").WithSummary("Guarda notas, fallas y observaciones.");
        docente.MapGet("/clases/{asignacionId:guid}/asistencia",
                async (Guid asignacionId, DateOnly? fecha, MiembroActual m, IAsistenciaService s, TimeProvider reloj, CancellationToken ct) =>
                    Responder(await s.PlanillaAsync(await Yo(m, ct), asignacionId, fecha ?? FechaColombia.Hoy(reloj), ct)))
            .WithName("PlanillaAsistencia").WithSummary("Planilla de asistencia de la clase en una fecha.");
        docente.MapPut("/clases/{asignacionId:guid}/asistencia",
                async (Guid asignacionId, RegistrarAsistenciaRequest r, MiembroActual m, IAsistenciaService s, CancellationToken ct) =>
                    Responder(await s.RegistrarAsync(await Yo(m, ct), asignacionId, r, ct)))
            .WithName("RegistrarAsistencia").WithSummary("Registra las novedades de asistencia y avisa a los acudientes.");
        docente.MapPost("/anotaciones", async (CrearAnotacionRequest r, MiembroActual m, IConvivenciaService s, CancellationToken ct) =>
                Responder(await s.CrearDesdeCampusAsync(await Yo(m, ct), r, ct)))
            .WithName("CrearAnotacionDocente").WithSummary("Registra una anotación en el observador de un estudiante del docente.");
        docente.MapGet("/clases/{asignacionId:guid}/anotaciones",
                async (Guid asignacionId, TipoAnotacion? tipo, string? buscar, int? pagina, int? tamanoPagina, MiembroActual m,
                    IConvivenciaService s, CancellationToken ct) =>
                    Responder(await s.HistorialCursoAsync(
                        await Yo(m, ct), asignacionId, new HistorialCursoQuery(tipo, buscar, pagina ?? 1, tamanoPagina ?? 20), ct)))
            .WithName("HistorialObservadorCurso").WithSummary("Historial del observador del curso de una clase del docente.");

        return app;
    }

    /// <summary>Miembro de la sesión (el filtro del grupo ya comprobó que existe).</summary>
    private static async Task<MiembroCampus> Yo(MiembroActual miembro, CancellationToken ct) => (await miembro.ObtenerAsync(ct))!;
}
