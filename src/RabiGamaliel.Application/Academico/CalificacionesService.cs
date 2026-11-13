using FluentValidation;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Academico;

public interface ICalificacionesService
{
    Task<Result<MisCursosResponse>> MisCursosAsync(MiembroCampus docente, CancellationToken ct);

    Task<Result<PlanillaNotasResponse>> PlanillaAsync(MiembroCampus docente, Guid asignacionId, Guid periodoId, CancellationToken ct);

    Task<Result<PlanillaNotasResponse>> GuardarAsync(
        MiembroCampus docente, Guid asignacionId, Guid periodoId, GuardarNotasRequest request, CancellationToken ct);

    /// <summary>Estudiantes de una clase del docente (para el observador).</summary>
    Task<Result<IReadOnlyList<EstudianteBoletin>>> EstudiantesAsync(MiembroCampus docente, Guid asignacionId, CancellationToken ct);
}

/// <summary>Registro de notas del docente desde el campus, para las asignaturas y cursos de su carga académica.</summary>
public sealed class CalificacionesService(
    IAcademicoRepository repository,
    IMiembroCampusRepository miembros,
    IUnitOfWork unitOfWork,
    TimeProvider reloj,
    IValidator<GuardarNotasRequest> validator) : ICalificacionesService
{
    internal static readonly Error SoloDocentes = Error.Conflicto("Academico.SoloDocentes", "Esta sección es para docentes.");

    private static readonly Error AsignacionAjena =
        Error.NoEncontrado("Asignacion.NoEncontrada", "Esa clase no está en su carga académica.");

    public async Task<Result<MisCursosResponse>> MisCursosAsync(MiembroCampus docente, CancellationToken ct)
    {
        if (docente.Rol != RolCampus.Docente)
        {
            return SoloDocentes;
        }

        var anio = FechaColombia.AnioLectivo(reloj);
        var asignaciones = await repository.AsignacionesAsync(anio, docente.Id, null, null, ct);
        var asignaturas = (await repository.AsignaturasAsync(soloActivas: false, ct)).ToDictionary(a => a.Id);
        var cursos = new List<CursoDocente>(asignaciones.Count);
        foreach (var a in asignaciones.OrderBy(a => a.Grado).ThenBy(a => a.Grupo))
        {
            var estudiantes = await miembros.EstudiantesDelCursoAsync(a.Grado, a.Grupo, ct);
            var asignatura = asignaturas.GetValueOrDefault(a.AsignaturaId);
            cursos.Add(new CursoDocente(
                a.Id, a.AsignaturaId, asignatura?.Nombre ?? "(asignatura)", asignatura?.Area ?? string.Empty, a.Grado, a.Grupo,
                a.IntensidadHoraria, estudiantes.Count));
        }

        var periodos = await repository.PeriodosAsync(anio, ct);
        return new MisCursosResponse(anio, [.. periodos.OrderBy(p => p.Numero).Select(PeriodoResponse.Desde)], cursos);
    }

    public async Task<Result<PlanillaNotasResponse>> PlanillaAsync(MiembroCampus docente, Guid asignacionId, Guid periodoId, CancellationToken ct)
    {
        var contexto = await ContextoAsync(docente, asignacionId, periodoId, ct);
        return contexto.IsSuccess ? await ArmarAsync(contexto.Value, ct) : contexto.Error;
    }

    public async Task<Result<PlanillaNotasResponse>> GuardarAsync(
        MiembroCampus docente, Guid asignacionId, Guid periodoId, GuardarNotasRequest request, CancellationToken ct)
    {
        if (await validator.ValidarAsync(request, ct) is { } invalido)
        {
            return invalido;
        }

        var contexto = await ContextoAsync(docente, asignacionId, periodoId, ct);
        if (!contexto.IsSuccess)
        {
            return contexto.Error;
        }

        var (asignacion, periodo, estudiantes, _) = contexto.Value;
        if (!periodo.NotasAbiertas)
        {
            return Error.Conflicto("Periodo.NotasCerradas", "El registro de notas de este periodo está cerrado. Pida a coordinación que lo abra.");
        }

        var delCurso = estudiantes.Select(e => e.Id).ToHashSet();
        if (request.Notas.Any(n => !delCurso.Contains(n.EstudianteId)))
        {
            return Error.Campo("Notas", "Hay estudiantes que no pertenecen a este curso.");
        }

        var existentes = (await repository.CalificacionesAsync([.. delCurso], [periodo.Id], asignacion.AsignaturaId, ct))
            .ToDictionary(c => c.EstudianteId);
        foreach (var fila in request.Notas)
        {
            if (!existentes.TryGetValue(fila.EstudianteId, out var calificacion))
            {
                if (fila.Nota is null && fila.Fallas is null && string.IsNullOrWhiteSpace(fila.Observacion))
                {
                    continue;
                }

                calificacion = Calificacion.Crear(fila.EstudianteId, asignacion.AsignaturaId, periodo.Id);
                repository.Agregar(calificacion);
            }

            calificacion.Registrar(fila.Nota, fila.Fallas, fila.Observacion, docente.NombreCompleto);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await ArmarAsync(contexto.Value, ct);
    }

    public async Task<Result<IReadOnlyList<EstudianteBoletin>>> EstudiantesAsync(MiembroCampus docente, Guid asignacionId, CancellationToken ct)
    {
        if (docente.Rol != RolCampus.Docente)
        {
            return SoloDocentes;
        }

        var asignacion = await repository.AsignacionAsync(asignacionId, ct);
        if (asignacion is null || asignacion.DocenteId != docente.Id)
        {
            return AsignacionAjena;
        }

        var estudiantes = await miembros.EstudiantesDelCursoAsync(asignacion.Grado, asignacion.Grupo, ct);
        return estudiantes.Select(e => new EstudianteBoletin(e.Id, e.NombreCompleto, e.Documento, e.Grado, e.Grupo)).ToList();
    }

    private sealed record Contexto(AsignacionDocente Asignacion, PeriodoAcademico Periodo, List<MiembroCampus> Estudiantes, string Asignatura);

    private async Task<Result<Contexto>> ContextoAsync(MiembroCampus docente, Guid asignacionId, Guid periodoId, CancellationToken ct)
    {
        if (docente.Rol != RolCampus.Docente)
        {
            return SoloDocentes;
        }

        var asignacion = await repository.AsignacionAsync(asignacionId, ct);
        if (asignacion is null || asignacion.DocenteId != docente.Id)
        {
            return AsignacionAjena;
        }

        var periodo = await repository.PeriodoAsync(periodoId, ct);
        if (periodo is null || periodo.Anio != asignacion.Anio)
        {
            return Error.NoEncontrado("Periodo.NoEncontrado", "Ese periodo no corresponde al año de la clase.");
        }

        var asignatura = await repository.AsignaturaAsync(asignacion.AsignaturaId, ct);
        var estudiantes = await miembros.EstudiantesDelCursoAsync(asignacion.Grado, asignacion.Grupo, ct);
        return new Contexto(asignacion, periodo, estudiantes, asignatura?.Nombre ?? "(asignatura)");
    }

    private async Task<PlanillaNotasResponse> ArmarAsync(Contexto contexto, CancellationToken ct)
    {
        var (asignacion, periodo, estudiantes, asignatura) = contexto;
        var ids = estudiantes.Select(e => e.Id).ToList();
        var notas = (await repository.CalificacionesAsync(ids, [periodo.Id], asignacion.AsignaturaId, ct)).ToDictionary(c => c.EstudianteId);
        var fallas = (await repository.FallasAsync(ids, periodo.Inicio, periodo.Fin, asignacion.AsignaturaId, ct))
            .ToDictionary(f => f.EstudianteId, f => f.Fallas);

        return new PlanillaNotasResponse(
            asignacion.Id, asignatura, asignacion.Grado, asignacion.Grupo, PeriodoResponse.Desde(periodo), periodo.NotasAbiertas,
            [
                .. estudiantes.Select(e =>
                {
                    var c = notas.GetValueOrDefault(e.Id);
                    return new FilaPlanillaNotas(e.Id, e.NombreCompleto, e.Documento, c?.Nota, c?.Fallas, fallas.GetValueOrDefault(e.Id), c?.Observacion);
                }),
            ]);
    }
}
