using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Noticias;

/// <summary>
/// Noticia o evento institucional publicado en la sección "Noticias recientes" del sitio.
/// Entidad rica: el estado solo cambia a través de sus métodos, nunca con setters públicos.
/// </summary>
public sealed class Noticia : Entity
{
    public const int TituloMaxLength = 150;
    public const int ResumenMaxLength = 300;
    public const int ContenidoMaxLength = 8000;
    public const int ImagenUrlMaxLength = 500;

    // Constructor sin parámetros requerido por EF Core para materializar la entidad.
    private Noticia()
    {
    }

    public string Titulo { get; private set; } = string.Empty;

    public string Resumen { get; private set; } = string.Empty;

    public string Contenido { get; private set; } = string.Empty;

    public CategoriaNoticia Categoria { get; private set; }

    public string? ImagenUrl { get; private set; }

    public DateOnly FechaPublicacion { get; private set; }

    public bool Publicada { get; private set; }

    public static Noticia Crear(
        string titulo,
        string resumen,
        string contenido,
        CategoriaNoticia categoria,
        DateOnly fechaPublicacion,
        string? imagenUrl = null,
        bool publicada = true)
    {
        var noticia = new Noticia();
        noticia.Actualizar(titulo, resumen, contenido, categoria, fechaPublicacion, imagenUrl, publicada);
        return noticia;
    }

    public void Actualizar(
        string titulo,
        string resumen,
        string contenido,
        CategoriaNoticia categoria,
        DateOnly fechaPublicacion,
        string? imagenUrl,
        bool publicada)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titulo);
        ArgumentException.ThrowIfNullOrWhiteSpace(resumen);

        Titulo = titulo.Trim();
        Resumen = resumen.Trim();
        Contenido = contenido?.Trim() ?? string.Empty;
        Categoria = categoria;
        FechaPublicacion = fechaPublicacion;
        ImagenUrl = string.IsNullOrWhiteSpace(imagenUrl) ? null : imagenUrl.Trim();
        Publicada = publicada;
    }
}
