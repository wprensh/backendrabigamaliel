using RabiGamaliel.Domain.Elecciones;

namespace RabiGamaliel.Application.Elecciones;

public sealed record ResumenEleccion(Guid EleccionId, int Candidatos, int Votantes);

public sealed record ConteoVotos(CargoEleccion Cargo, string Ambito, Guid? CandidatoId, int Votos);

/// <summary>Puerto de persistencia de elecciones, candidatos y votos (lo implementa Infrastructure).</summary>
public interface IEleccionRepository
{
    Task<Eleccion?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    /// <summary>Todas, de la más reciente a la más antigua (un colegio tiene pocas por año).</summary>
    Task<List<Eleccion>> ListarAsync(CancellationToken ct);

    Task<List<ResumenEleccion>> ResumirAsync(IReadOnlyCollection<Guid> eleccionIds, CancellationToken ct);

    void Agregar(Eleccion eleccion);

    /// <summary>Elimina la elección y sus candidatos.</summary>
    Task EliminarAsync(Eleccion eleccion, CancellationToken ct);

    Task<List<Candidato>> CandidatosAsync(Guid eleccionId, CancellationToken ct);

    Task<Candidato?> ObtenerCandidatoAsync(Guid eleccionId, Guid candidatoId, CancellationToken ct);

    void AgregarCandidato(Candidato candidato);

    void EliminarCandidato(Candidato candidato);

    Task<bool> TieneVotosAsync(Guid eleccionId, CancellationToken ct);

    Task<bool> YaVotoAsync(Guid eleccionId, Guid estudianteId, CancellationToken ct);

    /// <summary>Elecciones en las que votó el estudiante, de entre las indicadas.</summary>
    Task<HashSet<Guid>> EleccionesVotadasAsync(Guid estudianteId, IReadOnlyCollection<Guid> eleccionIds, CancellationToken ct);

    /// <summary>
    /// Guarda la participación y los votos en una sola transacción. Devuelve false si el estudiante
    /// ya había votado (dos envíos casi al mismo tiempo): el índice único lo impide.
    /// </summary>
    Task<bool> RegistrarVotoAsync(ParticipacionVoto participacion, IReadOnlyList<Voto> votos, CancellationToken ct);

    Task<List<ConteoVotos>> ContarVotosAsync(Guid eleccionId, CancellationToken ct);

    Task<Dictionary<int, int>> VotantesPorGradoAsync(Guid eleccionId, CancellationToken ct);
}
