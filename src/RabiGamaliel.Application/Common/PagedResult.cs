namespace RabiGamaliel.Application.Common;

/// <summary>Página de resultados para listados paginados.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Pagina, int TamanoPagina, int Total)
{
    public int TotalPaginas => TamanoPagina == 0 ? 0 : (int)Math.Ceiling(Total / (double)TamanoPagina);

    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new([.. Items.Select(selector)], Pagina, TamanoPagina, Total);
}
