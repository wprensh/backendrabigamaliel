using RabiGamaliel.Application.Simulacros;
using RabiGamaliel.Domain.Simulacros;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace RabiGamaliel.Infrastructure.Persistence.Repositories;

internal sealed class SimulacroRepository(ApplicationDbContext db) : ISimulacroRepository
{
    public Task<List<Simulacro>> ListarAsync(CancellationToken ct) => db.Simulacros.OrderByDescending(s => s.Inicio).ToListAsync(ct);

    public Task<Simulacro?> ObtenerAsync(Guid id, CancellationToken ct) => db.Simulacros.FirstOrDefaultAsync(s => s.Id == id, ct);

    public void Agregar(Simulacro simulacro) => db.Simulacros.Add(simulacro);

    public void Eliminar(Simulacro simulacro) => db.Simulacros.Remove(simulacro);

    public Task<List<PreguntaSimulacro>> PreguntasAsync(Guid simulacroId, CancellationToken ct) =>
        db.PreguntasSimulacro.Where(p => p.SimulacroId == simulacroId).OrderBy(p => p.Orden).ToListAsync(ct);

    public void Agregar(PreguntaSimulacro pregunta) => db.PreguntasSimulacro.Add(pregunta);

    public void Eliminar(PreguntaSimulacro pregunta) => db.PreguntasSimulacro.Remove(pregunta);

    public Task<Dictionary<Guid, int>> ConteoPreguntasAsync(CancellationToken ct) =>
        db.PreguntasSimulacro.GroupBy(p => p.SimulacroId).Select(g => new { g.Key, Total = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Total, ct);

    public Task<Dictionary<Guid, int>> ConteoPresentadosAsync(CancellationToken ct) =>
        db.IntentosSimulacro.Where(i => i.EntregadoEn != null).GroupBy(i => i.SimulacroId)
            .Select(g => new { g.Key, Total = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Total, ct);

    public Task<bool> TieneIntentosAsync(Guid simulacroId, CancellationToken ct) => db.IntentosSimulacro.AnyAsync(i => i.SimulacroId == simulacroId, ct);

    public Task<List<IntentoSimulacro>> IntentosAsync(Guid simulacroId, CancellationToken ct) =>
        db.IntentosSimulacro.Where(i => i.SimulacroId == simulacroId).ToListAsync(ct);

    public Task<IntentoSimulacro?> IntentoAsync(Guid simulacroId, Guid estudianteId, CancellationToken ct) =>
        db.IntentosSimulacro.FirstOrDefaultAsync(i => i.SimulacroId == simulacroId && i.EstudianteId == estudianteId, ct);

    public Task<List<IntentoSimulacro>> IntentosDeEstudianteAsync(Guid estudianteId, CancellationToken ct) =>
        db.IntentosSimulacro.Where(i => i.EstudianteId == estudianteId).ToListAsync(ct);

    public async Task<bool> AgregarIntentoAsync(IntentoSimulacro intento, CancellationToken ct)
    {
        db.IntentosSimulacro.Add(intento);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.Entry(intento).State = EntityState.Detached;
            return false;
        }
    }
}
