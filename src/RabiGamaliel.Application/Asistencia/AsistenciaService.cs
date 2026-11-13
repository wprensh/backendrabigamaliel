using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Asistencia;
using RabiGamaliel.Domain.Avisos;
using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Asistencia;

public sealed record FilaAsistencia(Guid EstudianteId, string NombreCompleto, string Documento, EstadoAsistencia? Estado, string? Observacion);

public sealed record PlanillaAsistenciaResponse(
    Guid AsignacionId, string Asignatura, int Grado, string? Grupo, DateOnly Fecha, bool Tomada, IReadOnlyList<FilaAsistencia> Filas);

public sealed record NovedadRequest(Guid EstudianteId, EstadoAsistencia Estado, string? Observacion);

public sealed record RegistrarAsistenciaRequest(DateOnly Fecha, IReadOnlyList<NovedadRequest> Novedades);

public sealed record ContactoAcudiente(string Nombre, string? Telefono, string? Parentesco);

/// <summary>Estudiante con novedad y sus acudientes, para avisarles por WhatsApp con un clic.</summary>
public sealed record ContactoNovedad(Guid EstudianteId, string Estudiante, EstadoAsistencia Estado, IReadOnlyList<ContactoAcudiente> Acudientes);

public sealed record RegistroAsistenciaResponse(PlanillaAsistenciaResponse Planilla, IReadOnlyList<ContactoNovedad> Contactos, int AvisosEnviados);

public sealed record NovedadDetalle(DateOnly Fecha, Guid AsignaturaId, EstadoAsistencia Estado, string? Observacion);

public sealed record NovedadEstudiante(DateOnly Fecha, string Asignatura, EstadoAsistencia Estado, string? Observacion);

public sealed record ResumenAsistenciaResponse(
    Guid EstudianteId, string NombreCompleto, int? Grado, string? Grupo, int Anio, int Ausencias, int Tardes, int Excusas,
    IReadOnlyList<NovedadEstudiante> Novedades);

public sealed record ConteoNovedad(Guid EstudianteId, EstadoAsistencia Estado, int Total);

public sealed record FilaReporteAsistencia(Guid EstudianteId, string NombreCompleto, string Documento, int Ausencias, int Tardes, int Excusas);

public sealed record ReporteAsistenciaResponse(int Grado, string? Grupo, DateOnly Desde, DateOnly Hasta, int ClasesRegistradas, IReadOnlyList<FilaReporteAsistencia> Filas);

public interface IAsistenciaRepository
{
    Task<TomaAsistencia?> TomaAsync(Guid asignacionId, DateOnly fecha, CancellationToken ct);

    void Agregar(TomaAsistencia toma);

    Task<List<NovedadDetalle>> NovedadesDeEstudianteAsync(Guid estudianteId, DateOnly desde, DateOnly hasta, CancellationToken ct);

    Task<List<ConteoNovedad>> ConteosAsync(IReadOnlyCollection<Guid> estudianteIds, DateOnly desde, DateOnly hasta, CancellationToken ct);

    Task<int> TomasAsync(int grado, string? grupo, DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public interface IAsistenciaService
{
    Task<Result<PlanillaAsistenciaResponse>> PlanillaAsync(MiembroCampus docente, Guid asignacionId, DateOnly fecha, CancellationToken ct);

    Task<Result<RegistroAsistenciaResponse>> RegistrarAsync(
        MiembroCampus docente, Guid asignacionId, RegistrarAsistenciaRequest request, CancellationToken ct);

    Task<Result<ResumenAsistenciaResponse>> ResumenAsync(MiembroCampus actor, Guid estudianteId, CancellationToken ct);

    Task<ReporteAsistenciaResponse> ReporteCursoAsync(int grado, string? grupo, DateOnly desde, DateOnly hasta, CancellationToken ct);
}

/// <summary>Asistencia por clase desde el campus, con aviso inmediato a los acudientes cuando un estudiante falta.</summary>
public sealed class AsistenciaService(
    IAsistenciaRepository repository,
    IAcademicoRepository academico,
    IMiembroCampusRepository miembros,
    IAccesoEstudiantes acceso,
    IAvisoRepository avisos,
    INotificador notificador,
    IUnitOfWork unitOfWork,
    TimeProvider reloj) : IAsistenciaService
{
    /// <summary>Días hacia atrás en que el docente puede registrar o corregir la asistencia.</summary>
    public const int DiasEditables = 7;

    public async Task<Result<PlanillaAsistenciaResponse>> PlanillaAsync(MiembroCampus docente, Guid asignacionId, DateOnly fecha, CancellationToken ct)
    {
        var contexto = await ContextoAsync(docente, asignacionId, fecha, ct);
        return contexto.IsSuccess ? Armar(contexto.Value, await repository.TomaAsync(asignacionId, fecha, ct)) : contexto.Error;
    }

    public async Task<Result<RegistroAsistenciaResponse>> RegistrarAsync(
        MiembroCampus docente, Guid asignacionId, RegistrarAsistenciaRequest request, CancellationToken ct)
    {
        var contexto = await ContextoAsync(docente, asignacionId, request.Fecha, ct);
        if (!contexto.IsSuccess)
        {
            return contexto.Error;
        }

        var (asignacion, asignatura, estudiantes, fecha) = contexto.Value;
        var novedades = request.Novedades ?? [];
        var delCurso = estudiantes.ToDictionary(e => e.Id);
        if (novedades.Any(n => !delCurso.ContainsKey(n.EstudianteId) || !Enum.IsDefined(n.Estado)))
        {
            return Error.Campo("Novedades", "Hay estudiantes que no pertenecen a este curso o estados no válidos.");
        }

        var toma = await repository.TomaAsync(asignacion.Id, fecha, ct);
        if (toma is null)
        {
            toma = TomaAsistencia.Crear(asignacion.Id, asignacion.AsignaturaId, docente.Id, fecha, asignacion.Grado, asignacion.Grupo);
            repository.Agregar(toma);
        }

        var cambiadas = toma.Registrar(docente.Id, estudiantes.Count, novedades.Select(n => (n.EstudianteId, n.Estado, n.Observacion)));

        // Contactos de acudientes de todos los estudiantes con novedad (para WhatsApp) y avisos de las novedades nuevas.
        var conNovedad = toma.Novedades.Select(n => n.EstudianteId).ToList();
        var vinculos = await miembros.VinculosDeEstudiantesAsync(conNovedad, ct);
        var acudientes = (await miembros.ObtenerPorIdsAsync([.. vinculos.Select(v => v.AcudienteId).Distinct()], ct))
            .Where(a => a.Activo).ToDictionary(a => a.Id);
        var inicioDelDia = new DateTimeOffset(fecha.ToDateTime(TimeOnly.MinValue), FechaColombia.Desfase).ToUniversalTime();
        var avisosEnviados = 0;

        foreach (var novedad in cambiadas.Where(n => n.Estado is EstadoAsistencia.Ausente or EstadoAsistencia.Tarde))
        {
            var estudiante = delCurso[novedad.EstudianteId];
            foreach (var vinculo in vinculos.Where(v => v.EstudianteId == novedad.EstudianteId))
            {
                if (!acudientes.TryGetValue(vinculo.AcudienteId, out var acudiente)
                    || await avisos.ExisteDesdeAsync(acudiente.Id, estudiante.Id, TipoAviso.Asistencia, inicioDelDia, ct))
                {
                    continue;
                }

                var ausente = novedad.Estado == EstadoAsistencia.Ausente;
                notificador.Notificar(
                    [acudiente], TipoAviso.Asistencia,
                    ausente ? $"{estudiante.NombreCompleto} no asistió a clase" : $"{estudiante.NombreCompleto} llegó tarde",
                    ausente
                        ? $"El {fecha:dd/MM/yyyy} {estudiante.NombreCompleto} no asistió a la clase de {asignatura}. Si tiene excusa, envíela al director de grupo."
                        : $"El {fecha:dd/MM/yyyy} {estudiante.NombreCompleto} llegó tarde a la clase de {asignatura}.",
                    "/campus/asistencia", estudiante.Id, porCorreo: ausente);
                avisosEnviados++;
            }
        }

        await unitOfWork.SaveChangesAsync(ct);

        var contactos = toma.Novedades
            .Where(n => n.Estado != EstadoAsistencia.Excusa)
            .Select(n => new ContactoNovedad(
                n.EstudianteId,
                delCurso[n.EstudianteId].NombreCompleto,
                n.Estado,
                [
                    .. vinculos.Where(v => v.EstudianteId == n.EstudianteId && acudientes.ContainsKey(v.AcudienteId))
                        .Select(v => new ContactoAcudiente(acudientes[v.AcudienteId].NombreCompleto, acudientes[v.AcudienteId].Telefono, v.Parentesco)),
                ]))
            .OrderBy(c => c.Estudiante)
            .ToList();

        return new RegistroAsistenciaResponse(Armar(contexto.Value, toma), contactos, avisosEnviados);
    }

    public async Task<Result<ResumenAsistenciaResponse>> ResumenAsync(MiembroCampus actor, Guid estudianteId, CancellationToken ct)
    {
        var visible = await acceso.EstudianteVisibleAsync(actor, estudianteId, ct);
        if (!visible.IsSuccess)
        {
            return visible.Error;
        }

        var estudiante = visible.Value;
        var anio = FechaColombia.AnioLectivo(reloj);
        var novedades = await repository.NovedadesDeEstudianteAsync(estudiante.Id, new DateOnly(anio, 1, 1), new DateOnly(anio, 12, 31), ct);
        var asignaturas = (await academico.AsignaturasAsync(soloActivas: false, ct)).ToDictionary(a => a.Id, a => a.Nombre);
        return new ResumenAsistenciaResponse(
            estudiante.Id, estudiante.NombreCompleto, estudiante.Grado, estudiante.Grupo, anio,
            novedades.Count(n => n.Estado == EstadoAsistencia.Ausente),
            novedades.Count(n => n.Estado == EstadoAsistencia.Tarde),
            novedades.Count(n => n.Estado == EstadoAsistencia.Excusa),
            [
                .. novedades.OrderByDescending(n => n.Fecha)
                    .Select(n => new NovedadEstudiante(n.Fecha, asignaturas.GetValueOrDefault(n.AsignaturaId, "(asignatura)"), n.Estado, n.Observacion)),
            ]);
    }

    public async Task<ReporteAsistenciaResponse> ReporteCursoAsync(int grado, string? grupo, DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        grupo = AcademicoService.Normalizar(grupo);
        var estudiantes = await miembros.EstudiantesDelCursoAsync(grado, grupo, ct);
        var conteos = (await repository.ConteosAsync([.. estudiantes.Select(e => e.Id)], desde, hasta, ct))
            .ToDictionary(c => (c.EstudianteId, c.Estado), c => c.Total);
        return new ReporteAsistenciaResponse(
            grado, grupo, desde, hasta, await repository.TomasAsync(grado, grupo, desde, hasta, ct),
            [
                .. estudiantes.Select(e => new FilaReporteAsistencia(
                    e.Id, e.NombreCompleto, e.Documento,
                    conteos.GetValueOrDefault((e.Id, EstadoAsistencia.Ausente)),
                    conteos.GetValueOrDefault((e.Id, EstadoAsistencia.Tarde)),
                    conteos.GetValueOrDefault((e.Id, EstadoAsistencia.Excusa)))),
            ]);
    }

    private sealed record Contexto(Domain.Academico.AsignacionDocente Asignacion, string Asignatura, List<MiembroCampus> Estudiantes, DateOnly Fecha);

    private async Task<Result<Contexto>> ContextoAsync(MiembroCampus docente, Guid asignacionId, DateOnly fecha, CancellationToken ct)
    {
        if (docente.Rol != RolCampus.Docente)
        {
            return CalificacionesService.SoloDocentes;
        }

        var asignacion = await academico.AsignacionAsync(asignacionId, ct);
        if (asignacion is null || asignacion.DocenteId != docente.Id)
        {
            return Error.NoEncontrado("Asignacion.NoEncontrada", "Esa clase no está en su carga académica.");
        }

        var hoy = FechaColombia.Hoy(reloj);
        if (fecha > hoy || fecha < hoy.AddDays(-DiasEditables))
        {
            return Error.Campo("Fecha", $"La asistencia se registra el mismo día o hasta {DiasEditables} días después.");
        }

        var asignatura = await academico.AsignaturaAsync(asignacion.AsignaturaId, ct);
        var estudiantes = await miembros.EstudiantesDelCursoAsync(asignacion.Grado, asignacion.Grupo, ct);
        return new Contexto(asignacion, asignatura?.Nombre ?? "(asignatura)", estudiantes, fecha);
    }

    private static PlanillaAsistenciaResponse Armar(Contexto contexto, TomaAsistencia? toma)
    {
        var novedades = toma?.Novedades.ToDictionary(n => n.EstudianteId) ?? [];
        return new PlanillaAsistenciaResponse(
            contexto.Asignacion.Id, contexto.Asignatura, contexto.Asignacion.Grado, contexto.Asignacion.Grupo, contexto.Fecha, toma is not null,
            [
                .. contexto.Estudiantes.Select(e =>
                {
                    var n = novedades.GetValueOrDefault(e.Id);
                    return new FilaAsistencia(e.Id, e.NombreCompleto, e.Documento, n?.Estado, n?.Observacion);
                }),
            ]);
    }
}
