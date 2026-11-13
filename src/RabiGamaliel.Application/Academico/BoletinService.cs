using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Avisos;
using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Academico;

public interface IBoletinService
{
    Task<IReadOnlyList<BoletinResponse>> CalcularAsync(
        IReadOnlyList<MiembroCampus> estudiantes, PeriodoAcademico periodo, bool incluirNoPublicados, CancellationToken ct);

    Task<BoletinesDisponiblesResponse> DisponiblesAsync(MiembroCampus actor, CancellationToken ct);

    Task<Result<BoletinResponse>> BoletinAsync(MiembroCampus actor, Guid estudianteId, Guid periodoId, CancellationToken ct);

    /// <summary>Avisa a estudiantes y acudientes que el boletín del periodo ya se puede consultar (no guarda cambios).</summary>
    Task AvisarPublicacionAsync(PeriodoAcademico periodo, CancellationToken ct);
}

/// <summary>Boletín de calificaciones por periodo, con nota acumulada, desempeño, fallas y puesto en el curso.</summary>
public sealed class BoletinService(
    IAcademicoRepository repository,
    IMiembroCampusRepository miembros,
    IAccesoEstudiantes acceso,
    INotificador notificador,
    TimeProvider reloj) : IBoletinService
{
    public async Task<IReadOnlyList<BoletinResponse>> CalcularAsync(
        IReadOnlyList<MiembroCampus> estudiantes, PeriodoAcademico periodo, bool incluirNoPublicados, CancellationToken ct)
    {
        var periodos = (await repository.PeriodosAsync(periodo.Anio, ct))
            .Where(p => p.Numero <= periodo.Numero && (p.Id == periodo.Id || p.Publicado || incluirNoPublicados))
            .OrderBy(p => p.Numero)
            .ToList();
        var columnas = periodos.Select(p => new PeriodoColumna(p.Id, p.Numero, p.Nombre, p.Porcentaje)).ToList();
        var asignaturas = (await repository.AsignaturasAsync(soloActivas: false, ct)).ToDictionary(a => a.Id);
        var ahora = reloj.GetUtcNow();
        var resultado = new List<BoletinResponse>(estudiantes.Count);

        foreach (var curso in estudiantes.Where(e => e.Grado is not null).GroupBy(e => (Grado: e.Grado!.Value, e.Grupo)))
        {
            var companeros = await miembros.EstudiantesDelCursoAsync(curso.Key.Grado, curso.Key.Grupo, ct);
            var idsCurso = companeros.Select(c => c.Id).Concat(curso.Select(e => e.Id)).Distinct().ToList();
            var asignaciones = (await repository.AsignacionesAsync(periodo.Anio, null, curso.Key.Grado, curso.Key.Grupo, ct))
                .GroupBy(a => a.AsignaturaId).ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.Grupo is not null).First());
            var docentes = (await miembros.ObtenerPorIdsAsync([.. asignaciones.Values.Select(a => a.DocenteId).Distinct()], ct))
                .ToDictionary(d => d.Id, d => d.NombreCompleto);
            var calificaciones = await repository.CalificacionesAsync(idsCurso, [.. periodos.Select(p => p.Id)], null, ct);
            var notas = calificaciones.ToDictionary(c => (c.EstudianteId, c.AsignaturaId, c.PeriodoId));
            var fallasRegistradas = (await repository.FallasAsync([.. curso.Select(e => e.Id)], periodo.Inicio, periodo.Fin, null, ct))
                .ToDictionary(f => (f.EstudianteId, f.AsignaturaId), f => f.Fallas);

            var idsAsignaturas = asignaciones.Keys
                .Concat(calificaciones.Where(c => curso.Any(e => e.Id == c.EstudianteId)).Select(c => c.AsignaturaId))
                .Distinct()
                .Where(asignaturas.ContainsKey)
                .Select(id => asignaturas[id])
                .OrderBy(a => a.Orden).ThenBy(a => a.Nombre)
                .ToList();

            // Puesto en el curso: promedio de las notas del periodo de todas las asignaturas del curso.
            var promedios = idsCurso.ToDictionary(
                id => id,
                id => CalculoBoletin.Promedio(idsAsignaturas.Select(a => notas.GetValueOrDefault((id, a.Id, periodo.Id))?.Nota)));
            var puestos = CalculoBoletin.Puestos(promedios);

            foreach (var estudiante in curso)
            {
                var filas = idsAsignaturas.Select(a =>
                {
                    var actual = notas.GetValueOrDefault((estudiante.Id, a.Id, periodo.Id));
                    var porPeriodo = periodos.Select(p => notas.GetValueOrDefault((estudiante.Id, a.Id, p.Id))?.Nota).ToList();
                    var asignacion = asignaciones.GetValueOrDefault(a.Id);
                    return new AsignaturaBoletin(
                        a.Id,
                        a.Nombre,
                        asignacion is not null && docentes.TryGetValue(asignacion.DocenteId, out var docente) ? docente : null,
                        asignacion?.IntensidadHoraria,
                        porPeriodo,
                        actual?.Nota,
                        CalculoBoletin.Desempeno(actual?.Nota),
                        actual?.Fallas ?? fallasRegistradas.GetValueOrDefault((estudiante.Id, a.Id)),
                        actual?.Observacion,
                        CalculoBoletin.Acumulada([.. periodos.Select((p, i) => (porPeriodo[i], p.Porcentaje))]));
                }).ToList();

                var areas = filas
                    .GroupBy(f => asignaturas[f.AsignaturaId].Area)
                    .Select(g => new AreaBoletin(g.Key, [.. g]))
                    .ToList();

                resultado.Add(new BoletinResponse(
                    new EstudianteBoletin(estudiante.Id, estudiante.NombreCompleto, estudiante.Documento, estudiante.Grado, estudiante.Grupo),
                    PeriodoResponse.Desde(periodo),
                    columnas,
                    areas,
                    promedios[estudiante.Id],
                    CalculoBoletin.Desempeno(promedios[estudiante.Id]),
                    puestos.TryGetValue(estudiante.Id, out var puesto) ? puesto : null,
                    idsCurso.Count,
                    filas.Sum(f => f.Fallas),
                    filas.Count(f => CalculoBoletin.Perdida(f.Nota)),
                    ahora));
            }
        }

        return resultado;
    }

    public async Task<BoletinesDisponiblesResponse> DisponiblesAsync(MiembroCampus actor, CancellationToken ct)
    {
        var estudiantes = await acceso.EstudiantesPropiosAsync(actor, ct);
        var periodos = (await repository.PeriodosAsync(FechaColombia.AnioLectivo(reloj), ct)).Where(p => p.Publicado);
        return new BoletinesDisponiblesResponse(
            [.. estudiantes.Select(e => new EstudianteBoletin(e.Id, e.NombreCompleto, e.Documento, e.Grado, e.Grupo))],
            [.. periodos.OrderBy(p => p.Numero).Select(PeriodoResponse.Desde)]);
    }

    public async Task<Result<BoletinResponse>> BoletinAsync(MiembroCampus actor, Guid estudianteId, Guid periodoId, CancellationToken ct)
    {
        var estudiante = await acceso.EstudianteVisibleAsync(actor, estudianteId, ct);
        if (!estudiante.IsSuccess)
        {
            return estudiante.Error;
        }

        var periodo = await repository.PeriodoAsync(periodoId, ct);
        if (periodo is null || (!periodo.Publicado && actor.Rol != RolCampus.Docente))
        {
            return Error.NoEncontrado("Boletin.NoPublicado", "El boletín de ese periodo todavía no está publicado.");
        }

        var boletines = await CalcularAsync([estudiante.Value], periodo, incluirNoPublicados: actor.Rol == RolCampus.Docente, ct);
        return boletines.Count == 0
            ? Error.NoEncontrado("Boletin.SinCurso", "El estudiante no tiene grado asignado.")
            : boletines[0];
    }

    public async Task AvisarPublicacionAsync(PeriodoAcademico periodo, CancellationToken ct)
    {
        var estudiantes = await miembros.ActivosPorRolAsync(RolCampus.Estudiante, ct);
        var vinculos = await miembros.VinculosDeEstudiantesAsync(null, ct);
        var acudientes = (await miembros.ActivosPorRolAsync(RolCampus.Acudiente, ct)).ToDictionary(a => a.Id);
        var titulo = $"Boletín del {periodo.Nombre.ToLowerInvariant()} disponible";

        notificador.Notificar(
            estudiantes, TipoAviso.Calificaciones, titulo,
            $"Ya puede consultar sus calificaciones del {periodo.Nombre.ToLowerInvariant()} en el campus virtual.",
            "/campus/boletines", null, porCorreo: false);

        foreach (var grupo in vinculos.Where(v => acudientes.ContainsKey(v.AcudienteId)).GroupBy(v => v.AcudienteId))
        {
            notificador.Notificar(
                [acudientes[grupo.Key]], TipoAviso.Calificaciones, titulo,
                $"Ya puede consultar el boletín del {periodo.Nombre.ToLowerInvariant()} de sus estudiantes en el campus virtual y descargarlo en PDF.",
                "/campus/boletines", null, porCorreo: true);
        }
    }
}
