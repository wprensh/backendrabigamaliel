using RabiGamaliel.Domain.Noticias;

namespace RabiGamaliel.Application.Noticias;

/// <summary>Datos que devuelve la API para una noticia.</summary>
public sealed record NoticiaResponse(
    Guid Id,
    string Titulo,
    string Resumen,
    string Contenido,
    CategoriaNoticia Categoria,
    string? ImagenUrl,
    DateOnly FechaPublicacion,
    bool Publicada,
    DateTimeOffset CreadoEn,
    DateTimeOffset? ModificadoEn)
{
    public static NoticiaResponse Desde(Noticia n) => new(
        n.Id, n.Titulo, n.Resumen, n.Contenido, n.Categoria, n.ImagenUrl,
        n.FechaPublicacion, n.Publicada, n.CreadoEn, n.ModificadoEn);
}

/// <summary>Cuerpo de las solicitudes de creación y edición (POST / PUT).</summary>
public sealed record GuardarNoticiaRequest(
    string Titulo,
    string Resumen,
    string? Contenido,
    CategoriaNoticia Categoria,
    DateOnly FechaPublicacion,
    string? ImagenUrl,
    bool Publicada = true);

/// <summary>Orden del listado de noticias por fecha de publicación.</summary>
public enum OrdenNoticias
{
    Recientes,
    Antiguas,
}

/// <summary>
/// Filtros y paginación del listado de noticias.
/// <see cref="Desde"/> y <see cref="Hasta"/> son inclusivos y se comparan con la fecha de publicación.
/// </summary>
public sealed record NoticiaQuery(
    string? Buscar = null,
    CategoriaNoticia? Categoria = null,
    bool SoloPublicadas = false,
    int Pagina = 1,
    int TamanoPagina = 10,
    DateOnly? Desde = null,
    DateOnly? Hasta = null,
    OrdenNoticias Orden = OrdenNoticias.Recientes)
{
    public const int TamanoPaginaMaximo = 100;
}

/// <summary>Cantidad de noticias publicadas en un mes: alimenta el archivo por meses del sitio.</summary>
public sealed record ArchivoNoticiasItem(int Anio, int Mes, int Total);
