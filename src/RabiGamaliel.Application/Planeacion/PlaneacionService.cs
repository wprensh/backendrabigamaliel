using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Avisos;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Planeacion;

namespace RabiGamaliel.Application.Planeacion;

public interface IPlaneacionRepository
{
    Task<List<PlanEstudios>> PlanesAsync(Guid? asignaturaId, int? grado, CancellationToken ct);

    Task<PlanEstudios?> PlanAsync(Guid asignaturaId, int grado, int periodo, CancellationToken ct);

    Task<PlanEstudios?> PlanAsync(Guid id, CancellationToken ct);

    void Agregar(PlanEstudios plan);

    void Eliminar(PlanEstudios plan);

    Task<List<PlaneacionClase>> PlaneacionesAsync(Guid? docenteId, IReadOnlyCollection<Guid>? asignacionIds, EstadoPlaneacion? estado, CancellationToken ct);

    Task<PlaneacionClase?> PlaneacionAsync(Guid id, CancellationToken ct);

    void Agregar(PlaneacionClase planeacion);

    void Eliminar(PlaneacionClase planeacion);
}

public sealed record PlanEstudiosResponse(
    Guid Id, Guid AsignaturaId, string Asignatura, string Area, int Grado, int Periodo, string? Estandares, string? Dba, string? Competencias,
    string? Contenidos, string? Indicadores, string ActualizadoPor, DateTimeOffset? ActualizadoEn);

public sealed record GuardarPlanEstudiosRequest(
    Guid AsignaturaId, int Grado, int Periodo, string? Estandares, string? Dba, string? Competencias, string? Contenidos, string? Indicadores);

public sealed record PlaneacionResponse(
    Guid Id, Guid AsignacionId, string Asignatura, int Grado, string? Grupo, Guid DocenteId, string Docente, Guid? PeriodoId, DateOnly Semana,
    string Tema, string? Aprendizajes, string? Actividades, string? Recursos, string? Evaluacion, EstadoPlaneacion Estado, DateTimeOffset? EnviadaEn,
    string? Observacion, string? RevisadaPor, DateTimeOffset? RevisadaEn, DateTimeOffset CreadoEn);

public sealed record GuardarPlaneacionRequest(
    Guid AsignacionId, Guid? PeriodoId, DateOnly Semana, string Tema, string? Aprendizajes, string? Actividades, string? Recursos, string? Evaluacion,
    bool Enviar = false);

public sealed record RevisarPlaneacionRequest(bool Aprobar, string? Observacion);

public interface IPlaneacionService
{
    Task<IReadOnlyList<PlanEstudiosResponse>> PlanesAsync(Guid? asignaturaId, int? grado, CancellationToken ct);

    Task<Result<PlanEstudiosResponse>> GuardarPlanAsync(GuardarPlanEstudiosRequest request, string autor, CancellationToken ct);

    Task<Result<Unit>> EliminarPlanAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<PlaneacionResponse>> PlaneacionesAsync(EstadoPlaneacion? estado, Guid? docenteId, CancellationToken ct);

    Task<Result<PlaneacionResponse>> RevisarAsync(Guid id, RevisarPlaneacionRequest request, string revisor, CancellationToken ct);

    Task<Result<IReadOnlyList<PlaneacionResponse>>> MisPlaneacionesAsync(MiembroCampus docente, CancellationToken ct);

    Task<Result<PlaneacionResponse>> GuardarAsync(MiembroCampus docente, Guid? id, GuardarPlaneacionRequest request, CancellationToken ct);

    Task<Result<Unit>> EliminarAsync(MiembroCampus docente, Guid id, CancellationToken ct);

    /// <summary>Malla curricular de la asignatura y el grado de una clase del docente.</summary>
    Task<Result<IReadOnlyList<PlanEstudiosResponse>>> MallaAsync(MiembroCampus docente, Guid asignacionId, CancellationToken ct);
}

/// <summary>Plan de estudios (mallas) y planeador semanal de clases con revisión de coordinación.</summary>
public sealed class PlaneacionService(
    IPlaneacionRepository repository,
    IAcademicoRepository academico,
    IMiembroCampusRepository miembros,
    INotificador notificador,
    IUnitOfWork unitOfWork,
    TimeProvider reloj) : IPlaneacionService
{
    private static readonly Error NoEncontrada = Error.NoEncontrado("Planeacion.NoEncontrada", "No encontramos esa planeación.");

    // ---------- Plan de estudios ----------

    public async Task<IReadOnlyList<PlanEstudiosResponse>> PlanesAsync(Guid? asignaturaId, int? grado, CancellationToken ct)
    {
        var asignaturas = (await academico.AsignaturasAsync(soloActivas: false, ct)).ToDictionary(a => a.Id);
        return
        [
            .. (await repository.PlanesAsync(asignaturaId, grado, ct))
                .OrderBy(p => asignaturas.GetValueOrDefault(p.AsignaturaId)?.Orden).ThenBy(p => p.Grado).ThenBy(p => p.Periodo)
                .Select(p => Plan(p, asignaturas)),
        ];
    }

    public async Task<Result<PlanEstudiosResponse>> GuardarPlanAsync(GuardarPlanEstudiosRequest request, string autor, CancellationToken ct)
    {
        var r = request;
        var asignatura = await academico.AsignaturaAsync(r.AsignaturaId, ct);
        if (asignatura is null)
        {
            return Error.Campo("AsignaturaId", "Elija la asignatura.");
        }

        if (!MiembroCampus.GradoValido(r.Grado))
        {
            return Error.Campo("Grado", "Elija un grado de transición a once.");
        }

        if (r.Periodo is < 1 or > 4)
        {
            return Error.Campo("Periodo", "El periodo va de 1 a 4.");
        }

        if (new[] { r.Estandares, r.Dba, r.Competencias, r.Contenidos, r.Indicadores }.Any(t => t?.Length > PlanEstudios.TextoMaxLength))
        {
            return Error.Campo("Contenidos", $"Cada campo admite máximo {PlanEstudios.TextoMaxLength} caracteres.");
        }

        var plan = await repository.PlanAsync(r.AsignaturaId, r.Grado, r.Periodo, ct);
        if (plan is null)
        {
            plan = PlanEstudios.Crear(r.AsignaturaId, r.Grado, r.Periodo);
            repository.Agregar(plan);
        }

        plan.Actualizar(r.Estandares, r.Dba, r.Competencias, r.Contenidos, r.Indicadores, autor);
        await unitOfWork.SaveChangesAsync(ct);
        return Plan(plan, new Dictionary<Guid, Asignatura> { [asignatura.Id] = asignatura });
    }

    public async Task<Result<Unit>> EliminarPlanAsync(Guid id, CancellationToken ct)
    {
        var plan = await repository.PlanAsync(id, ct);
        if (plan is null)
        {
            return Error.NoEncontrado("Plan.NoEncontrado", "No encontramos esa malla.");
        }

        repository.Eliminar(plan);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    // ---------- Revisión (coordinación) ----------

    public async Task<IReadOnlyList<PlaneacionResponse>> PlaneacionesAsync(EstadoPlaneacion? estado, Guid? docenteId, CancellationToken ct)
    {
        // Coordinación no revisa borradores: solo lo enviado, aprobado o devuelto.
        var lista = (await repository.PlaneacionesAsync(docenteId, null, estado, ct)).Where(p => p.Estado != EstadoPlaneacion.Borrador).ToList();
        return await ArmarAsync(lista, ct);
    }

    public async Task<Result<PlaneacionResponse>> RevisarAsync(Guid id, RevisarPlaneacionRequest request, string revisor, CancellationToken ct)
    {
        var planeacion = await repository.PlaneacionAsync(id, ct);
        if (planeacion is null)
        {
            return NoEncontrada;
        }

        if (planeacion.Estado != EstadoPlaneacion.Enviada)
        {
            return Error.Conflicto("Planeacion.NoEnviada", "Esta planeación ya fue revisada.");
        }

        if (!request.Aprobar && string.IsNullOrWhiteSpace(request.Observacion))
        {
            return Error.Campo("Observacion", "Escriba qué debe ajustar el docente.");
        }

        if (request.Observacion?.Length > PlaneacionClase.ObservacionMaxLength)
        {
            return Error.Campo("Observacion", $"La observación admite máximo {PlaneacionClase.ObservacionMaxLength} caracteres.");
        }

        planeacion.Revisar(request.Aprobar, request.Observacion, revisor, reloj.GetUtcNow());
        if (await miembros.ObtenerPorIdAsync(planeacion.DocenteId, ct) is { } docente)
        {
            notificador.Notificar(
                [docente], TipoAviso.Planeacion,
                request.Aprobar ? "Planeación aprobada" : "Planeación devuelta con observaciones",
                $"«{planeacion.Tema}» (semana del {planeacion.Semana:dd/MM/yyyy}).{(planeacion.Observacion is { } o ? $" {o}" : string.Empty)}",
                "/campus/planeador", null, porCorreo: !request.Aprobar);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return (await ArmarAsync([planeacion], ct))[0];
    }

    // ---------- Docente ----------

    public async Task<Result<IReadOnlyList<PlaneacionResponse>>> MisPlaneacionesAsync(MiembroCampus docente, CancellationToken ct)
    {
        if (docente.Rol != RolCampus.Docente)
        {
            return CalificacionesService.SoloDocentes;
        }

        return Resultado.Exito<IReadOnlyList<PlaneacionResponse>>(await ArmarAsync(await repository.PlaneacionesAsync(docente.Id, null, null, ct), ct));
    }

    public async Task<Result<PlaneacionResponse>> GuardarAsync(MiembroCampus docente, Guid? id, GuardarPlaneacionRequest request, CancellationToken ct)
    {
        var r = request;
        if (docente.Rol != RolCampus.Docente)
        {
            return CalificacionesService.SoloDocentes;
        }

        var asignacion = await academico.AsignacionAsync(r.AsignacionId, ct);
        if (asignacion is null || asignacion.DocenteId != docente.Id)
        {
            return Error.Campo("AsignacionId", "Elija una de sus clases.");
        }

        if (string.IsNullOrWhiteSpace(r.Tema) || r.Tema.Trim().Length > PlaneacionClase.TemaMaxLength)
        {
            return Error.Campo("Tema", $"Escriba el tema (máximo {PlaneacionClase.TemaMaxLength} caracteres).");
        }

        if (new[] { r.Aprendizajes, r.Actividades, r.Recursos, r.Evaluacion }.Any(t => t?.Length > PlaneacionClase.TextoMaxLength))
        {
            return Error.Campo("Actividades", $"Cada campo admite máximo {PlaneacionClase.TextoMaxLength} caracteres.");
        }

        if (r.Enviar && string.IsNullOrWhiteSpace(r.Actividades))
        {
            return Error.Campo("Actividades", "Describa las actividades de la clase antes de enviarla.");
        }

        if (r.PeriodoId is { } periodoId && (await academico.PeriodoAsync(periodoId, ct))?.Anio != asignacion.Anio)
        {
            return Error.Campo("PeriodoId", "Elija un periodo del año de la clase.");
        }

        PlaneacionClase planeacion;
        if (id is { } existente)
        {
            var encontrada = await repository.PlaneacionAsync(existente, ct);
            if (encontrada is null || encontrada.DocenteId != docente.Id)
            {
                return NoEncontrada;
            }

            if (!encontrada.Editable)
            {
                return Error.Conflicto("Planeacion.Enviada", "La planeación ya fue enviada a coordinación; espere la revisión.");
            }

            planeacion = encontrada;
        }
        else
        {
            planeacion = PlaneacionClase.Crear(asignacion.Id, docente.Id);
            repository.Agregar(planeacion);
        }

        planeacion.Actualizar(r.PeriodoId, r.Semana, r.Tema, r.Aprendizajes, r.Actividades, r.Recursos, r.Evaluacion);
        if (r.Enviar)
        {
            planeacion.Enviar(reloj.GetUtcNow());
        }

        await unitOfWork.SaveChangesAsync(ct);
        return (await ArmarAsync([planeacion], ct))[0];
    }

    public async Task<Result<Unit>> EliminarAsync(MiembroCampus docente, Guid id, CancellationToken ct)
    {
        var planeacion = await repository.PlaneacionAsync(id, ct);
        if (planeacion is null || planeacion.DocenteId != docente.Id)
        {
            return NoEncontrada;
        }

        if (planeacion.Estado != EstadoPlaneacion.Borrador)
        {
            return Error.Conflicto("Planeacion.Enviada", "Solo se eliminan borradores.");
        }

        repository.Eliminar(planeacion);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Result<IReadOnlyList<PlanEstudiosResponse>>> MallaAsync(MiembroCampus docente, Guid asignacionId, CancellationToken ct)
    {
        var asignacion = await academico.AsignacionAsync(asignacionId, ct);
        if (docente.Rol != RolCampus.Docente || asignacion is null || asignacion.DocenteId != docente.Id)
        {
            return Error.NoEncontrado("Asignacion.NoEncontrada", "Esa clase no está en su carga académica.");
        }

        return Resultado.Exito(await PlanesAsync(asignacion.AsignaturaId, asignacion.Grado, ct));
    }

    // ---------- Apoyo ----------

    private static PlanEstudiosResponse Plan(PlanEstudios p, IReadOnlyDictionary<Guid, Asignatura> asignaturas)
    {
        var a = asignaturas.GetValueOrDefault(p.AsignaturaId);
        return new PlanEstudiosResponse(
            p.Id, p.AsignaturaId, a?.Nombre ?? "(asignatura)", a?.Area ?? string.Empty, p.Grado, p.Periodo, p.Estandares, p.Dba, p.Competencias,
            p.Contenidos, p.Indicadores, p.ActualizadoPor, p.ModificadoEn ?? p.CreadoEn);
    }

    private async Task<List<PlaneacionResponse>> ArmarAsync(List<PlaneacionClase> lista, CancellationToken ct)
    {
        if (lista.Count == 0)
        {
            return [];
        }

        var asignaciones = new Dictionary<Guid, AsignacionDocente>();
        foreach (var id in lista.Select(p => p.AsignacionId).Distinct())
        {
            if (await academico.AsignacionAsync(id, ct) is { } a)
            {
                asignaciones[id] = a;
            }
        }

        var asignaturas = (await academico.AsignaturasAsync(soloActivas: false, ct)).ToDictionary(a => a.Id, a => a.Nombre);
        var docentes = (await miembros.ObtenerPorIdsAsync([.. lista.Select(p => p.DocenteId).Distinct()], ct)).ToDictionary(d => d.Id, d => d.NombreCompleto);
        return
        [
            .. lista.OrderByDescending(p => p.Semana).ThenByDescending(p => p.CreadoEn).Select(p =>
            {
                var a = asignaciones.GetValueOrDefault(p.AsignacionId);
                return new PlaneacionResponse(
                    p.Id, p.AsignacionId, a is null ? "(clase eliminada)" : asignaturas.GetValueOrDefault(a.AsignaturaId, "(asignatura)"), a?.Grado ?? 0,
                    a?.Grupo, p.DocenteId, docentes.GetValueOrDefault(p.DocenteId, "(docente)"), p.PeriodoId, p.Semana, p.Tema, p.Aprendizajes,
                    p.Actividades, p.Recursos, p.Evaluacion, p.Estado, p.EnviadaEn, p.Observacion, p.RevisadaPor, p.RevisadaEn, p.CreadoEn);
            }),
        ];
    }
}
