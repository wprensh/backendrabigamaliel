using RabiGamaliel.Domain.Simulacros;

namespace RabiGamaliel.Application.Simulacros;

public interface ISimulacroRepository
{
    Task<List<Simulacro>> ListarAsync(CancellationToken ct);

    Task<Simulacro?> ObtenerAsync(Guid id, CancellationToken ct);

    void Agregar(Simulacro simulacro);

    void Eliminar(Simulacro simulacro);

    Task<List<PreguntaSimulacro>> PreguntasAsync(Guid simulacroId, CancellationToken ct);

    void Agregar(PreguntaSimulacro pregunta);

    void Eliminar(PreguntaSimulacro pregunta);

    Task<Dictionary<Guid, int>> ConteoPreguntasAsync(CancellationToken ct);

    /// <summary>Intentos entregados por simulacro.</summary>
    Task<Dictionary<Guid, int>> ConteoPresentadosAsync(CancellationToken ct);

    Task<bool> TieneIntentosAsync(Guid simulacroId, CancellationToken ct);

    Task<List<IntentoSimulacro>> IntentosAsync(Guid simulacroId, CancellationToken ct);

    Task<IntentoSimulacro?> IntentoAsync(Guid simulacroId, Guid estudianteId, CancellationToken ct);

    Task<List<IntentoSimulacro>> IntentosDeEstudianteAsync(Guid estudianteId, CancellationToken ct);

    /// <summary>Guarda un intento nuevo; false si el estudiante ya tenía uno (dos pestañas a la vez).</summary>
    Task<bool> AgregarIntentoAsync(IntentoSimulacro intento, CancellationToken ct);
}

public interface IBancoPreguntasRepository
{
    Task<List<PreguntaBanco>> ListarAsync(CancellationToken ct);

    Task<List<PreguntaBanco>> ObtenerAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);

    Task<PreguntaBanco?> ObtenerAsync(Guid id, CancellationToken ct);

    void Agregar(PreguntaBanco pregunta);

    void Eliminar(PreguntaBanco pregunta);

    /// <summary>En cuántos simulacros se ha usado cada pregunta del banco.</summary>
    Task<Dictionary<Guid, int>> UsosAsync(CancellationToken ct);
}
