namespace RabiGamaliel.Application.Abstractions;

/// <summary>
/// Confirma en una sola transacción los cambios registrados por los repositorios.
/// La implementa el DbContext en la capa de infraestructura.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
