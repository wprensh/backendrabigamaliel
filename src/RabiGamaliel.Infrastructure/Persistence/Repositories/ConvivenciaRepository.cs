using RabiGamaliel.Application.Common;
using RabiGamaliel.Application.Convivencia;
using RabiGamaliel.Domain.Convivencia;
using Microsoft.EntityFrameworkCore;

namespace RabiGamaliel.Infrastructure.Persistence.Repositories;

internal sealed class ConvivenciaRepository(ApplicationDbContext db) : IConvivenciaRepository
{
    public Task<Anotacion?> ObtenerAsync(Guid id, CancellationToken ct) =>
        db.Anotaciones.Include(a => a.Seguimientos).FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<List<Anotacion>> DeEstudianteAsync(Guid estudianteId, CancellationToken ct) =>
        db.Anotaciones.AsNoTracking().Include(a => a.Seguimientos).Where(a => a.EstudianteId == estudianteId).ToListAsync(ct);

    public async Task<PagedResult<Anotacion>> ListarAsync(AnotacionQuery query, CancellationToken ct)
    {
        var consulta = db.Anotaciones.AsNoTracking();
        if (query.Tipo is { } tipo)
        {
            consulta = consulta.Where(a => a.Tipo == tipo);
        }
        else if (query.SoloSituaciones)
        {
            consulta = consulta.Where(a => a.Tipo == TipoAnotacion.SituacionTipoI || a.Tipo == TipoAnotacion.SituacionTipoII || a.Tipo == TipoAnotacion.SituacionTipoIII);
        }

        if (query.Estado is { } estado)
        {
            consulta = consulta.Where(a => a.Estado == estado);
        }

        if (query.Grado is { } grado)
        {
            consulta = consulta.Where(a => a.Grado == grado);
        }

        if (query.Grupo is { } grupo)
        {
            consulta = consulta.Where(a => a.Grupo == grupo);
        }

        if (!string.IsNullOrWhiteSpace(query.Buscar))
        {
            var patron = $"%{Like.Escapar(query.Buscar.Trim())}%";
            var ids = db.MiembrosCampus
                .Where(m => EF.Functions.ILike(m.NombreCompleto, patron, @"\") || EF.Functions.ILike(m.Documento, patron, @"\"))
                .Select(m => m.Id);
            consulta = consulta.Where(a => ids.Contains(a.EstudianteId));
        }

        var total = await consulta.CountAsync(ct);
        var items = await consulta.Include(a => a.Seguimientos)
            .OrderByDescending(a => a.Fecha).ThenByDescending(a => a.CreadoEn)
            .Skip((query.Pagina - 1) * query.TamanoPagina).Take(query.TamanoPagina)
            .ToListAsync(ct);
        return new PagedResult<Anotacion>(items, query.Pagina, query.TamanoPagina, total);
    }

    public void Agregar(Anotacion anotacion) => db.Anotaciones.Add(anotacion);

    public void Agregar(SeguimientoAnotacion seguimiento) => db.SeguimientosAnotacion.Add(seguimiento);
}

internal static class Like
{
    public static string Escapar(string valor) =>
        valor.Replace(@"\", @"\\", StringComparison.Ordinal)
             .Replace("%", @"\%", StringComparison.Ordinal)
             .Replace("_", @"\_", StringComparison.Ordinal);
}
