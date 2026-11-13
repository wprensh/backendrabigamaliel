using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Campus;

/// <summary>Estudiantes y acudientes a quienes se avisa de algo que le pasa a un estudiante.</summary>
public static class Familias
{
    /// <summary>Los estudiantes indicados (activos) y sus acudientes activos.</summary>
    public static async Task<List<MiembroCampus>> DeEstudiantesAsync(
        IMiembroCampusRepository miembros, IReadOnlyCollection<Guid> estudianteIds, CancellationToken ct)
    {
        if (estudianteIds.Count == 0)
        {
            return [];
        }

        var vinculos = await miembros.VinculosDeEstudiantesAsync(estudianteIds, ct);
        var ids = estudianteIds.Concat(vinculos.Select(v => v.AcudienteId)).Distinct().ToList();
        return [.. (await miembros.ObtenerPorIdsAsync(ids, ct)).Where(m => m.Activo)];
    }

    public static string Curso(int? grado, string? grupo) =>
        grado is { } g ? $"{GradosEscolares.Nombre(g)} {grupo}".Trim() : string.Empty;
}
