using RabiGamaliel.Application.Admisiones;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Admisiones;
using Microsoft.EntityFrameworkCore;

namespace RabiGamaliel.Infrastructure.Persistence.Repositories;

internal sealed class AdmisionRepository(ApplicationDbContext db) : IAdmisionRepository
{
    public Task<SolicitudAdmision?> ObtenerAsync(Guid id, CancellationToken ct) => db.SolicitudesAdmision.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<SolicitudAdmision?> PorRadicadoAsync(string radicado, CancellationToken ct) =>
        db.SolicitudesAdmision.FirstOrDefaultAsync(s => s.Radicado == radicado, ct);

    public Task<bool> ExisteEnCursoAsync(string documentoAspirante, int anioLectivo, CancellationToken ct) =>
        db.SolicitudesAdmision.AnyAsync(
            s => s.AspiranteDocumento == documentoAspirante && s.AnioLectivo == anioLectivo && s.Estado != EstadoAdmision.Rechazada, ct);

    public async Task<PagedResult<SolicitudAdmision>> ListarAsync(AdmisionQuery query, CancellationToken ct)
    {
        var consulta = db.SolicitudesAdmision.AsNoTracking();
        if (query.Estado is { } estado)
        {
            consulta = consulta.Where(s => s.Estado == estado);
        }

        if (query.AnioLectivo is { } anio)
        {
            consulta = consulta.Where(s => s.AnioLectivo == anio);
        }

        if (query.Grado is { } grado)
        {
            consulta = consulta.Where(s => s.GradoSolicitado == grado);
        }

        if (!string.IsNullOrWhiteSpace(query.Buscar))
        {
            var patron = $"%{Like.Escapar(query.Buscar.Trim())}%";
            consulta = consulta.Where(s =>
                EF.Functions.ILike(s.AspiranteNombres + " " + s.AspiranteApellidos, patron, @"\")
                || EF.Functions.ILike(s.AspiranteDocumento, patron, @"\")
                || EF.Functions.ILike(s.Radicado, patron, @"\")
                || EF.Functions.ILike(s.AcudienteNombre, patron, @"\"));
        }

        var total = await consulta.CountAsync(ct);
        var items = await consulta.OrderByDescending(s => s.CreadoEn)
            .Skip((query.Pagina - 1) * query.TamanoPagina).Take(query.TamanoPagina).ToListAsync(ct);
        return new PagedResult<SolicitudAdmision>(items, query.Pagina, query.TamanoPagina, total);
    }

    public Task<List<DocumentoAdmisionResponse>> DocumentosAsync(Guid solicitudId, CancellationToken ct) =>
        db.DocumentosAdmision.Where(d => d.SolicitudId == solicitudId).OrderBy(d => d.CreadoEn)
            .Select(d => new DocumentoAdmisionResponse(d.Id, d.Tipo, d.NombreArchivo, d.TipoContenido, d.Tamano, d.CreadoEn))
            .ToListAsync(ct);

    public Task<DocumentoAdmision?> DocumentoAsync(Guid solicitudId, Guid documentoId, CancellationToken ct) =>
        db.DocumentosAdmision.AsNoTracking().FirstOrDefaultAsync(d => d.SolicitudId == solicitudId && d.Id == documentoId, ct);

    public Task<List<ConteoEstadoAdmision>> ConteoPorEstadoAsync(int? anioLectivo, CancellationToken ct) =>
        db.SolicitudesAdmision.Where(s => anioLectivo == null || s.AnioLectivo == anioLectivo)
            .GroupBy(s => s.Estado).Select(g => new ConteoEstadoAdmision(g.Key, g.Count())).ToListAsync(ct);

    public void Agregar(SolicitudAdmision solicitud) => db.SolicitudesAdmision.Add(solicitud);

    public void Agregar(DocumentoAdmision documento) => db.DocumentosAdmision.Add(documento);

    public void Eliminar(SolicitudAdmision solicitud) => db.SolicitudesAdmision.Remove(solicitud);
}
