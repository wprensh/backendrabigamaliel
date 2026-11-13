using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Campus;

public sealed record ConteoPorGrado(int Grado, int Total);

/// <summary>Puerto de persistencia de estudiantes y docentes del campus (lo implementa Infrastructure).</summary>
public interface IMiembroCampusRepository
{
    Task<MiembroCampus?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<MiembroCampus?> ObtenerPorDocumentoAsync(string documento, CancellationToken ct);

    Task<List<MiembroCampus>> ObtenerPorDocumentosAsync(IReadOnlyCollection<string> documentos, CancellationToken ct);

    Task<PagedResult<MiembroCampus>> ListarAsync(MiembroQuery query, CancellationToken ct);

    /// <summary>Miembros a los que se generará clave, ordenados por grado, grupo y nombre.</summary>
    Task<List<MiembroCampus>> ParaGenerarClavesAsync(GenerarClavesRequest filtro, int maximo, CancellationToken ct);

    /// <summary>Estudiantes activos de los grados indicados que coinciden con la búsqueda.</summary>
    Task<List<MiembroCampus>> BuscarEstudiantesAsync(string? buscar, IReadOnlyCollection<int> grados, int maximo, CancellationToken ct);

    /// <summary>Estudiantes activos por grado: los habilitados para votar.</summary>
    Task<List<ConteoPorGrado>> EstudiantesActivosPorGradoAsync(CancellationToken ct);

    /// <summary>¿Votó, fue candidato o tiene notas, asistencia, observador o simulacros? Entonces no se elimina, se desactiva.</summary>
    Task<bool> TieneHistorialAsync(Guid id, CancellationToken ct);

    Task<List<MiembroCampus>> ObtenerPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);

    /// <summary>Estudiantes activos de un curso, por nombre. Con grupo null, todo el grado.</summary>
    Task<List<MiembroCampus>> EstudiantesDelCursoAsync(int grado, string? grupo, CancellationToken ct);

    /// <summary>Miembros activos de un rol (para calcular destinatarios de circulares).</summary>
    Task<List<MiembroCampus>> ActivosPorRolAsync(RolCampus rol, CancellationToken ct);

    Task<MiembroCampus?> ObtenerPorCodigoCarnetAsync(string codigo, CancellationToken ct);

    // ---------- Acudientes ----------

    Task<List<VinculoResponse>> VinculosAsync(Guid? acudienteId, Guid? estudianteId, CancellationToken ct);

    /// <summary>Vínculos de todos los estudiantes con acudiente entre los ids indicados (o todos, si es null).</summary>
    Task<List<VinculoAcudiente>> VinculosDeEstudiantesAsync(IReadOnlyCollection<Guid>? estudianteIds, CancellationToken ct);

    Task<List<VinculoAcudiente>> VinculosDeAcudienteAsync(Guid acudienteId, CancellationToken ct);

    Task<VinculoAcudiente?> ObtenerVinculoAsync(Guid id, CancellationToken ct);

    void AgregarVinculo(VinculoAcudiente vinculo);

    void EliminarVinculo(VinculoAcudiente vinculo);

    void Agregar(MiembroCampus miembro);

    void Eliminar(MiembroCampus miembro);
}
