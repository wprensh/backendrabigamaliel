using RabiGamaliel.Application.Elecciones;
using RabiGamaliel.Domain.Elecciones;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace RabiGamaliel.Infrastructure.Persistence.Repositories;

internal sealed class EleccionRepository(ApplicationDbContext db) : IEleccionRepository
{
    public Task<Eleccion?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        db.Elecciones.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<List<Eleccion>> ListarAsync(CancellationToken ct) =>
        db.Elecciones.AsNoTracking().OrderByDescending(e => e.Inicio).ToListAsync(ct);

    public async Task<List<ResumenEleccion>> ResumirAsync(IReadOnlyCollection<Guid> eleccionIds, CancellationToken ct)
    {
        var candidatos = await db.Candidatos.Where(c => eleccionIds.Contains(c.EleccionId))
            .GroupBy(c => c.EleccionId).Select(g => new { g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Total, ct);
        var votantes = await db.ParticipacionesVoto.Where(p => eleccionIds.Contains(p.EleccionId))
            .GroupBy(p => p.EleccionId).Select(g => new { g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Total, ct);

        return [.. eleccionIds.Select(id => new ResumenEleccion(id, candidatos.GetValueOrDefault(id), votantes.GetValueOrDefault(id)))];
    }

    public void Agregar(Eleccion eleccion) => db.Elecciones.Add(eleccion);

    public async Task EliminarAsync(Eleccion eleccion, CancellationToken ct)
    {
        db.Candidatos.RemoveRange(await db.Candidatos.Where(c => c.EleccionId == eleccion.Id).ToListAsync(ct));
        db.Elecciones.Remove(eleccion);
    }

    public Task<List<Candidato>> CandidatosAsync(Guid eleccionId, CancellationToken ct) =>
        db.Candidatos.AsNoTracking().Where(c => c.EleccionId == eleccionId).ToListAsync(ct);

    public Task<Candidato?> ObtenerCandidatoAsync(Guid eleccionId, Guid candidatoId, CancellationToken ct) =>
        db.Candidatos.FirstOrDefaultAsync(c => c.EleccionId == eleccionId && c.Id == candidatoId, ct);

    public void AgregarCandidato(Candidato candidato) => db.Candidatos.Add(candidato);

    public void EliminarCandidato(Candidato candidato) => db.Candidatos.Remove(candidato);

    public Task<bool> TieneVotosAsync(Guid eleccionId, CancellationToken ct) =>
        db.ParticipacionesVoto.AnyAsync(p => p.EleccionId == eleccionId, ct);

    public Task<bool> YaVotoAsync(Guid eleccionId, Guid estudianteId, CancellationToken ct) =>
        db.ParticipacionesVoto.AnyAsync(p => p.EleccionId == eleccionId && p.EstudianteId == estudianteId, ct);

    public async Task<HashSet<Guid>> EleccionesVotadasAsync(
        Guid estudianteId, IReadOnlyCollection<Guid> eleccionIds, CancellationToken ct) =>
        [
            .. await db.ParticipacionesVoto
                .Where(p => p.EstudianteId == estudianteId && eleccionIds.Contains(p.EleccionId))
                .Select(p => p.EleccionId)
                .ToListAsync(ct),
        ];

    public async Task<bool> RegistrarVotoAsync(ParticipacionVoto participacion, IReadOnlyList<Voto> votos, CancellationToken ct)
    {
        db.ParticipacionesVoto.Add(participacion);
        db.Votos.AddRange(votos);
        try
        {
            // Un solo SaveChanges: la participación y los votos quedan juntos o no queda nada.
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.Entry(participacion).State = EntityState.Detached;
            foreach (var voto in votos)
            {
                db.Entry(voto).State = EntityState.Detached;
            }

            return false;
        }
    }

    public Task<List<ConteoVotos>> ContarVotosAsync(Guid eleccionId, CancellationToken ct) =>
        db.Votos
            .Where(v => v.EleccionId == eleccionId)
            .GroupBy(v => new { v.Cargo, v.Ambito, v.CandidatoId })
            .Select(g => new ConteoVotos(g.Key.Cargo, g.Key.Ambito, g.Key.CandidatoId, g.Count()))
            .ToListAsync(ct);

    public Task<Dictionary<int, int>> VotantesPorGradoAsync(Guid eleccionId, CancellationToken ct) =>
        db.ParticipacionesVoto
            .Where(p => p.EleccionId == eleccionId)
            .GroupBy(p => p.Grado)
            .Select(g => new { g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Total, ct);
}
