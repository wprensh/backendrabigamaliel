using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Campus;
using Microsoft.EntityFrameworkCore;

namespace RabiGamaliel.Infrastructure.Persistence.Repositories;

internal sealed class MiembroCampusRepository(ApplicationDbContext db) : IMiembroCampusRepository
{
    public Task<MiembroCampus?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        db.MiembrosCampus.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<MiembroCampus?> ObtenerPorDocumentoAsync(string documento, CancellationToken ct) =>
        db.MiembrosCampus.FirstOrDefaultAsync(m => m.Documento == documento, ct);

    public Task<List<MiembroCampus>> ObtenerPorDocumentosAsync(IReadOnlyCollection<string> documentos, CancellationToken ct) =>
        db.MiembrosCampus.Where(m => documentos.Contains(m.Documento)).ToListAsync(ct);

    public async Task<PagedResult<MiembroCampus>> ListarAsync(MiembroQuery query, CancellationToken ct)
    {
        var consulta = Filtrar(db.MiembrosCampus.AsNoTracking(), query.Rol, query.Grado, query.Grupo);

        if (!string.IsNullOrWhiteSpace(query.Buscar))
        {
            var patron = $"%{EscaparLike(query.Buscar.Trim())}%";
            consulta = consulta.Where(m =>
                EF.Functions.ILike(m.NombreCompleto, patron, @"\") || EF.Functions.ILike(m.Documento, patron, @"\"));
        }

        var total = await consulta.CountAsync(ct);
        var items = await Ordenar(consulta)
            .Skip((query.Pagina - 1) * query.TamanoPagina)
            .Take(query.TamanoPagina)
            .ToListAsync(ct);

        return new PagedResult<MiembroCampus>(items, query.Pagina, query.TamanoPagina, total);
    }

    public Task<List<MiembroCampus>> ParaGenerarClavesAsync(GenerarClavesRequest filtro, int maximo, CancellationToken ct)
    {
        var consulta = db.MiembrosCampus.Where(m => m.Activo);
        consulta = filtro.Ids is { Count: > 0 } ids
            ? consulta.Where(m => ids.Contains(m.Id))
            : Filtrar(consulta, filtro.Rol, filtro.Grado, filtro.Grupo);

        if (filtro.SoloSinClave)
        {
            consulta = consulta.Where(m => m.HashClave == null);
        }

        return Ordenar(consulta).Take(maximo).ToListAsync(ct);
    }

    public Task<List<MiembroCampus>> BuscarEstudiantesAsync(
        string? buscar, IReadOnlyCollection<int> grados, int maximo, CancellationToken ct)
    {
        var consulta = db.MiembrosCampus.AsNoTracking()
            .Where(m => m.Activo && m.Rol == RolCampus.Estudiante && m.Grado != null && grados.Contains(m.Grado.Value));

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var patron = $"%{EscaparLike(buscar.Trim())}%";
            consulta = consulta.Where(m =>
                EF.Functions.ILike(m.NombreCompleto, patron, @"\") || EF.Functions.ILike(m.Documento, patron, @"\"));
        }

        return Ordenar(consulta).Take(maximo).ToListAsync(ct);
    }

    public Task<List<ConteoPorGrado>> EstudiantesActivosPorGradoAsync(CancellationToken ct) =>
        db.MiembrosCampus
            .Where(m => m.Activo && m.Rol == RolCampus.Estudiante && m.Grado != null)
            .GroupBy(m => m.Grado!.Value)
            .Select(g => new ConteoPorGrado(g.Key, g.Count()))
            .ToListAsync(ct);

    public async Task<bool> TieneHistorialAsync(Guid id, CancellationToken ct) =>
        await db.ParticipacionesVoto.AnyAsync(p => p.EstudianteId == id, ct)
        || await db.Candidatos.AnyAsync(c => c.EstudianteId == id, ct)
        || await db.Calificaciones.AnyAsync(c => c.EstudianteId == id, ct)
        || await db.NovedadesAsistencia.AnyAsync(n => n.EstudianteId == id, ct)
        || await db.Anotaciones.AnyAsync(a => a.EstudianteId == id, ct)
        || await db.IntentosSimulacro.AnyAsync(i => i.EstudianteId == id, ct)
        || await db.AsignacionesDocentes.AnyAsync(a => a.DocenteId == id, ct);

    public Task<List<MiembroCampus>> ObtenerPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        ids.Count == 0 ? Task.FromResult(new List<MiembroCampus>()) : db.MiembrosCampus.Where(m => ids.Contains(m.Id)).ToListAsync(ct);

    public Task<List<MiembroCampus>> EstudiantesDelCursoAsync(int grado, string? grupo, CancellationToken ct)
    {
        var consulta = db.MiembrosCampus.AsNoTracking().Where(m => m.Activo && m.Rol == RolCampus.Estudiante && m.Grado == grado);
        if (grupo is not null)
        {
            consulta = consulta.Where(m => m.Grupo == grupo);
        }

        return consulta.OrderBy(m => m.Grupo).ThenBy(m => m.NombreCompleto).ToListAsync(ct);
    }

    public Task<List<MiembroCampus>> ActivosPorRolAsync(RolCampus rol, CancellationToken ct) =>
        db.MiembrosCampus.AsNoTracking().Where(m => m.Activo && m.Rol == rol).ToListAsync(ct);

    public Task<MiembroCampus?> ObtenerPorCodigoCarnetAsync(string codigo, CancellationToken ct) =>
        db.MiembrosCampus.AsNoTracking().FirstOrDefaultAsync(m => m.CodigoCarnet == codigo, ct);

    public Task<List<VinculoResponse>> VinculosAsync(Guid? acudienteId, Guid? estudianteId, CancellationToken ct)
    {
        var consulta = db.VinculosAcudiente.AsNoTracking();
        if (acudienteId is { } a)
        {
            consulta = consulta.Where(v => v.AcudienteId == a);
        }

        if (estudianteId is { } e)
        {
            consulta = consulta.Where(v => v.EstudianteId == e);
        }

        return (from v in consulta
                join acudiente in db.MiembrosCampus on v.AcudienteId equals acudiente.Id
                join estudiante in db.MiembrosCampus on v.EstudianteId equals estudiante.Id
                orderby estudiante.NombreCompleto, acudiente.NombreCompleto
                select new VinculoResponse(
                    v.Id, acudiente.Id, acudiente.NombreCompleto, acudiente.Documento, acudiente.Telefono, acudiente.Correo,
                    estudiante.Id, estudiante.NombreCompleto, estudiante.Documento, estudiante.Grado, estudiante.Grupo, v.Parentesco))
            .ToListAsync(ct);
    }

    public Task<List<VinculoAcudiente>> VinculosDeEstudiantesAsync(IReadOnlyCollection<Guid>? estudianteIds, CancellationToken ct) =>
        estudianteIds is null
            ? db.VinculosAcudiente.AsNoTracking().ToListAsync(ct)
            : db.VinculosAcudiente.AsNoTracking().Where(v => estudianteIds.Contains(v.EstudianteId)).ToListAsync(ct);

    public Task<List<VinculoAcudiente>> VinculosDeAcudienteAsync(Guid acudienteId, CancellationToken ct) =>
        db.VinculosAcudiente.Where(v => v.AcudienteId == acudienteId).ToListAsync(ct);

    public Task<VinculoAcudiente?> ObtenerVinculoAsync(Guid id, CancellationToken ct) =>
        db.VinculosAcudiente.FirstOrDefaultAsync(v => v.Id == id, ct);

    public void AgregarVinculo(VinculoAcudiente vinculo) => db.VinculosAcudiente.Add(vinculo);

    public void EliminarVinculo(VinculoAcudiente vinculo) => db.VinculosAcudiente.Remove(vinculo);

    public void Agregar(MiembroCampus miembro) => db.MiembrosCampus.Add(miembro);

    public void Eliminar(MiembroCampus miembro) => db.MiembrosCampus.Remove(miembro);

    private static IQueryable<MiembroCampus> Filtrar(IQueryable<MiembroCampus> consulta, RolCampus? rol, int? grado, string? grupo)
    {
        if (rol is { } r)
        {
            consulta = consulta.Where(m => m.Rol == r);
        }

        if (grado is { } g)
        {
            consulta = consulta.Where(m => m.Grado == g);
        }

        if (!string.IsNullOrWhiteSpace(grupo))
        {
            consulta = consulta.Where(m => m.Grupo == grupo);
        }

        return consulta;
    }

    /// <summary>Docentes primero; luego estudiantes por grado, grupo y nombre (el orden de las listas de clase).</summary>
    private static IOrderedQueryable<MiembroCampus> Ordenar(IQueryable<MiembroCampus> consulta) =>
        consulta.OrderByDescending(m => m.Rol).ThenBy(m => m.Grado).ThenBy(m => m.Grupo).ThenBy(m => m.NombreCompleto);

    private static string EscaparLike(string valor) =>
        valor.Replace(@"\", @"\\", StringComparison.Ordinal)
             .Replace("%", @"\%", StringComparison.Ordinal)
             .Replace("_", @"\_", StringComparison.Ordinal);
}
