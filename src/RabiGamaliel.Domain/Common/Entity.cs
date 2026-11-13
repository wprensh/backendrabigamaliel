namespace RabiGamaliel.Domain.Common;

/// <summary>
/// Clase base de todas las entidades del dominio.
/// Usa identificadores <see cref="Guid"/> versión 7 (ordenables por tiempo, .NET 9),
/// lo que mejora el rendimiento de los índices en PostgreSQL frente a Guid v4.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; protected init; } = Guid.CreateVersion7();

    /// <summary>Fecha de creación en UTC. La asigna el interceptor de auditoría.</summary>
    public DateTimeOffset CreadoEn { get; private set; }

    /// <summary>Fecha de la última modificación en UTC. La asigna el interceptor de auditoría.</summary>
    public DateTimeOffset? ModificadoEn { get; private set; }

    public void MarcarCreado(DateTimeOffset ahora) => CreadoEn = ahora;

    public void MarcarModificado(DateTimeOffset ahora) => ModificadoEn = ahora;
}
