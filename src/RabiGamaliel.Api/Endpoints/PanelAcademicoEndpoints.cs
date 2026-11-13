using RabiGamaliel.Api.Infrastructure;
using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Asistencia;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Application.Convivencia;
using RabiGamaliel.Application.Indicadores;
using RabiGamaliel.Application.Simulacros;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Convivencia;
using RabiGamaliel.Domain.Simulacros;
using RabiGamaliel.Domain.Usuarios;
using static RabiGamaliel.Api.Endpoints.EleccionesEndpoints;

namespace RabiGamaliel.Api.Endpoints;

/// <summary>Módulos del panel: académico (notas, boletines, asistencia), convivencia, simulacros e indicadores.</summary>
internal static class PanelAcademicoEndpoints
{
    public sealed record PersonaResumen(Guid Id, string NombreCompleto, string Documento, int? Grado, string? Grupo);

    public static IEndpointRouteBuilder MapPanelAcademicoEndpoints(this IEndpointRouteBuilder app)
    {
        MapAcademico(app.MapGroup("/api/academico").WithTags("Académico"));
        MapConvivencia(app.MapGroup("/api/convivencia").WithTags("Convivencia"));
        MapSimulacros(app.MapGroup("/api/simulacros").WithTags("Simulacros"));

        app.MapGet("/api/indicadores", async (IIndicadoresService s, CancellationToken ct) => TypedResults.Ok(await s.TableroAsync(ct)))
            .WithTags("Indicadores").WithName("TableroIndicadores").WithSummary("Tablero del rector.")
            .RequierePermiso(ModuloSistema.Indicadores, AccionPermiso.Ver);

        // Público: el QR del carnet apunta a /carnet/{codigo} en el sitio web.
        app.MapGet("/api/carnets/{codigo}", async (string codigo, IAcudientesService s, CancellationToken ct) =>
                Responder(await s.VerificarCarnetAsync(codigo, ct)))
            .WithTags("Carnet digital").WithName("VerificarCarnet").WithSummary("Verifica un carnet estudiantil o docente.")
            .RequireRateLimiting(CertificadosEndpoints.RateLimitPolicy);

        return app;
    }

    private static void MapAcademico(RouteGroupBuilder g)
    {
        const ModuloSistema M = ModuloSistema.Academico;

        g.MapGet("/asignaturas", async (IAcademicoService s, CancellationToken ct) => TypedResults.Ok(await s.AsignaturasAsync(ct)))
            .WithName("ListarAsignaturas").RequierePermiso(M, AccionPermiso.Ver);
        g.MapPost("/asignaturas", async (GuardarAsignaturaRequest r, IAcademicoService s, CancellationToken ct) => Responder(await s.CrearAsignaturaAsync(r, ct)))
            .WithName("CrearAsignatura").RequierePermiso(M, AccionPermiso.Crear);
        g.MapPut("/asignaturas/{id:guid}", async (Guid id, GuardarAsignaturaRequest r, IAcademicoService s, CancellationToken ct) =>
                Responder(await s.ActualizarAsignaturaAsync(id, r, ct)))
            .WithName("ActualizarAsignatura").RequierePermiso(M, AccionPermiso.Editar);
        g.MapDelete("/asignaturas/{id:guid}", async (Guid id, IAcademicoService s, CancellationToken ct) => Vacio(await s.EliminarAsignaturaAsync(id, ct)))
            .WithName("EliminarAsignatura").RequierePermiso(M, AccionPermiso.Eliminar);

        g.MapGet("/periodos", async (int? anio, IAcademicoService s, CancellationToken ct) => TypedResults.Ok(await s.PeriodosAsync(anio, ct)))
            .WithName("ListarPeriodos").RequierePermiso(M, AccionPermiso.Ver);
        g.MapPost("/periodos", async (GuardarPeriodoRequest r, IAcademicoService s, CancellationToken ct) => Responder(await s.CrearPeriodoAsync(r, ct)))
            .WithName("CrearPeriodo").RequierePermiso(M, AccionPermiso.Crear);
        g.MapPut("/periodos/{id:guid}", async (Guid id, GuardarPeriodoRequest r, IAcademicoService s, CancellationToken ct) =>
                Responder(await s.ActualizarPeriodoAsync(id, r, ct)))
            .WithName("ActualizarPeriodo").WithSummary("Cambia fechas, abre o cierra notas y publica el boletín (avisa a las familias).")
            .RequierePermiso(M, AccionPermiso.Editar);
        g.MapDelete("/periodos/{id:guid}", async (Guid id, IAcademicoService s, CancellationToken ct) => Vacio(await s.EliminarPeriodoAsync(id, ct)))
            .WithName("EliminarPeriodo").RequierePermiso(M, AccionPermiso.Eliminar);

        g.MapGet("/asignaciones", async (int anio, Guid? docenteId, int? grado, string? grupo, IAcademicoService s, CancellationToken ct) =>
                TypedResults.Ok(await s.AsignacionesAsync(anio, docenteId, grado, grupo, ct)))
            .WithName("ListarAsignaciones").RequierePermiso(M, AccionPermiso.Ver);
        g.MapPost("/asignaciones", async (CrearAsignacionRequest r, IAcademicoService s, CancellationToken ct) => Responder(await s.CrearAsignacionAsync(r, ct)))
            .WithName("CrearAsignacion").RequierePermiso(M, AccionPermiso.Crear);
        g.MapPut("/asignaciones/{id:guid}", async (Guid id, ActualizarAsignacionRequest r, IAcademicoService s, CancellationToken ct) =>
                Responder(await s.ActualizarAsignacionAsync(id, r, ct)))
            .WithName("ActualizarAsignacion").RequierePermiso(M, AccionPermiso.Editar);
        g.MapDelete("/asignaciones/{id:guid}", async (Guid id, IAcademicoService s, CancellationToken ct) => Vacio(await s.EliminarAsignacionAsync(id, ct)))
            .WithName("EliminarAsignacion").RequierePermiso(M, AccionPermiso.Eliminar);

        g.MapGet("/cursos", async (IAcademicoService s, CancellationToken ct) => TypedResults.Ok(await s.CursosAsync(ct)))
            .WithName("ListarCursos").RequierePermisoEnAlguno(AccionPermiso.Ver, M, ModuloSistema.Convivencia);
        g.MapGet("/docentes", async (IMiembroCampusRepository miembros, CancellationToken ct) =>
                TypedResults.Ok((await miembros.ActivosPorRolAsync(RolCampus.Docente, ct))
                    .OrderBy(d => d.NombreCompleto)
                    .Select(d => new PersonaResumen(d.Id, d.NombreCompleto, d.Documento, null, null))
                    .ToList()))
            .WithName("ListarDocentesAcademico").RequierePermiso(M, AccionPermiso.Ver);

        g.MapGet("/horario", async (int anio, Guid? docenteId, int? grado, string? grupo, IHorarioService s, CancellationToken ct) =>
                Responder(await s.HorarioAsync(anio, docenteId, grado, grupo, ct)))
            .WithName("HorarioSemanal").WithSummary("Horario de un docente o de un curso, con las horas programadas de cada clase.")
            .RequierePermiso(M, AccionPermiso.Ver);
        g.MapPost("/horario", async (GuardarBloqueRequest r, IHorarioService s, CancellationToken ct) => Responder(await s.CrearBloqueAsync(r, ct)))
            .WithName("CrearBloqueHorario").WithSummary("Ubica una clase de la carga docente en el horario (sin cruces de docente ni de curso).")
            .RequierePermiso(M, AccionPermiso.Crear);
        g.MapPut("/horario/{id:guid}", async (Guid id, MoverBloqueRequest r, IHorarioService s, CancellationToken ct) =>
                Responder(await s.MoverBloqueAsync(id, r, ct)))
            .WithName("MoverBloqueHorario").RequierePermiso(M, AccionPermiso.Editar);
        g.MapDelete("/horario/{id:guid}", async (Guid id, IHorarioService s, CancellationToken ct) => Vacio(await s.EliminarBloqueAsync(id, ct)))
            .WithName("EliminarBloqueHorario").RequierePermiso(M, AccionPermiso.Eliminar);

        g.MapGet("/consolidado", async (Guid periodoId, int grado, string? grupo, IAcademicoService s, CancellationToken ct) =>
                Responder(await s.ConsolidadoAsync(periodoId, grado, grupo, ct)))
            .WithName("ConsolidadoNotas").WithSummary("Sábana de notas del curso con promedios y puestos.").RequierePermiso(M, AccionPermiso.Ver);
        g.MapGet("/boletines", async (Guid periodoId, int grado, string? grupo, IAcademicoService s, CancellationToken ct) =>
                Responder(await s.BoletinesCursoAsync(periodoId, grado, grupo, ct)))
            .WithName("BoletinesCurso").WithSummary("Boletines de todo un curso, para imprimir.").RequierePermiso(M, AccionPermiso.Ver);
        g.MapGet("/asistencia", async (int grado, string? grupo, DateOnly? desde, DateOnly? hasta, IAsistenciaService s, TimeProvider reloj, CancellationToken ct) =>
            {
                var hoy = FechaColombia.Hoy(reloj);
                return TypedResults.Ok(await s.ReporteCursoAsync(grado, grupo, desde ?? new DateOnly(hoy.Year, 1, 1), hasta ?? hoy, ct));
            })
            .WithName("ReporteAsistencia").WithSummary("Inasistencias por estudiante de un curso.")
            .RequierePermisoEnAlguno(AccionPermiso.Ver, M, ModuloSistema.Convivencia);
    }

    private static void MapConvivencia(RouteGroupBuilder g)
    {
        const ModuloSistema M = ModuloSistema.Convivencia;

        g.MapGet("/", async (
                IConvivenciaService s, CancellationToken ct, TipoAnotacion? tipo = null, EstadoCaso? estado = null, int? grado = null,
                string? grupo = null, string? buscar = null, bool soloSituaciones = false, int pagina = 1, int tamanoPagina = 20) =>
                TypedResults.Ok(await s.ListarAsync(new AnotacionQuery(tipo, estado, grado, grupo, buscar, soloSituaciones, pagina, tamanoPagina), ct)))
            .WithName("ListarAnotaciones").RequierePermiso(M, AccionPermiso.Ver);
        g.MapGet("/{id:guid}", async (Guid id, IConvivenciaService s, CancellationToken ct) => Responder(await s.ObtenerAsync(id, ct)))
            .WithName("ObtenerAnotacion").RequierePermiso(M, AccionPermiso.Ver);
        g.MapGet("/estudiantes", async (string? buscar, IMiembroCampusRepository miembros, CancellationToken ct) =>
                TypedResults.Ok((await miembros.BuscarEstudiantesAsync(buscar, [.. Enumerable.Range(0, 12)], 20, ct))
                    .Select(e => new PersonaResumen(e.Id, e.NombreCompleto, e.Documento, e.Grado, e.Grupo)).ToList()))
            .WithName("BuscarEstudiantesConvivencia").RequierePermiso(M, AccionPermiso.Ver);
        g.MapGet("/estudiantes/{id:guid}/observador", async (Guid id, IConvivenciaService s, CancellationToken ct) => Responder(await s.ObservadorAdminAsync(id, ct)))
            .WithName("ObservadorPanel").RequierePermiso(M, AccionPermiso.Ver);
        g.MapPost("/", async (CrearAnotacionRequest r, HttpContext http, IConvivenciaService s, CancellationToken ct) =>
                Responder(await s.CrearDesdePanelAsync(await AutorAsync(http, ct), r, ct)))
            .WithName("CrearAnotacionPanel").RequierePermiso(M, AccionPermiso.Crear);
        g.MapPost("/{id:guid}/seguimientos", async (Guid id, SeguimientoRequest r, HttpContext http, IConvivenciaService s, CancellationToken ct) =>
                Responder(await s.AgregarSeguimientoAsync(id, await AutorAsync(http, ct), r, ct)))
            .WithName("SeguimientoAnotacion").WithSummary("Registra un seguimiento y cambia el estado del caso.").RequierePermiso(M, AccionPermiso.Editar);
    }

    private static void MapSimulacros(RouteGroupBuilder g)
    {
        const ModuloSistema M = ModuloSistema.Simulacros;

        g.MapGet("/", async (ISimulacroService s, CancellationToken ct) => TypedResults.Ok(await s.ListarAsync(ct)))
            .WithName("ListarSimulacros").RequierePermiso(M, AccionPermiso.Ver);
        g.MapGet("/{id:guid}", async (Guid id, ISimulacroService s, CancellationToken ct) => Responder(await s.ObtenerAsync(id, ct)))
            .WithName("ObtenerSimulacro").RequierePermiso(M, AccionPermiso.Ver);
        g.MapPost("/", async (GuardarSimulacroRequest r, ISimulacroService s, CancellationToken ct) => Responder(await s.CrearAsync(r, ct)))
            .WithName("CrearSimulacro").RequierePermiso(M, AccionPermiso.Crear);
        g.MapPut("/{id:guid}", async (Guid id, GuardarSimulacroRequest r, ISimulacroService s, CancellationToken ct) => Responder(await s.ActualizarAsync(id, r, ct)))
            .WithName("ActualizarSimulacro").RequierePermiso(M, AccionPermiso.Editar);
        g.MapDelete("/{id:guid}", async (Guid id, ISimulacroService s, CancellationToken ct) => Vacio(await s.EliminarAsync(id, ct)))
            .WithName("EliminarSimulacro").RequierePermiso(M, AccionPermiso.Eliminar);
        g.MapPut("/{id:guid}/preguntas", async (Guid id, GuardarPreguntasRequest r, ISimulacroService s, CancellationToken ct) =>
                Responder(await s.GuardarPreguntasAsync(id, r, ct)))
            .WithName("GuardarPreguntas").WithSummary("Reemplaza las preguntas del simulacro.").RequierePermiso(M, AccionPermiso.Editar);
        g.MapPost("/{id:guid}/preguntas/banco", async (Guid id, AgregarDesdeBancoRequest r, IBancoPreguntasService s, CancellationToken ct) =>
                Responder(await s.AgregarASimulacroAsync(id, r, ct)))
            .WithName("AgregarDesdeBanco").WithSummary("Agrega al simulacro preguntas del banco, elegidas o al azar por área.")
            .RequierePermiso(M, AccionPermiso.Editar);

        g.MapGet("/banco", async (AreaEvaluada? area, int? grado, DificultadPregunta? dificultad, string? texto, IBancoPreguntasService s, CancellationToken ct) =>
                TypedResults.Ok(await s.ListarAsync(new FiltroBanco(area, grado, dificultad, texto), ct)))
            .WithName("ListarBancoPreguntas").WithSummary("Banco de preguntas tipo Saber del colegio.").RequierePermiso(M, AccionPermiso.Ver);
        g.MapPost("/banco", async (GuardarPreguntaBancoRequest r, IBancoPreguntasService s, CancellationToken ct) => Responder(await s.CrearAsync(r, ct)))
            .WithName("CrearPreguntaBanco").RequierePermiso(M, AccionPermiso.Crear);
        g.MapPost("/banco/lote", async (GuardarLoteBancoRequest r, IBancoPreguntasService s, CancellationToken ct) => Responder(await s.CrearLoteAsync(r, ct)))
            .WithName("CrearLoteBanco").WithSummary("Carga varias preguntas al banco a la vez.").RequierePermiso(M, AccionPermiso.Crear);
        g.MapPut("/banco/{id:guid}", async (Guid id, GuardarPreguntaBancoRequest r, IBancoPreguntasService s, CancellationToken ct) =>
                Responder(await s.ActualizarAsync(id, r, ct)))
            .WithName("ActualizarPreguntaBanco").RequierePermiso(M, AccionPermiso.Editar);
        g.MapDelete("/banco/{id:guid}", async (Guid id, IBancoPreguntasService s, CancellationToken ct) => Vacio(await s.EliminarAsync(id, ct)))
            .WithName("EliminarPreguntaBanco").RequierePermiso(M, AccionPermiso.Eliminar);
        g.MapGet("/{id:guid}/resultados", async (Guid id, ISimulacroService s, CancellationToken ct) => Responder(await s.ResultadosAsync(id, null, ct)))
            .WithName("ResultadosSimulacro").RequierePermiso(M, AccionPermiso.Ver);
    }

    /// <summary>Nombre de quien escribe desde el panel (para el observador).</summary>
    internal static async Task<string> AutorAsync(HttpContext http, CancellationToken ct)
    {
        var usuario = await http.RequestServices.GetRequiredService<UsuarioActual>().ObtenerAsync(ct);
        return usuario?.NombreCompleto ?? "Coordinación";
    }
}
