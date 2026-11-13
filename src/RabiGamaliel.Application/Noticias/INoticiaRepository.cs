using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Noticias;

namespace RabiGamaliel.Application.Noticias;

/// <summary>Puerto de persistencia de noticias (lo implementa Infrastructure).</summary>
public interface INoticiaRepository
{
    Task<Noticia?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<PagedResult<Noticia>> ListarAsync(NoticiaQuery query, CancellationToken ct);

    /// <summary>Noticias agrupadas por año y mes, del mes más reciente al más antiguo.</summary>
    Task<IReadOnlyList<ArchivoNoticiasItem>> ArchivoAsync(bool soloPublicadas, CancellationToken ct);

    Task<bool> ExisteTituloAsync(string titulo, Guid? excluirId, CancellationToken ct);

    void Agregar(Noticia noticia);

    void Eliminar(Noticia noticia);
}
