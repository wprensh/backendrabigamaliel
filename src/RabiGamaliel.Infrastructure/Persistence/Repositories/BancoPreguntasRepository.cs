using RabiGamaliel.Application.Simulacros;
using RabiGamaliel.Domain.Simulacros;
using Microsoft.EntityFrameworkCore;

namespace RabiGamaliel.Infrastructure.Persistence.Repositories;

internal sealed class BancoPreguntasRepository(ApplicationDbContext db) : IBancoPreguntasRepository
{
    public Task<List<PreguntaBanco>> ListarAsync(CancellationToken ct) => db.PreguntasBanco.ToListAsync(ct);

    public Task<List<PreguntaBanco>> ObtenerAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        db.PreguntasBanco.Where(p => ids.Contains(p.Id)).ToListAsync(ct);

    public Task<PreguntaBanco?> ObtenerAsync(Guid id, CancellationToken ct) => db.PreguntasBanco.FirstOrDefaultAsync(p => p.Id == id, ct);

    public void Agregar(PreguntaBanco pregunta) => db.PreguntasBanco.Add(pregunta);

    public void Eliminar(PreguntaBanco pregunta) => db.PreguntasBanco.Remove(pregunta);

    public Task<Dictionary<Guid, int>> UsosAsync(CancellationToken ct) =>
        db.PreguntasSimulacro.Where(p => p.PreguntaBancoId != null).GroupBy(p => p.PreguntaBancoId!.Value)
            .Select(g => new { g.Key, Total = g.Select(p => p.SimulacroId).Distinct().Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Total, ct);
}
