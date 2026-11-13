using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Campus;

/// <summary>Relación entre un acudiente y un estudiante: el acudiente ve el boletín, la asistencia y el observador.</summary>
public sealed class VinculoAcudiente : Entity
{
    public const int ParentescoMaxLength = 30;

    private VinculoAcudiente()
    {
    }

    public Guid AcudienteId { get; private set; }

    public Guid EstudianteId { get; private set; }

    /// <summary>Madre, padre, abuela, tío... Texto libre.</summary>
    public string? Parentesco { get; private set; }

    public static VinculoAcudiente Crear(Guid acudienteId, Guid estudianteId, string? parentesco) => new()
    {
        AcudienteId = acudienteId,
        EstudianteId = estudianteId,
        Parentesco = Limpiar(parentesco),
    };

    public void CambiarParentesco(string? parentesco) => Parentesco = Limpiar(parentesco);

    private static string? Limpiar(string? parentesco) =>
        string.IsNullOrWhiteSpace(parentesco) ? null : parentesco.Trim()[..Math.Min(parentesco.Trim().Length, ParentescoMaxLength)];
}
