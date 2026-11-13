using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Avisos;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Evaluacion;

namespace RabiGamaliel.Application.Evaluacion;

public interface IEvaluacionRepository
{
    Task<List<Recuperacion>> RecuperacionesAsync(
        IReadOnlyCollection<Guid>? estudianteIds, IReadOnlyCollection<Guid>? periodoIds, Guid? asignaturaId, CancellationToken ct);

    Task<Recuperacion?> RecuperacionAsync(Guid id, CancellationToken ct);

    void Agregar(Recuperacion recuperacion);

    Task<List<ActaComision>> ActasAsync(int anio, CancellationToken ct);

    Task<ActaComision?> ActaAsync(Guid id, CancellationToken ct);

    Task<int> SiguienteNumeroActaAsync(int anio, CancellationToken ct);

    void Agregar(ActaComision acta);

    void Eliminar(ActaComision acta);

    Task<List<DecisionEstudiante>> DecisionesAsync(IReadOnlyCollection<Guid> actaIds, CancellationToken ct);

    void Agregar(DecisionEstudiante decision);

    void Eliminar(DecisionEstudiante decision);
}

// ---------- Contratos ----------

public sealed record RecuperacionResponse(
    Guid Id, Guid EstudianteId, string Estudiante, string Curso, Guid AsignaturaId, string Asignatura, Guid PeriodoId, string Periodo,
    decimal NotaOriginal, string Plan, DateOnly? FechaLimite, decimal? NotaRecuperacion, decimal? NotaDefinitiva, string? Observacion,
    EstadoRecuperacion Estado, string RegistradaPor, DateTimeOffset CreadoEn);

public sealed record FilaRecuperacionClase(Guid EstudianteId, string NombreCompleto, string Documento, decimal? NotaPeriodo, RecuperacionResponse? Recuperacion);

public sealed record RecuperacionesClaseResponse(
    Guid AsignacionId, string Asignatura, int Grado, string? Grupo, PeriodoResponse Periodo, IReadOnlyList<FilaRecuperacionClase> Filas);

public sealed record AsignarPlanRequest(IReadOnlyList<Guid> EstudianteIds, string Plan, DateOnly? FechaLimite);

public sealed record CalificarRecuperacionRequest(decimal Nota, string? Observacion);

public sealed record AsignaturaPerdida(string Asignatura, decimal Nota);

public sealed record CandidatoComision(
    Guid EstudianteId, string NombreCompleto, string Documento, IReadOnlyList<AsignaturaPerdida> Perdidas, int Fallas, int RecuperacionesPendientes,
    decimal? Promedio, DecisionComision Sugerida);

public sealed record CandidatosComisionResponse(int Anio, Guid? PeriodoId, string Periodo, int Grado, string? Grupo, IReadOnlyList<CandidatoComision> Estudiantes);

public sealed record DecisionRequest(Guid EstudianteId, string? Situacion, DecisionComision Decision, string? Observacion);

public sealed record GuardarActaRequest(
    int Anio, Guid? PeriodoId, int Grado, string? Grupo, DateOnly Fecha, string? Integrantes, string? Observaciones, IReadOnlyList<DecisionRequest> Decisiones);

public sealed record DecisionResponse(Guid EstudianteId, string NombreEstudiante, string? Situacion, DecisionComision Decision, string? Observacion);

public sealed record ActaResponse(
    Guid Id, string Codigo, int Anio, Guid? PeriodoId, string Periodo, int Grado, string? Grupo, int Numero, DateOnly Fecha, string? Integrantes,
    string? Observaciones, IReadOnlyList<DecisionResponse> Decisiones, DateTimeOffset CreadoEn);

public interface IEvaluacionService
{
    // Docente
    Task<Result<RecuperacionesClaseResponse>> RecuperacionesClaseAsync(MiembroCampus docente, Guid asignacionId, Guid periodoId, CancellationToken ct);

    Task<Result<RecuperacionesClaseResponse>> AsignarPlanesAsync(
        MiembroCampus docente, Guid asignacionId, Guid periodoId, AsignarPlanRequest request, CancellationToken ct);

    Task<Result<RecuperacionResponse>> CalificarAsync(MiembroCampus docente, Guid recuperacionId, CalificarRecuperacionRequest request, CancellationToken ct);

    // Estudiante y acudiente
    Task<Result<IReadOnlyList<RecuperacionResponse>>> MisRecuperacionesAsync(MiembroCampus actor, Guid? estudianteId, CancellationToken ct);

    // Panel
    Task<IReadOnlyList<RecuperacionResponse>> RecuperacionesAsync(int anio, Guid? periodoId, int? grado, string? grupo, CancellationToken ct);

    Task<Result<CandidatosComisionResponse>> CandidatosAsync(int anio, Guid? periodoId, int grado, string? grupo, CancellationToken ct);

    Task<IReadOnlyList<ActaResponse>> ActasAsync(int anio, CancellationToken ct);

    Task<Result<ActaResponse>> ActaAsync(Guid id, CancellationToken ct);

    Task<Result<ActaResponse>> GuardarActaAsync(Guid? id, GuardarActaRequest request, CancellationToken ct);

    Task<Result<Unit>> EliminarActaAsync(Guid id, CancellationToken ct);
}

/// <summary>
/// Recuperaciones (plan de mejoramiento y nota de recuperación por periodo) y comisiones de evaluación y promoción
/// por curso (Decreto 1290 de 2009).
/// </summary>
public sealed class EvaluacionService(
    IEvaluacionRepository repository,
    IAcademicoRepository academico,
    IMiembroCampusRepository miembros,
    IAccesoEstudiantes acceso,
    INotificador notificador,
    IUnitOfWork unitOfWork) : IEvaluacionService
{
    private static readonly Error ClaseAjena = Error.NoEncontrado("Asignacion.NoEncontrada", "Esa clase no está en su carga académica.");

    // ---------- Docente ----------

    public async Task<Result<RecuperacionesClaseResponse>> RecuperacionesClaseAsync(
        MiembroCampus docente, Guid asignacionId, Guid periodoId, CancellationToken ct)
    {
        var contexto = await ContextoAsync(docente, asignacionId, periodoId, ct);
        return contexto.IsSuccess ? await ArmarClaseAsync(contexto.Value, ct) : contexto.Error;
    }

    public async Task<Result<RecuperacionesClaseResponse>> AsignarPlanesAsync(
        MiembroCampus docente, Guid asignacionId, Guid periodoId, AsignarPlanRequest request, CancellationToken ct)
    {
        var contexto = await ContextoAsync(docente, asignacionId, periodoId, ct);
        if (!contexto.IsSuccess)
        {
            return contexto.Error;
        }

        if (string.IsNullOrWhiteSpace(request.Plan) || request.Plan.Trim().Length > Recuperacion.PlanMaxLength)
        {
            return Error.Campo("Plan", $"Describa las actividades del plan de mejoramiento (máximo {Recuperacion.PlanMaxLength} caracteres).");
        }

        if (request.EstudianteIds.Count == 0)
        {
            return Error.Campo("EstudianteIds", "Elija al menos un estudiante.");
        }

        var (asignacion, periodo, estudiantes, asignatura) = contexto.Value;
        var notas = (await academico.CalificacionesAsync([.. request.EstudianteIds], [periodo.Id], asignacion.AsignaturaId, ct))
            .ToDictionary(c => c.EstudianteId);
        var existentes = (await repository.RecuperacionesAsync([.. request.EstudianteIds], [periodo.Id], asignacion.AsignaturaId, ct))
            .ToDictionary(r => r.EstudianteId);
        var delCurso = estudiantes.ToDictionary(e => e.Id);

        foreach (var estudianteId in request.EstudianteIds.Distinct())
        {
            if (!delCurso.TryGetValue(estudianteId, out var estudiante))
            {
                return Error.Campo("EstudianteIds", "Hay estudiantes que no pertenecen a este curso.");
            }

            if (!existentes.TryGetValue(estudianteId, out var recuperacion))
            {
                if (notas.GetValueOrDefault(estudianteId)?.Nota is not { } nota || !CalculoBoletin.Perdida(nota))
                {
                    return Error.Conflicto("Recuperacion.NoPerdida", $"{estudiante.NombreCompleto} no perdió {asignatura} en {periodo.Nombre}.");
                }

                recuperacion = Recuperacion.Crear(estudianteId, asignacion.AsignaturaId, periodo.Id, docente.Id, nota);
                repository.Agregar(recuperacion);
            }
            else if (recuperacion.Estado != EstadoRecuperacion.Pendiente)
            {
                continue;
            }

            recuperacion.AsignarPlan(request.Plan, request.FechaLimite, docente.NombreCompleto);
            var limite = request.FechaLimite is { } f ? $" Fecha límite: {f:dd/MM/yyyy}." : string.Empty;
            notificador.Notificar(
                await Familias.DeEstudiantesAsync(miembros, [estudianteId], ct), TipoAviso.Recuperacion,
                $"Plan de mejoramiento de {asignatura}",
                $"{estudiante.NombreCompleto} debe presentar la recuperación de {asignatura} ({periodo.Nombre}).{limite} Actividades: {request.Plan.Trim()}",
                "/campus/recuperaciones", estudianteId, porCorreo: true);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await ArmarClaseAsync(contexto.Value, ct);
    }

    public async Task<Result<RecuperacionResponse>> CalificarAsync(
        MiembroCampus docente, Guid recuperacionId, CalificarRecuperacionRequest request, CancellationToken ct)
    {
        var recuperacion = await repository.RecuperacionAsync(recuperacionId, ct);
        if (recuperacion is null || docente.Rol != RolCampus.Docente)
        {
            return Error.NoEncontrado("Recuperacion.NoEncontrada", "No encontramos esa recuperación.");
        }

        var estudiante = await miembros.ObtenerPorIdAsync(recuperacion.EstudianteId, ct);
        var asignaciones = await academico.AsignacionesAsync(
            (await academico.PeriodoAsync(recuperacion.PeriodoId, ct))?.Anio ?? 0, docente.Id, null, null, ct);
        if (estudiante is null || !asignaciones.Any(a => a.AsignaturaId == recuperacion.AsignaturaId && a.Incluye(estudiante.Grado, estudiante.Grupo)))
        {
            return Error.NoEncontrado("Recuperacion.NoEncontrada", "No encontramos esa recuperación.");
        }

        if (!EscalaValorativa.EsValida(request.Nota))
        {
            return Error.Campo("Nota", $"La nota va de {EscalaValorativa.Minima:0.0} a {EscalaValorativa.Maxima:0.0}.");
        }

        if (request.Observacion?.Length > Recuperacion.ObservacionMaxLength)
        {
            return Error.Campo("Observacion", $"La observación admite máximo {Recuperacion.ObservacionMaxLength} caracteres.");
        }

        var definitiva = recuperacion.Calificar(request.Nota, request.Observacion, docente.NombreCompleto);

        // La nota del periodo queda con la definitiva; la original se conserva en la recuperación.
        var calificacion = (await academico.CalificacionesAsync([recuperacion.EstudianteId], [recuperacion.PeriodoId], recuperacion.AsignaturaId, ct))
            .FirstOrDefault();
        if (calificacion is null)
        {
            calificacion = Calificacion.Crear(recuperacion.EstudianteId, recuperacion.AsignaturaId, recuperacion.PeriodoId);
            academico.Agregar(calificacion);
        }

        var nota = $"Recuperó: {recuperacion.NotaOriginal:0.0} → {definitiva:0.0}";
        calificacion.Registrar(
            definitiva, calificacion.Fallas,
            calificacion.Observacion is { } o && !o.Contains("Recuperó", StringComparison.Ordinal) ? $"{o} {nota}." : $"{nota}.",
            docente.NombreCompleto);

        var asignatura = (await academico.AsignaturaAsync(recuperacion.AsignaturaId, ct))?.Nombre ?? "la asignatura";
        notificador.Notificar(
            await Familias.DeEstudiantesAsync(miembros, [recuperacion.EstudianteId], ct), TipoAviso.Recuperacion,
            recuperacion.Estado == EstadoRecuperacion.Aprobada ? $"Recuperación de {asignatura} aprobada" : $"Recuperación de {asignatura} no aprobada",
            $"{estudiante.NombreCompleto} obtuvo {recuperacion.NotaRecuperacion:0.0} en la recuperación; la nota del periodo queda en {definitiva:0.0}.",
            "/campus/recuperaciones", recuperacion.EstudianteId, porCorreo: true);

        await unitOfWork.SaveChangesAsync(ct);
        return (await ArmarAsync([recuperacion], ct))[0];
    }

    // ---------- Estudiante y acudiente ----------

    public async Task<Result<IReadOnlyList<RecuperacionResponse>>> MisRecuperacionesAsync(MiembroCampus actor, Guid? estudianteId, CancellationToken ct)
    {
        List<Guid> ids;
        if (estudianteId is { } id)
        {
            var visible = await acceso.EstudianteVisibleAsync(actor, id, ct);
            if (!visible.IsSuccess)
            {
                return visible.Error;
            }

            ids = [id];
        }
        else
        {
            ids = [.. (await acceso.EstudiantesPropiosAsync(actor, ct)).Select(e => e.Id)];
        }

        var lista = ids.Count == 0 ? [] : await repository.RecuperacionesAsync(ids, null, null, ct);
        return Resultado.Exito<IReadOnlyList<RecuperacionResponse>>(await ArmarAsync(lista, ct));
    }

    // ---------- Panel ----------

    public async Task<IReadOnlyList<RecuperacionResponse>> RecuperacionesAsync(int anio, Guid? periodoId, int? grado, string? grupo, CancellationToken ct)
    {
        var periodos = (await academico.PeriodosAsync(anio, ct)).Where(p => periodoId is null || p.Id == periodoId).Select(p => p.Id).ToList();
        if (periodos.Count == 0)
        {
            return [];
        }

        List<Guid>? ids = null;
        if (grado is { } g)
        {
            ids = [.. (await miembros.EstudiantesDelCursoAsync(g, string.IsNullOrWhiteSpace(grupo) ? null : grupo.Trim().ToUpperInvariant(), ct)).Select(e => e.Id)];
        }

        return await ArmarAsync(await repository.RecuperacionesAsync(ids, periodos, null, ct), ct);
    }

    public async Task<Result<CandidatosComisionResponse>> CandidatosAsync(int anio, Guid? periodoId, int grado, string? grupo, CancellationToken ct)
    {
        var periodos = (await academico.PeriodosAsync(anio, ct)).OrderBy(p => p.Numero).ToList();
        PeriodoAcademico? periodo = null;
        if (periodoId is { } pid)
        {
            periodo = periodos.FirstOrDefault(p => p.Id == pid);
            if (periodo is null)
            {
                return Error.NoEncontrado("Periodo.NoEncontrado", "Ese periodo no corresponde al año.");
            }
        }

        if (periodos.Count == 0)
        {
            return Error.Conflicto("Periodo.SinPeriodos", "Configure los periodos del año antes de reunir la comisión.");
        }

        var grupoNormalizado = string.IsNullOrWhiteSpace(grupo) ? null : grupo.Trim().ToUpperInvariant();
        var estudiantes = await miembros.EstudiantesDelCursoAsync(grado, grupoNormalizado, ct);
        var ids = estudiantes.Select(e => e.Id).ToList();
        var asignaturas = (await academico.AsignaturasAsync(soloActivas: false, ct)).ToDictionary(a => a.Id);
        var notas = ids.Count == 0 ? [] : await academico.CalificacionesAsync(ids, [.. periodos.Select(p => p.Id)], null, ct);
        var porEstudiante = notas.Where(n => n.Nota is not null).ToLookup(n => n.EstudianteId);
        var desde = periodo?.Inicio ?? periodos[0].Inicio;
        var hasta = periodo?.Fin ?? periodos[^1].Fin;
        var fallas = ids.Count == 0 ? [] : (await academico.FallasAsync(ids, desde, hasta, null, ct))
            .GroupBy(f => f.EstudianteId).ToDictionary(g => g.Key, g => g.Sum(f => f.Fallas));
        var pendientes = (ids.Count == 0 ? [] : await repository.RecuperacionesAsync(ids, [.. periodos.Select(p => p.Id)], null, ct))
            .Where(r => r.Estado == EstadoRecuperacion.Pendiente)
            .GroupBy(r => r.EstudianteId).ToDictionary(g => g.Key, g => g.Count());

        var candidatos = estudiantes.Select(e =>
        {
            var propias = porEstudiante[e.Id].ToList();
            List<AsignaturaPerdida> perdidas;
            decimal? promedio;
            if (periodo is not null)
            {
                var delPeriodo = propias.Where(n => n.PeriodoId == periodo.Id).ToList();
                perdidas = [.. delPeriodo.Where(n => CalculoBoletin.Perdida(n.Nota)).Select(n => new AsignaturaPerdida(Nombre(asignaturas, n.AsignaturaId), n.Nota!.Value))];
                promedio = CalculoBoletin.Promedio(delPeriodo.Select(n => n.Nota));
            }
            else
            {
                var finales = propias.GroupBy(n => n.AsignaturaId)
                    .Select(g => (Asignatura: g.Key, Nota: CalculoBoletin.Acumulada([.. periodos.Select(p => (g.FirstOrDefault(n => n.PeriodoId == p.Id)?.Nota, p.Porcentaje))])))
                    .ToList();
                perdidas = [.. finales.Where(f => CalculoBoletin.Perdida(f.Nota)).Select(f => new AsignaturaPerdida(Nombre(asignaturas, f.Asignatura), f.Nota!.Value))];
                promedio = CalculoBoletin.Promedio(finales.Select(f => f.Nota));
            }

            var sugerida = periodo is not null
                ? perdidas.Count > 0 ? DecisionComision.PlanMejoramiento : DecisionComision.Promovido
                : perdidas.Count >= 3 ? DecisionComision.NoPromovido
                : perdidas.Count > 0 ? DecisionComision.PlanMejoramiento
                : DecisionComision.Promovido;
            return new CandidatoComision(
                e.Id, e.NombreCompleto, e.Documento, [.. perdidas.OrderBy(p => p.Nota)], fallas.GetValueOrDefault(e.Id), pendientes.GetValueOrDefault(e.Id),
                promedio, sugerida);
        }).ToList();

        return new CandidatosComisionResponse(anio, periodo?.Id, periodo?.Nombre ?? "Promoción final", grado, grupoNormalizado, candidatos);
    }

    public async Task<IReadOnlyList<ActaResponse>> ActasAsync(int anio, CancellationToken ct) =>
        await ArmarActasAsync(await repository.ActasAsync(anio, ct), ct);

    public async Task<Result<ActaResponse>> ActaAsync(Guid id, CancellationToken ct)
    {
        var acta = await repository.ActaAsync(id, ct);
        return acta is null ? ActaNoEncontrada : (await ArmarActasAsync([acta], ct))[0];
    }

    private static readonly Error ActaNoEncontrada = Error.NoEncontrado("Acta.NoEncontrada", "No encontramos esa acta.");

    public async Task<Result<ActaResponse>> GuardarActaAsync(Guid? id, GuardarActaRequest request, CancellationToken ct)
    {
        var r = request;
        if (!MiembroCampus.GradoValido(r.Grado))
        {
            return Error.Campo("Grado", "Elija el grado.");
        }

        if (r.Integrantes?.Length > ActaComision.IntegrantesMaxLength || r.Observaciones?.Length > ActaComision.ObservacionesMaxLength)
        {
            return Error.Campo("Observaciones", "El texto del acta es demasiado largo.");
        }

        if (r.PeriodoId is { } periodoId && (await academico.PeriodoAsync(periodoId, ct))?.Anio != r.Anio)
        {
            return Error.Campo("PeriodoId", "El periodo no corresponde al año.");
        }

        if (r.Decisiones.Any(d => !Enum.IsDefined(d.Decision)) || r.Decisiones.Select(d => d.EstudianteId).Distinct().Count() != r.Decisiones.Count)
        {
            return Error.Campo("Decisiones", "Revise las decisiones: cada estudiante una sola vez.");
        }

        var nombres = (await miembros.ObtenerPorIdsAsync([.. r.Decisiones.Select(d => d.EstudianteId)], ct)).ToDictionary(m => m.Id, m => m.NombreCompleto);
        if (r.Decisiones.Any(d => !nombres.ContainsKey(d.EstudianteId)))
        {
            return Error.Campo("Decisiones", "Hay estudiantes que no existen.");
        }

        ActaComision acta;
        if (id is { } existente)
        {
            var encontrada = await repository.ActaAsync(existente, ct);
            if (encontrada is null)
            {
                return ActaNoEncontrada;
            }

            acta = encontrada;
            foreach (var anterior in await repository.DecisionesAsync([acta.Id], ct))
            {
                repository.Eliminar(anterior);
            }
        }
        else
        {
            acta = ActaComision.Crear(r.Anio, r.PeriodoId, r.Grado, r.Grupo, await repository.SiguienteNumeroActaAsync(r.Anio, ct));
            repository.Agregar(acta);
        }

        acta.Actualizar(r.Fecha, r.Integrantes, r.Observaciones);
        foreach (var d in r.Decisiones)
        {
            repository.Agregar(DecisionEstudiante.Crear(acta.Id, d.EstudianteId, nombres[d.EstudianteId], d.Situacion, d.Decision, d.Observacion));
        }

        await unitOfWork.SaveChangesAsync(ct);
        return (await ArmarActasAsync([acta], ct))[0];
    }

    public async Task<Result<Unit>> EliminarActaAsync(Guid id, CancellationToken ct)
    {
        var acta = await repository.ActaAsync(id, ct);
        if (acta is null)
        {
            return ActaNoEncontrada;
        }

        repository.Eliminar(acta);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    // ---------- Apoyo ----------

    private sealed record Contexto(AsignacionDocente Asignacion, PeriodoAcademico Periodo, List<MiembroCampus> Estudiantes, string Asignatura);

    private async Task<Result<Contexto>> ContextoAsync(MiembroCampus docente, Guid asignacionId, Guid periodoId, CancellationToken ct)
    {
        if (docente.Rol != RolCampus.Docente)
        {
            return CalificacionesService.SoloDocentes;
        }

        var asignacion = await academico.AsignacionAsync(asignacionId, ct);
        if (asignacion is null || asignacion.DocenteId != docente.Id)
        {
            return ClaseAjena;
        }

        var periodo = await academico.PeriodoAsync(periodoId, ct);
        if (periodo is null || periodo.Anio != asignacion.Anio)
        {
            return Error.NoEncontrado("Periodo.NoEncontrado", "Ese periodo no corresponde al año de la clase.");
        }

        var asignatura = (await academico.AsignaturaAsync(asignacion.AsignaturaId, ct))?.Nombre ?? "(asignatura)";
        return new Contexto(asignacion, periodo, await miembros.EstudiantesDelCursoAsync(asignacion.Grado, asignacion.Grupo, ct), asignatura);
    }

    private async Task<RecuperacionesClaseResponse> ArmarClaseAsync(Contexto contexto, CancellationToken ct)
    {
        var (asignacion, periodo, estudiantes, asignatura) = contexto;
        var ids = estudiantes.Select(e => e.Id).ToList();
        var notas = (await academico.CalificacionesAsync(ids, [periodo.Id], asignacion.AsignaturaId, ct)).ToDictionary(c => c.EstudianteId);
        var recuperaciones = (await ArmarAsync(await repository.RecuperacionesAsync(ids, [periodo.Id], asignacion.AsignaturaId, ct), ct))
            .ToDictionary(r => r.EstudianteId);
        var filas = estudiantes
            .Where(e => recuperaciones.ContainsKey(e.Id) || CalculoBoletin.Perdida(notas.GetValueOrDefault(e.Id)?.Nota))
            .Select(e => new FilaRecuperacionClase(e.Id, e.NombreCompleto, e.Documento, notas.GetValueOrDefault(e.Id)?.Nota, recuperaciones.GetValueOrDefault(e.Id)))
            .ToList();
        return new RecuperacionesClaseResponse(asignacion.Id, asignatura, asignacion.Grado, asignacion.Grupo, PeriodoResponse.Desde(periodo), filas);
    }

    private static string Nombre(Dictionary<Guid, Asignatura> asignaturas, Guid id) => asignaturas.GetValueOrDefault(id)?.Nombre ?? "(asignatura)";

    private async Task<List<RecuperacionResponse>> ArmarAsync(List<Recuperacion> lista, CancellationToken ct)
    {
        if (lista.Count == 0)
        {
            return [];
        }

        var asignaturas = (await academico.AsignaturasAsync(soloActivas: false, ct)).ToDictionary(a => a.Id);
        var estudiantes = (await miembros.ObtenerPorIdsAsync([.. lista.Select(r => r.EstudianteId).Distinct()], ct)).ToDictionary(e => e.Id);
        var periodos = new Dictionary<Guid, PeriodoAcademico>();
        foreach (var id in lista.Select(r => r.PeriodoId).Distinct())
        {
            if (await academico.PeriodoAsync(id, ct) is { } p)
            {
                periodos[id] = p;
            }
        }

        return
        [
            .. lista
                .OrderBy(r => periodos.GetValueOrDefault(r.PeriodoId)?.Numero)
                .ThenBy(r => estudiantes.GetValueOrDefault(r.EstudianteId)?.Grado)
                .ThenBy(r => estudiantes.GetValueOrDefault(r.EstudianteId)?.NombreCompleto)
                .Select(r =>
                {
                    var e = estudiantes.GetValueOrDefault(r.EstudianteId);
                    return new RecuperacionResponse(
                        r.Id, r.EstudianteId, e?.NombreCompleto ?? "(estudiante)", Familias.Curso(e?.Grado, e?.Grupo), r.AsignaturaId,
                        Nombre(asignaturas, r.AsignaturaId), r.PeriodoId, periodos.GetValueOrDefault(r.PeriodoId)?.Nombre ?? string.Empty, r.NotaOriginal,
                        r.Plan, r.FechaLimite, r.NotaRecuperacion, r.NotaDefinitiva, r.Observacion, r.Estado, r.RegistradaPor, r.CreadoEn);
                }),
        ];
    }

    private async Task<List<ActaResponse>> ArmarActasAsync(List<ActaComision> actas, CancellationToken ct)
    {
        if (actas.Count == 0)
        {
            return [];
        }

        var decisiones = (await repository.DecisionesAsync([.. actas.Select(a => a.Id)], ct)).ToLookup(d => d.ActaId);
        var periodos = new Dictionary<Guid, string>();
        foreach (var id in actas.Select(a => a.PeriodoId).OfType<Guid>().Distinct())
        {
            periodos[id] = (await academico.PeriodoAsync(id, ct))?.Nombre ?? "Periodo";
        }

        return
        [
            .. actas.OrderByDescending(a => a.Anio).ThenByDescending(a => a.Numero).Select(a => new ActaResponse(
                a.Id, $"CEP-{a.Anio}-{a.Numero:000}", a.Anio, a.PeriodoId, a.PeriodoId is { } p ? periodos.GetValueOrDefault(p, "Periodo") : "Promoción final",
                a.Grado, a.Grupo, a.Numero, a.Fecha, a.Integrantes, a.Observaciones,
                [.. decisiones[a.Id].OrderBy(d => d.NombreEstudiante).Select(d => new DecisionResponse(d.EstudianteId, d.NombreEstudiante, d.Situacion, d.Decision, d.Observacion))],
                a.CreadoEn)),
        ];
    }
}
