using FluentValidation;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Academico;

public interface IHorarioService
{
    /// <summary>Horario de un docente (por docenteId) o de un curso (por grado y grupo) en el año lectivo.</summary>
    Task<Result<HorarioResponse>> HorarioAsync(int anio, Guid? docenteId, int? grado, string? grupo, CancellationToken ct);

    Task<Result<BloqueHorarioResponse>> CrearBloqueAsync(GuardarBloqueRequest request, CancellationToken ct);

    Task<Result<BloqueHorarioResponse>> MoverBloqueAsync(Guid id, MoverBloqueRequest request, CancellationToken ct);

    Task<Result<Unit>> EliminarBloqueAsync(Guid id, CancellationToken ct);

    /// <summary>Horario en el campus: el docente ve el suyo, el estudiante el de su curso y el acudiente el de su estudiante.</summary>
    Task<Result<HorarioResponse>> MiHorarioAsync(MiembroCampus actor, Guid? estudianteId, CancellationToken ct);
}

/// <summary>
/// Horario semanal de clases. Cada bloque ubica una asignación de la carga docente en un día y una franja, y no puede
/// cruzarse con otra clase del mismo docente ni del mismo curso (una asignación de todo el grado ocupa a todos sus grupos).
/// </summary>
public sealed class HorarioService(
    IAcademicoRepository repository,
    IMiembroCampusRepository miembros,
    IAccesoEstudiantes acceso,
    IUnitOfWork unitOfWork,
    TimeProvider reloj,
    IValidator<GuardarBloqueRequest> guardarValidator,
    IValidator<MoverBloqueRequest> moverValidator) : IHorarioService
{
    public async Task<Result<HorarioResponse>> HorarioAsync(int anio, Guid? docenteId, int? grado, string? grupo, CancellationToken ct)
    {
        if (docenteId is null && grado is null)
        {
            return Error.Campo("DocenteId", "Elija un docente o un curso.");
        }

        var asignaciones = await repository.AsignacionesAsync(anio, docenteId, grado, AcademicoService.Normalizar(grupo), ct);
        return await ResponderAsync(asignaciones, ct);
    }

    public async Task<Result<BloqueHorarioResponse>> CrearBloqueAsync(GuardarBloqueRequest request, CancellationToken ct)
    {
        if (await guardarValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var asignacion = await repository.AsignacionAsync(request.AsignacionId, ct);
        if (asignacion is null)
        {
            return Error.Campo("AsignacionId", "Elija una clase de la carga docente.");
        }

        if (await CruceAsync(asignacion, null, request.Dia, request.Inicio, request.Fin, ct) is { } cruce)
        {
            return cruce;
        }

        var bloque = BloqueHorario.Crear(asignacion.Id, request.Dia, request.Inicio, request.Fin);
        repository.Agregar(bloque);
        await unitOfWork.SaveChangesAsync(ct);
        return (await ResponderAsync([asignacion], ct, [bloque])).Bloques[0];
    }

    public async Task<Result<BloqueHorarioResponse>> MoverBloqueAsync(Guid id, MoverBloqueRequest request, CancellationToken ct)
    {
        if (await moverValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var bloque = await repository.BloqueAsync(id, ct);
        var asignacion = bloque is null ? null : await repository.AsignacionAsync(bloque.AsignacionId, ct);
        if (bloque is null || asignacion is null)
        {
            return BloqueNoEncontrado(id);
        }

        if (await CruceAsync(asignacion, bloque.Id, request.Dia, request.Inicio, request.Fin, ct) is { } cruce)
        {
            return cruce;
        }

        bloque.Mover(request.Dia, request.Inicio, request.Fin);
        await unitOfWork.SaveChangesAsync(ct);
        return (await ResponderAsync([asignacion], ct, [bloque])).Bloques[0];
    }

    public async Task<Result<Unit>> EliminarBloqueAsync(Guid id, CancellationToken ct)
    {
        var bloque = await repository.BloqueAsync(id, ct);
        if (bloque is null)
        {
            return BloqueNoEncontrado(id);
        }

        repository.Eliminar(bloque);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Result<HorarioResponse>> MiHorarioAsync(MiembroCampus actor, Guid? estudianteId, CancellationToken ct)
    {
        var anio = FechaColombia.AnioLectivo(reloj);
        if (actor.Rol == RolCampus.Docente)
        {
            return await HorarioAsync(anio, actor.Id, null, null, ct);
        }

        var estudiante = actor;
        if (actor.Rol != RolCampus.Estudiante || estudianteId is not null)
        {
            if (estudianteId is not { } idEstudiante)
            {
                return Error.Campo("EstudianteId", "Elija el estudiante.");
            }

            var visible = await acceso.EstudianteVisibleAsync(actor, idEstudiante, ct);
            if (!visible.IsSuccess)
            {
                return visible.Error;
            }

            estudiante = visible.Value;
        }

        if (estudiante.Grado is not { } grado)
        {
            return Resultado.Exito(new HorarioResponse([], []));
        }

        return await HorarioAsync(anio, null, grado, estudiante.Grupo, ct);
    }

    /// <summary>Busca otra clase del mismo docente o del mismo curso que ocupe esa franja.</summary>
    private async Task<Error?> CruceAsync(AsignacionDocente asignacion, Guid? excluirBloqueId, int dia, TimeOnly inicio, TimeOnly fin, CancellationToken ct)
    {
        var delDocente = await repository.AsignacionesAsync(asignacion.Anio, asignacion.DocenteId, null, null, ct);

        // Con grupo: las clases de ese grupo y las de todo el grado. Sin grupo (todo el grado): las de cualquier grupo.
        var delCurso = await repository.AsignacionesAsync(asignacion.Anio, null, asignacion.Grado, asignacion.Grupo, ct);

        var porId = delDocente.Concat(delCurso).DistinctBy(a => a.Id).ToDictionary(a => a.Id);
        var bloques = await repository.BloquesAsync([.. porId.Keys], ct);
        var cruce = bloques.FirstOrDefault(b => b.Id != excluirBloqueId && b.SeCruzaCon(dia, inicio, fin));
        if (cruce is null)
        {
            return null;
        }

        var otra = porId[cruce.AsignacionId];
        var franja = $"{cruce.Inicio:HH\\:mm}–{cruce.Fin:HH\\:mm}";
        var detalle = (await ResponderAsync([otra], ct, [cruce])).Bloques[0];
        var curso = Curso(detalle.Grado, detalle.Grupo);
        var mensaje = otra.DocenteId == asignacion.DocenteId
            ? $"{detalle.DocenteNombre} ya tiene clase de {detalle.Asignatura} con {curso} en esa franja ({franja})."
            : $"{curso} ya tiene clase de {detalle.Asignatura} con {detalle.DocenteNombre} en esa franja ({franja}).";
        return Error.Conflicto("Horario.Cruce", mensaje);
    }

    private async Task<HorarioResponse> ResponderAsync(
        IReadOnlyList<AsignacionDocente> asignaciones, CancellationToken ct, IReadOnlyList<BloqueHorario>? bloques = null)
    {
        bloques ??= await repository.BloquesAsync([.. asignaciones.Select(a => a.Id)], ct);
        var docentes = (await miembros.ObtenerPorIdsAsync([.. asignaciones.Select(a => a.DocenteId).Distinct()], ct)).ToDictionary(d => d.Id);
        var asignaturas = (await repository.AsignaturasAsync(soloActivas: false, ct)).ToDictionary(a => a.Id);
        var porId = asignaciones.ToDictionary(a => a.Id);

        AsignacionResponse Asignacion(AsignacionDocente a)
        {
            asignaturas.TryGetValue(a.AsignaturaId, out var s);
            return new AsignacionResponse(
                a.Id, a.Anio, a.DocenteId, docentes.TryGetValue(a.DocenteId, out var d) ? d.NombreCompleto : "(sin docente)",
                a.AsignaturaId, s?.Nombre ?? "(asignatura)", s?.Area ?? string.Empty, a.Grado, a.Grupo, a.IntensidadHoraria);
        }

        var respuestas = asignaciones.ToDictionary(a => a.Id, Asignacion);
        var horas = bloques.GroupBy(b => b.AsignacionId).ToDictionary(g => g.Key, g => g.Sum(b => b.Horas));

        var lista = respuestas.Values
            .OrderBy(a => a.Grado).ThenBy(a => a.Grupo).ThenBy(a => a.Asignatura)
            .Select(a => new AsignacionHorarioResponse(a, Math.Round(horas.GetValueOrDefault(a.Id), 2)))
            .ToList();

        var detalle = bloques
            .Where(b => porId.ContainsKey(b.AsignacionId))
            .OrderBy(b => b.Dia).ThenBy(b => b.Inicio)
            .Select(b =>
            {
                var a = respuestas[b.AsignacionId];
                return new BloqueHorarioResponse(b.Id, b.AsignacionId, b.Dia, b.Inicio, b.Fin, a.Asignatura, a.Grado, a.Grupo, a.DocenteId, a.DocenteNombre);
            })
            .ToList();

        return new HorarioResponse(lista, detalle);
    }

    private static string Curso(int grado, string? grupo) =>
        GradosEscolares.Nombre(grado) + (grupo is null ? string.Empty : $" {grupo}");

    private static Error BloqueNoEncontrado(Guid id) =>
        Error.NoEncontrado("Horario.BloqueNoEncontrado", $"No encontramos la clase {id} en el horario.");
}
