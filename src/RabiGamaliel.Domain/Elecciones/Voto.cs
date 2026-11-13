using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Elecciones;

/// <summary>
/// Constancia de que un estudiante ya votó en una elección. Se guarda aparte del voto y sin
/// relación con él: se sabe quién votó, pero no por quién.
/// </summary>
public sealed class ParticipacionVoto : Entity
{
    private ParticipacionVoto()
    {
    }

    public Guid EleccionId { get; private set; }

    public Guid EstudianteId { get; private set; }

    /// <summary>Grado del estudiante cuando votó, para el reporte de participación por grado.</summary>
    public int Grado { get; private set; }

    public static ParticipacionVoto Registrar(Guid eleccionId, Guid estudianteId, int grado) =>
        new() { EleccionId = eleccionId, EstudianteId = estudianteId, Grado = grado };
}

/// <summary>
/// Voto secreto por un cargo. No hereda de <see cref="Entity"/> a propósito: no tiene fecha ni un id
/// ordenado por tiempo, así no se puede emparejar con la <see cref="ParticipacionVoto"/> de quien lo emitió.
/// </summary>
public sealed class Voto
{
    private Voto()
    {
    }

    /// <summary>Id aleatorio (Guid v4), sin información de orden.</summary>
    public Guid Id { get; private init; }

    public Guid EleccionId { get; private set; }

    public CargoEleccion Cargo { get; private set; }

    public string Ambito { get; private set; } = string.Empty;

    /// <summary>Null es voto en blanco.</summary>
    public Guid? CandidatoId { get; private set; }

    public static Voto Emitir(Guid eleccionId, CargoEleccion cargo, string ambito, Guid? candidatoId) =>
        new() { Id = Guid.NewGuid(), EleccionId = eleccionId, Cargo = cargo, Ambito = ambito, CandidatoId = candidatoId };
}
