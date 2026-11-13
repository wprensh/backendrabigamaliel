using RabiGamaliel.Application.Certificaciones;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Certificaciones;
using Microsoft.EntityFrameworkCore;

namespace RabiGamaliel.Infrastructure.Persistence.Repositories;

internal sealed class CertificadoRepository(ApplicationDbContext db) : ICertificadoRepository
{
    public Task<Certificado?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        db.Certificados.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Certificado?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct) =>
        db.Certificados.AsNoTracking().FirstOrDefaultAsync(c => c.Codigo == codigo, ct);

    public async Task<PagedResult<Certificado>> ListarAsync(CertificadoQuery query, CancellationToken ct)
    {
        var consulta = db.Certificados.AsNoTracking();

        if (query.Tipo is { } tipo)
        {
            consulta = consulta.Where(c => c.Tipo == tipo);
        }

        if (!string.IsNullOrWhiteSpace(query.Buscar))
        {
            // Busca por nombre, número de documento o código, sin distinguir mayúsculas.
            var patron = $"%{EscaparLike(query.Buscar.Trim())}%";
            consulta = consulta.Where(c =>
                EF.Functions.ILike(c.NombreBeneficiario, patron, @"\")
                || EF.Functions.ILike(c.NumeroDocumento, patron, @"\")
                || EF.Functions.ILike(c.Codigo, patron, @"\"));
        }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderByDescending(c => c.CreadoEn)
            .Skip((query.Pagina - 1) * query.TamanoPagina)
            .Take(query.TamanoPagina)
            .ToListAsync(ct);

        return new PagedResult<Certificado>(items, query.Pagina, query.TamanoPagina, total);
    }

    /// <summary>Escapa los comodines de LIKE para que el texto del usuario se trate literalmente.</summary>
    private static string EscaparLike(string valor) =>
        valor.Replace(@"\", @"\\", StringComparison.Ordinal)
             .Replace("%", @"\%", StringComparison.Ordinal)
             .Replace("_", @"\_", StringComparison.Ordinal);

    public void Agregar(Certificado certificado) => db.Certificados.Add(certificado);
}
