using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Academico;

/// <summary>
/// Quién puede ver a qué estudiante en el campus: el estudiante a sí mismo, el acudiente a sus estudiantes y el docente
/// a los estudiantes de los cursos donde dicta clase este año.
/// </summary>
public interface IAccesoEstudiantes
{
    Task<Result<MiembroCampus>> EstudianteVisibleAsync(MiembroCampus actor, Guid estudianteId, CancellationToken ct);

    /// <summary>Estudiantes propios: el mismo estudiante o los del acudiente (vacío para docentes).</summary>
    Task<List<MiembroCampus>> EstudiantesPropiosAsync(MiembroCampus actor, CancellationToken ct);
}

public sealed class AccesoEstudiantes(
    IMiembroCampusRepository miembros, IAcademicoRepository academico, TimeProvider reloj) : IAccesoEstudiantes
{
    public static readonly Error NoVisible =
        Error.NoEncontrado("Estudiante.NoVisible", "No encontramos a ese estudiante entre los suyos.");

    public async Task<Result<MiembroCampus>> EstudianteVisibleAsync(MiembroCampus actor, Guid estudianteId, CancellationToken ct)
    {
        if (actor.Rol == RolCampus.Estudiante)
        {
            return actor.Id == estudianteId ? actor : NoVisible;
        }

        var estudiante = await miembros.ObtenerPorIdAsync(estudianteId, ct);
        if (estudiante is not { Rol: RolCampus.Estudiante })
        {
            return NoVisible;
        }

        if (actor.Rol == RolCampus.Acudiente)
        {
            var vinculos = await miembros.VinculosDeAcudienteAsync(actor.Id, ct);
            return vinculos.Any(v => v.EstudianteId == estudianteId) ? estudiante : NoVisible;
        }

        var asignaciones = await academico.AsignacionesAsync(FechaColombia.AnioLectivo(reloj), actor.Id, null, null, ct);
        return asignaciones.Any(a => a.Incluye(estudiante.Grado, estudiante.Grupo)) ? estudiante : NoVisible;
    }

    public async Task<List<MiembroCampus>> EstudiantesPropiosAsync(MiembroCampus actor, CancellationToken ct)
    {
        if (actor.Rol == RolCampus.Estudiante)
        {
            return [actor];
        }

        if (actor.Rol != RolCampus.Acudiente)
        {
            return [];
        }

        var ids = (await miembros.VinculosDeAcudienteAsync(actor.Id, ct)).Select(v => v.EstudianteId).ToList();
        return [.. (await miembros.ObtenerPorIdsAsync(ids, ct)).Where(e => e.Activo).OrderBy(e => e.NombreCompleto)];
    }
}
