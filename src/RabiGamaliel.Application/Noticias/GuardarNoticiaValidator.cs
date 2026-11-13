using FluentValidation;
using RabiGamaliel.Domain.Noticias;

namespace RabiGamaliel.Application.Noticias;

public sealed class GuardarNoticiaValidator : AbstractValidator<GuardarNoticiaRequest>
{
    public GuardarNoticiaValidator()
    {
        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("El título es obligatorio.")
            .MaximumLength(Noticia.TituloMaxLength);

        RuleFor(x => x.Resumen)
            .NotEmpty().WithMessage("El resumen es obligatorio.")
            .MaximumLength(Noticia.ResumenMaxLength);

        RuleFor(x => x.Contenido)
            .MaximumLength(Noticia.ContenidoMaxLength);

        RuleFor(x => x.Categoria)
            .IsInEnum().WithMessage("La categoría no es válida.");

        RuleFor(x => x.FechaPublicacion)
            .NotEmpty().WithMessage("La fecha de publicación es obligatoria.");

        RuleFor(x => x.ImagenUrl)
            .MaximumLength(Noticia.ImagenUrlMaxLength)
            .Must(url => EsUrlDeImagenValida(url!))
            .When(x => !string.IsNullOrWhiteSpace(x.ImagenUrl))
            .WithMessage("La imagen debe ser una dirección http(s) o una ruta del sitio que empiece por \"/\" (p. ej. /img/noticias/foto.png).");
    }

    /// <summary>
    /// Acepta dos formatos:
    /// <list type="bullet">
    ///   <item>URL absoluta http(s): <c>https://res.cloudinary.com/.../foto.png</c></item>
    ///   <item>Ruta del propio sitio (imágenes publicadas con el frontend): <c>/img/noticias/foto.png</c></item>
    /// </list>
    /// </summary>
    public static bool EsUrlDeImagenValida(string url)
    {
        url = url.Trim();

        // Ruta relativa al sitio: "/..." pero no "//host" (URL de protocolo relativo),
        // sin espacios ni ".." (evita salir de la carpeta pública).
        if (url.StartsWith('/'))
        {
            return !url.StartsWith("//", StringComparison.Ordinal)
                && !url.Contains("..", StringComparison.Ordinal)
                && !url.Any(char.IsWhiteSpace)
                && Uri.IsWellFormedUriString(url, UriKind.Relative);
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }
}
