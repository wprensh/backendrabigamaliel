using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Usuarios;

namespace RabiGamaliel.Application.Usuarios;

/// <summary>Puerto de persistencia de usuarios (lo implementa Infrastructure).</summary>
public interface IUsuarioRepository
{
    Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken ct);

    Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken ct);

    Task<bool> ExisteAlgunoAsync(CancellationToken ct);

    /// <summary>Superadministradores activos distintos de <paramref name="excluirId"/>.</summary>
    Task<int> ContarOtrosSuperAdministradoresActivosAsync(Guid excluirId, CancellationToken ct);

    Task<PagedResult<Usuario>> ListarAsync(UsuarioQuery query, CancellationToken ct);

    void Agregar(Usuario usuario);

    void Eliminar(Usuario usuario);
}
