using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Certificaciones;

namespace RabiGamaliel.Application.Certificaciones;

/// <summary>Puerto de persistencia de certificados (lo implementa Infrastructure).</summary>
public interface ICertificadoRepository
{
    Task<Certificado?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<Certificado?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct);

    Task<PagedResult<Certificado>> ListarAsync(CertificadoQuery query, CancellationToken ct);

    void Agregar(Certificado certificado);
}
