using RabiGamaliel.Application.Common;
using RabiGamaliel.Application.Usuarios;
using RabiGamaliel.Domain.Usuarios;
using Microsoft.EntityFrameworkCore;

namespace RabiGamaliel.Infrastructure.Persistence.Repositories;

internal sealed class UsuarioRepository(ApplicationDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken ct) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario, ct);

    public Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken ct) =>
        db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario, ct);

    public Task<bool> ExisteAlgunoAsync(CancellationToken ct) => db.Usuarios.AnyAsync(ct);

    public Task<int> ContarOtrosSuperAdministradoresActivosAsync(Guid excluirId, CancellationToken ct) =>
        db.Usuarios.CountAsync(u => u.Id != excluirId && u.EsSuperAdministrador && u.Activo, ct);

    public async Task<PagedResult<Usuario>> ListarAsync(UsuarioQuery query, CancellationToken ct)
    {
        var consulta = db.Usuarios.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Buscar))
        {
            var patron = $"%{EscaparLike(query.Buscar.Trim())}%";
            consulta = consulta.Where(u =>
                EF.Functions.ILike(u.NombreUsuario, patron, @"\")
                || EF.Functions.ILike(u.NombreCompleto, patron, @"\")
                || (u.Correo != null && EF.Functions.ILike(u.Correo, patron, @"\")));
        }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderBy(u => u.NombreCompleto)
            .Skip((query.Pagina - 1) * query.TamanoPagina)
            .Take(query.TamanoPagina)
            .ToListAsync(ct);

        return new PagedResult<Usuario>(items, query.Pagina, query.TamanoPagina, total);
    }

    /// <summary>Escapa los comodines de LIKE para que el texto del usuario se trate literalmente.</summary>
    private static string EscaparLike(string valor) =>
        valor.Replace(@"\", @"\\", StringComparison.Ordinal)
             .Replace("%", @"\%", StringComparison.Ordinal)
             .Replace("_", @"\_", StringComparison.Ordinal);

    public void Agregar(Usuario usuario) => db.Usuarios.Add(usuario);

    public void Eliminar(Usuario usuario) => db.Usuarios.Remove(usuario);
}
