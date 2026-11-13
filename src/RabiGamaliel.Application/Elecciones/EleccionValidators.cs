using FluentValidation;
using RabiGamaliel.Domain.Elecciones;

namespace RabiGamaliel.Application.Elecciones;

internal static class ReglasEleccion
{
    /// <summary>
    /// URL http(s) absoluta o ruta del propio sitio ("/img/candidatos/ana.jpg"), sin "//" inicial ni "..",
    /// la misma regla que las imágenes de las noticias.
    /// </summary>
    public static bool UrlValida(string? url) =>
        string.IsNullOrWhiteSpace(url)
        || (Uri.TryCreate(url.Trim(), UriKind.Absolute, out var absoluta)
            && (absoluta.Scheme == Uri.UriSchemeHttps || absoluta.Scheme == Uri.UriSchemeHttp))
        || (url.Trim().StartsWith('/') && !url.Trim().StartsWith("//", StringComparison.Ordinal)
            && !url.Contains("..", StringComparison.Ordinal) && !url.Any(char.IsWhiteSpace));
}

public sealed class GuardarEleccionValidator : AbstractValidator<GuardarEleccionRequest>
{
    public GuardarEleccionValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre de la elección es obligatorio.")
            .MaximumLength(Eleccion.NombreMaxLength);
        RuleFor(x => x.Descripcion).MaximumLength(Eleccion.DescripcionMaxLength);
        RuleFor(x => x.Fin).GreaterThan(x => x.Inicio).WithMessage("El cierre debe ser posterior a la apertura.");
        RuleFor(x => x.Cargos)
            .NotEmpty().WithMessage("Elija al menos un cargo.")
            .Must(c => c is null || c.All(Enum.IsDefined)).WithMessage("Hay cargos que no existen.");
    }
}

internal static class ReglasPerfil
{
    public static void Aplicar<T>(
        AbstractValidator<T> validador,
        System.Linq.Expressions.Expression<Func<T, string?>> lema,
        System.Linq.Expressions.Expression<Func<T, string?>> propuestas,
        System.Linq.Expressions.Expression<Func<T, string?>> fotoUrl)
    {
        validador.RuleFor(lema).MaximumLength(Candidato.LemaMaxLength);
        validador.RuleFor(propuestas).MaximumLength(Candidato.PropuestasMaxLength);
        validador.RuleFor(fotoUrl)
            .MaximumLength(Candidato.FotoUrlMaxLength)
            .Must(ReglasEleccion.UrlValida).WithMessage("Use una dirección http(s):// o una ruta del sitio que empiece por /.");
    }
}

public sealed class CrearCandidatoValidator : AbstractValidator<CrearCandidatoRequest>
{
    public CrearCandidatoValidator()
    {
        RuleFor(x => x.Cargo).IsInEnum().WithMessage("Elija el cargo.");
        RuleFor(x => x.EstudianteId).NotEmpty().WithMessage("Elija el estudiante.");
        RuleFor(x => x.Numero).InclusiveBetween(1, Candidato.NumeroMaximo).When(x => x.Numero is not null)
            .WithMessage($"El número del tarjetón va de 1 a {Candidato.NumeroMaximo}.");
        ReglasPerfil.Aplicar(this, x => x.Lema, x => x.Propuestas, x => x.FotoUrl);
    }
}

public sealed class ActualizarCandidatoValidator : AbstractValidator<ActualizarCandidatoRequest>
{
    public ActualizarCandidatoValidator()
    {
        RuleFor(x => x.Numero).InclusiveBetween(1, Candidato.NumeroMaximo)
            .WithMessage($"El número del tarjetón va de 1 a {Candidato.NumeroMaximo}.");
        ReglasPerfil.Aplicar(this, x => x.Lema, x => x.Propuestas, x => x.FotoUrl);
    }
}
