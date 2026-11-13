using RabiGamaliel.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace RabiGamaliel.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor de EF Core que asigna automáticamente las fechas de auditoría
/// (<see cref="Entity.CreadoEn"/> / <see cref="Entity.ModificadoEn"/>) antes de guardar.
/// Usa <see cref="TimeProvider"/> para que las pruebas puedan controlar el reloj.
/// </summary>
internal sealed class AuditoriaInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AplicarAuditoria(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AplicarAuditoria(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AplicarAuditoria(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var ahora = timeProvider.GetUtcNow();

        foreach (var entry in context.ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.MarcarCreado(ahora);
                    break;
                case EntityState.Modified:
                    entry.Entity.MarcarModificado(ahora);
                    break;
            }
        }
    }
}
