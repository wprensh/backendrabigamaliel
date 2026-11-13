using RabiGamaliel.Application.Common;
using RabiGamaliel.Application.Noticias;
using RabiGamaliel.Domain.Noticias;
using Microsoft.EntityFrameworkCore;

namespace RabiGamaliel.Infrastructure.Persistence.Repositories;

internal sealed class NoticiaRepository(ApplicationDbContext db) : INoticiaRepository
{
    public Task<Noticia?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        db.Noticias.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<PagedResult<Noticia>> ListarAsync(NoticiaQuery query, CancellationToken ct)
    {
        // AsNoTracking: los listados son de solo lectura, no necesitan seguimiento de cambios.
        var consulta = db.Noticias.AsNoTracking();

        if (query.SoloPublicadas)
        {
            consulta = consulta.Where(n => n.Publicada);
        }

        if (query.Categoria is { } categoria)
        {
            consulta = consulta.Where(n => n.Categoria == categoria);
        }

        if (query.Desde is { } desde)
        {
            consulta = consulta.Where(n => n.FechaPublicacion >= desde);
        }

        if (query.Hasta is { } hasta)
        {
            consulta = consulta.Where(n => n.FechaPublicacion <= hasta);
        }

        if (!string.IsNullOrWhiteSpace(query.Buscar))
        {
            // ILIKE de PostgreSQL: búsqueda sin distinguir mayúsculas.
            var patron = $"%{EscaparLike(query.Buscar.Trim())}%";
            consulta = consulta.Where(n =>
                EF.Functions.ILike(n.Titulo, patron, @"\") || EF.Functions.ILike(n.Resumen, patron, @"\"));
        }

        var total = await consulta.CountAsync(ct);

        var ordenada = query.Orden == OrdenNoticias.Antiguas
            ? consulta.OrderBy(n => n.FechaPublicacion).ThenBy(n => n.CreadoEn)
            : consulta.OrderByDescending(n => n.FechaPublicacion).ThenByDescending(n => n.CreadoEn);

        var items = await ordenada
            .Skip((query.Pagina - 1) * query.TamanoPagina)
            .Take(query.TamanoPagina)
            .ToListAsync(ct);

        return new PagedResult<Noticia>(items, query.Pagina, query.TamanoPagina, total);
    }

    public async Task<IReadOnlyList<ArchivoNoticiasItem>> ArchivoAsync(bool soloPublicadas, CancellationToken ct)
    {
        var consulta = db.Noticias.AsNoTracking();
        if (soloPublicadas)
        {
            consulta = consulta.Where(n => n.Publicada);
        }

        // Npgsql traduce DateOnly.Year/Month a date_part: el agrupado se resuelve en PostgreSQL.
        return await consulta
            .GroupBy(n => new { n.FechaPublicacion.Year, n.FechaPublicacion.Month })
            .OrderByDescending(g => g.Key.Year)
            .ThenByDescending(g => g.Key.Month)
            .Select(g => new ArchivoNoticiasItem(g.Key.Year, g.Key.Month, g.Count()))
            .ToListAsync(ct);
    }

    public Task<bool> ExisteTituloAsync(string titulo, Guid? excluirId, CancellationToken ct)
    {
        // Coincidencia exacta sin distinguir mayúsculas: ILIKE con los comodines escapados.
        var patron = EscaparLike(titulo.Trim());
        return db.Noticias.AnyAsync(n => EF.Functions.ILike(n.Titulo, patron, @"\") && n.Id != excluirId, ct);
    }

    /// <summary>Escapa los comodines de LIKE para que el texto del usuario se trate literalmente.</summary>
    private static string EscaparLike(string valor) =>
        valor.Replace(@"\", @"\\", StringComparison.Ordinal)
             .Replace("%", @"\%", StringComparison.Ordinal)
             .Replace("_", @"\_", StringComparison.Ordinal);

    public void Agregar(Noticia noticia) => db.Noticias.Add(noticia);

    public void Eliminar(Noticia noticia) => db.Noticias.Remove(noticia);
}
