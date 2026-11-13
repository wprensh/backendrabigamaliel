using FluentValidation;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Academico;

public interface IAcademicoService
{
    Task<IReadOnlyList<AsignaturaResponse>> AsignaturasAsync(CancellationToken ct);

    Task<Result<AsignaturaResponse>> CrearAsignaturaAsync(GuardarAsignaturaRequest request, CancellationToken ct);

    Task<Result<AsignaturaResponse>> ActualizarAsignaturaAsync(Guid id, GuardarAsignaturaRequest request, CancellationToken ct);

    Task<Result<Unit>> EliminarAsignaturaAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<PeriodoResponse>> PeriodosAsync(int? anio, CancellationToken ct);

    Task<Result<PeriodoResponse>> CrearPeriodoAsync(GuardarPeriodoRequest request, CancellationToken ct);

    Task<Result<PeriodoResponse>> ActualizarPeriodoAsync(Guid id, GuardarPeriodoRequest request, CancellationToken ct);

    Task<Result<Unit>> EliminarPeriodoAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<AsignacionResponse>> AsignacionesAsync(int anio, Guid? docenteId, int? grado, string? grupo, CancellationToken ct);

    Task<Result<AsignacionResponse>> CrearAsignacionAsync(CrearAsignacionRequest request, CancellationToken ct);

    Task<Result<AsignacionResponse>> ActualizarAsignacionAsync(Guid id, ActualizarAsignacionRequest request, CancellationToken ct);

    Task<Result<Unit>> EliminarAsignacionAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<CursoResponse>> CursosAsync(CancellationToken ct);

    Task<Result<ConsolidadoResponse>> ConsolidadoAsync(Guid periodoId, int grado, string? grupo, CancellationToken ct);

    Task<Result<IReadOnlyList<BoletinResponse>>> BoletinesCursoAsync(Guid periodoId, int grado, string? grupo, CancellationToken ct);
}

/// <summary>Configuración académica del panel: asignaturas, periodos, carga docente, consolidado y boletines por curso.</summary>
public sealed class AcademicoService(
    IAcademicoRepository repository,
    IMiembroCampusRepository miembros,
    IBoletinService boletines,
    IUnitOfWork unitOfWork,
    IValidator<GuardarAsignaturaRequest> asignaturaValidator,
    IValidator<GuardarPeriodoRequest> periodoValidator,
    IValidator<CrearAsignacionRequest> asignacionValidator) : IAcademicoService
{
    private static Error AsignaturaNoEncontrada(Guid id) => Error.NoEncontrado("Asignatura.NoEncontrada", $"No existe la asignatura '{id}'.");

    private static Error PeriodoNoEncontrado(Guid id) => Error.NoEncontrado("Periodo.NoEncontrado", $"No existe el periodo '{id}'.");

    private static Error AsignacionNoEncontrada(Guid id) => Error.NoEncontrado("Asignacion.NoEncontrada", $"No existe la asignación '{id}'.");

    // ---------- Asignaturas ----------

    public async Task<IReadOnlyList<AsignaturaResponse>> AsignaturasAsync(CancellationToken ct) =>
        [.. (await repository.AsignaturasAsync(soloActivas: false, ct)).Select(AsignaturaResponse.Desde)];

    public async Task<Result<AsignaturaResponse>> CrearAsignaturaAsync(GuardarAsignaturaRequest request, CancellationToken ct)
    {
        if (await asignaturaValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var existentes = await repository.AsignaturasAsync(soloActivas: false, ct);
        if (existentes.Any(a => string.Equals(a.Nombre, request.Nombre.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Error.Conflicto("Asignatura.Repetida", "Ya existe una asignatura con ese nombre.");
        }

        var asignatura = Asignatura.Crear(request.Nombre, request.Area ?? string.Empty, request.Orden);
        repository.Agregar(asignatura);
        await unitOfWork.SaveChangesAsync(ct);
        return AsignaturaResponse.Desde(asignatura);
    }

    public async Task<Result<AsignaturaResponse>> ActualizarAsignaturaAsync(Guid id, GuardarAsignaturaRequest request, CancellationToken ct)
    {
        if (await asignaturaValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var asignatura = await repository.AsignaturaAsync(id, ct);
        if (asignatura is null)
        {
            return AsignaturaNoEncontrada(id);
        }

        asignatura.Actualizar(request.Nombre, request.Area ?? string.Empty, request.Orden, request.Activa);
        await unitOfWork.SaveChangesAsync(ct);
        return AsignaturaResponse.Desde(asignatura);
    }

    public async Task<Result<Unit>> EliminarAsignaturaAsync(Guid id, CancellationToken ct)
    {
        var asignatura = await repository.AsignaturaAsync(id, ct);
        if (asignatura is null)
        {
            return AsignaturaNoEncontrada(id);
        }

        if (await repository.AsignaturaEnUsoAsync(id, ct))
        {
            return Error.Conflicto("Asignatura.EnUso", "La asignatura ya tiene carga académica o notas. Desactívela en lugar de eliminarla.");
        }

        repository.Eliminar(asignatura);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    // ---------- Periodos ----------

    public async Task<IReadOnlyList<PeriodoResponse>> PeriodosAsync(int? anio, CancellationToken ct) =>
        [.. (await repository.PeriodosAsync(anio, ct)).Select(PeriodoResponse.Desde)];

    public async Task<Result<PeriodoResponse>> CrearPeriodoAsync(GuardarPeriodoRequest request, CancellationToken ct)
    {
        if (await periodoValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        if (await repository.ExistePeriodoAsync(request.Anio, request.Numero, null, ct))
        {
            return Error.Conflicto("Periodo.Repetido", $"Ya existe el periodo {request.Numero} de {request.Anio}.");
        }

        var periodo = PeriodoAcademico.Crear(request.Anio, request.Numero, request.Nombre, request.Inicio, request.Fin, request.Porcentaje);
        periodo.Actualizar(request.Nombre, request.Inicio, request.Fin, request.Porcentaje, request.NotasAbiertas, request.Publicado);
        repository.Agregar(periodo);
        await unitOfWork.SaveChangesAsync(ct);
        return PeriodoResponse.Desde(periodo);
    }

    public async Task<Result<PeriodoResponse>> ActualizarPeriodoAsync(Guid id, GuardarPeriodoRequest request, CancellationToken ct)
    {
        if (await periodoValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var periodo = await repository.PeriodoAsync(id, ct);
        if (periodo is null)
        {
            return PeriodoNoEncontrado(id);
        }

        if (periodo.Anio != request.Anio || periodo.Numero != request.Numero)
        {
            return Error.Conflicto("Periodo.NoCambiaNumero", "El año y el número del periodo no se cambian; cree otro periodo.");
        }

        var sePublica = !periodo.Publicado && request.Publicado;
        periodo.Actualizar(request.Nombre, request.Inicio, request.Fin, request.Porcentaje, request.NotasAbiertas, request.Publicado);
        if (sePublica)
        {
            await boletines.AvisarPublicacionAsync(periodo, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return PeriodoResponse.Desde(periodo);
    }

    public async Task<Result<Unit>> EliminarPeriodoAsync(Guid id, CancellationToken ct)
    {
        var periodo = await repository.PeriodoAsync(id, ct);
        if (periodo is null)
        {
            return PeriodoNoEncontrado(id);
        }

        if (await repository.PeriodoConNotasAsync(id, ct))
        {
            return Error.Conflicto("Periodo.ConNotas", "El periodo ya tiene notas registradas y no se puede eliminar.");
        }

        repository.Eliminar(periodo);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    // ---------- Carga académica ----------

    public async Task<IReadOnlyList<AsignacionResponse>> AsignacionesAsync(
        int anio, Guid? docenteId, int? grado, string? grupo, CancellationToken ct)
    {
        var asignaciones = await repository.AsignacionesAsync(anio, docenteId, grado, Normalizar(grupo), ct);
        return await ResponderAsync(asignaciones, ct);
    }

    public async Task<Result<AsignacionResponse>> CrearAsignacionAsync(CrearAsignacionRequest request, CancellationToken ct)
    {
        if (await asignacionValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        if (await ValidarDocenteYAsignaturaAsync(request.DocenteId, request.AsignaturaId, ct) is { } invalido)
        {
            return invalido;
        }

        var grupo = Normalizar(request.Grupo);
        if (await repository.ExisteAsignacionAsync(request.Anio, request.AsignaturaId, request.Grado, grupo, ct))
        {
            return Error.Conflicto("Asignacion.Repetida", "Esa asignatura ya tiene docente en ese curso. Edite la asignación existente.");
        }

        var asignacion = AsignacionDocente.Crear(request.Anio, request.DocenteId, request.AsignaturaId, request.Grado, grupo, request.IntensidadHoraria);
        repository.Agregar(asignacion);
        await unitOfWork.SaveChangesAsync(ct);
        return (await ResponderAsync([asignacion], ct))[0];
    }

    public async Task<Result<AsignacionResponse>> ActualizarAsignacionAsync(Guid id, ActualizarAsignacionRequest request, CancellationToken ct)
    {
        var asignacion = await repository.AsignacionAsync(id, ct);
        if (asignacion is null)
        {
            return AsignacionNoEncontrada(id);
        }

        if (await ValidarDocenteYAsignaturaAsync(request.DocenteId, asignacion.AsignaturaId, ct) is { } invalido)
        {
            return invalido;
        }

        asignacion.CambiarDocente(request.DocenteId, request.IntensidadHoraria);
        await unitOfWork.SaveChangesAsync(ct);
        return (await ResponderAsync([asignacion], ct))[0];
    }

    public async Task<Result<Unit>> EliminarAsignacionAsync(Guid id, CancellationToken ct)
    {
        var asignacion = await repository.AsignacionAsync(id, ct);
        if (asignacion is null)
        {
            return AsignacionNoEncontrada(id);
        }

        if (await repository.AsignacionConRegistrosAsync(id, ct))
        {
            return Error.Conflicto("Asignacion.ConAsistencia", "Esta clase ya tiene asistencia registrada. Cambie el docente en lugar de eliminarla.");
        }

        repository.Eliminar(asignacion);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<IReadOnlyList<CursoResponse>> CursosAsync(CancellationToken ct) => await repository.CursosAsync(ct);

    // ---------- Consolidado y boletines ----------

    public async Task<Result<ConsolidadoResponse>> ConsolidadoAsync(Guid periodoId, int grado, string? grupo, CancellationToken ct)
    {
        var periodo = await repository.PeriodoAsync(periodoId, ct);
        if (periodo is null)
        {
            return PeriodoNoEncontrado(periodoId);
        }

        grupo = Normalizar(grupo);
        var estudiantes = await miembros.EstudiantesDelCursoAsync(grado, grupo, ct);
        var asignaciones = await repository.AsignacionesAsync(periodo.Anio, null, grado, grupo, ct);
        var asignaturas = (await repository.AsignaturasAsync(soloActivas: false, ct)).ToDictionary(a => a.Id);
        var calificaciones = await repository.CalificacionesAsync([.. estudiantes.Select(e => e.Id)], [periodo.Id], null, ct);

        var columnas = asignaciones
            .Select(a => a.AsignaturaId)
            .Concat(calificaciones.Select(c => c.AsignaturaId))
            .Distinct()
            .Where(asignaturas.ContainsKey)
            .Select(id => asignaturas[id])
            .OrderBy(a => a.Orden).ThenBy(a => a.Nombre)
            .Select(a => new ColumnaAsignatura(a.Id, a.Nombre, a.Area))
            .ToList();

        var notas = calificaciones.ToDictionary(c => (c.EstudianteId, c.AsignaturaId), c => c.Nota);
        var promedios = estudiantes.ToDictionary(
            e => e.Id,
            e => CalculoBoletin.Promedio(columnas.Select(c => notas.GetValueOrDefault((e.Id, c.Id)))));
        var puestos = CalculoBoletin.Puestos(promedios);

        var filas = estudiantes.Select(e =>
        {
            var propias = columnas.Select(c => notas.GetValueOrDefault((e.Id, c.Id))).ToList();
            return new FilaConsolidado(
                e.Id, e.NombreCompleto, e.Documento, propias, promedios[e.Id], puestos.TryGetValue(e.Id, out var p) ? p : null,
                propias.Count(CalculoBoletin.Perdida));
        }).ToList();

        var esperadas = estudiantes.Count * asignaciones.Select(a => a.AsignaturaId).Distinct().Count();
        var registradas = calificaciones.Count(c => c.Nota is not null);
        var avance = esperadas == 0 ? 0 : (int)Math.Min(100, Math.Round(registradas * 100.0 / esperadas));

        return new ConsolidadoResponse(
            PeriodoResponse.Desde(periodo), grado, grupo, columnas, filas, CalculoBoletin.Promedio(promedios.Values), avance);
    }

    public async Task<Result<IReadOnlyList<BoletinResponse>>> BoletinesCursoAsync(Guid periodoId, int grado, string? grupo, CancellationToken ct)
    {
        var periodo = await repository.PeriodoAsync(periodoId, ct);
        if (periodo is null)
        {
            return PeriodoNoEncontrado(periodoId);
        }

        var estudiantes = await miembros.EstudiantesDelCursoAsync(grado, Normalizar(grupo), ct);
        return Resultado.Exito(await boletines.CalcularAsync(estudiantes, periodo, incluirNoPublicados: true, ct));
    }

    private async Task<Error?> ValidarDocenteYAsignaturaAsync(Guid docenteId, Guid asignaturaId, CancellationToken ct)
    {
        if (await miembros.ObtenerPorIdAsync(docenteId, ct) is not { Rol: RolCampus.Docente, Activo: true })
        {
            return Error.Campo("DocenteId", "Elija un docente activo.");
        }

        return await repository.AsignaturaAsync(asignaturaId, ct) is null
            ? Error.Campo("AsignaturaId", "Elija una asignatura.")
            : null;
    }

    private async Task<IReadOnlyList<AsignacionResponse>> ResponderAsync(IReadOnlyList<AsignacionDocente> asignaciones, CancellationToken ct)
    {
        var docentes = (await miembros.ObtenerPorIdsAsync([.. asignaciones.Select(a => a.DocenteId).Distinct()], ct)).ToDictionary(d => d.Id);
        var asignaturas = (await repository.AsignaturasAsync(soloActivas: false, ct)).ToDictionary(a => a.Id);
        return
        [
            .. asignaciones
                .Select(a => new AsignacionResponse(
                    a.Id, a.Anio, a.DocenteId, docentes.TryGetValue(a.DocenteId, out var d) ? d.NombreCompleto : "(sin docente)",
                    a.AsignaturaId, asignaturas.TryGetValue(a.AsignaturaId, out var s) ? s.Nombre : "(asignatura)",
                    asignaturas.TryGetValue(a.AsignaturaId, out var s2) ? s2.Area : string.Empty,
                    a.Grado, a.Grupo, a.IntensidadHoraria))
                .OrderBy(a => a.Grado).ThenBy(a => a.Grupo).ThenBy(a => a.Asignatura),
        ];
    }

    internal static string? Normalizar(string? grupo) => string.IsNullOrWhiteSpace(grupo) ? null : grupo.Trim().ToUpperInvariant();
}
