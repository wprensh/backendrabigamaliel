using FluentValidation;
using RabiGamaliel.Domain.Usuarios;

namespace RabiGamaliel.Application.Usuarios;

internal static class ReglasUsuario
{
    public static IRuleBuilderOptions<T, string> NombreUsuarioValido<T>(this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .Length(Usuario.NombreUsuarioMinLength, Usuario.NombreUsuarioMaxLength)
            .WithMessage($"El nombre de usuario debe tener entre {Usuario.NombreUsuarioMinLength} y {Usuario.NombreUsuarioMaxLength} caracteres.")
            .Matches("^[A-Za-z0-9._-]+$").WithMessage("El nombre de usuario solo admite letras sin tildes, números, puntos, guiones y guion bajo.");

    public static IRuleBuilderOptions<T, string> ClaveSegura<T>(this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .Length(Usuario.ClaveMinLength, Usuario.ClaveMaxLength)
            .WithMessage($"La contraseña debe tener al menos {Usuario.ClaveMinLength} caracteres.")
            .Must(c => c.Any(char.IsLetter) && c.Any(char.IsDigit))
            .WithMessage("La contraseña debe combinar letras y números.");

    public static IRuleBuilderOptions<T, string> NombreCompletoValido<T>(this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty().WithMessage("El nombre completo es obligatorio.")
            .MaximumLength(Usuario.NombreCompletoMaxLength);

    public static IRuleBuilderOptions<T, string?> CorreoValido<T>(this IRuleBuilder<T, string?> regla) =>
        regla
            .MaximumLength(Usuario.CorreoMaxLength)
            .EmailAddress().WithMessage("El correo no es válido.");

    public static IRuleBuilderOptions<T, IReadOnlyList<string>?> PermisosValidos<T>(this IRuleBuilder<T, IReadOnlyList<string>?> regla) =>
        regla.Must(permisos => permisos is null || permisos.All(p => Permiso.TryParse(p, out _)))
            .WithMessage("Hay permisos que no existen.");
}

public sealed class CrearUsuarioValidator : AbstractValidator<CrearUsuarioRequest>
{
    public CrearUsuarioValidator()
    {
        RuleFor(x => x.NombreUsuario).NombreUsuarioValido();
        RuleFor(x => x.NombreCompleto).NombreCompletoValido();
        RuleFor(x => x.Correo).CorreoValido().When(x => !string.IsNullOrWhiteSpace(x.Correo));
        RuleFor(x => x.Clave).ClaveSegura();
        RuleFor(x => x.Permisos).PermisosValidos();
    }
}

public sealed class ActualizarUsuarioValidator : AbstractValidator<ActualizarUsuarioRequest>
{
    public ActualizarUsuarioValidator()
    {
        RuleFor(x => x.NombreCompleto).NombreCompletoValido();
        RuleFor(x => x.Correo).CorreoValido().When(x => !string.IsNullOrWhiteSpace(x.Correo));
        RuleFor(x => x.Permisos).PermisosValidos();
    }
}

public sealed class RestablecerClaveValidator : AbstractValidator<RestablecerClaveRequest>
{
    public RestablecerClaveValidator() => RuleFor(x => x.ClaveNueva).ClaveSegura();
}

public sealed class ConfiguracionInicialValidator : AbstractValidator<ConfiguracionInicialRequest>
{
    public ConfiguracionInicialValidator()
    {
        RuleFor(x => x.NombreUsuario).NombreUsuarioValido();
        RuleFor(x => x.NombreCompleto).NombreCompletoValido();
        RuleFor(x => x.Correo).CorreoValido().When(x => !string.IsNullOrWhiteSpace(x.Correo));
        RuleFor(x => x.Clave).ClaveSegura();
    }
}

public sealed class CambiarMiClaveValidator : AbstractValidator<CambiarMiClaveRequest>
{
    public CambiarMiClaveValidator()
    {
        RuleFor(x => x.ClaveActual).NotEmpty().WithMessage("Escriba su contraseña actual.");
        RuleFor(x => x.ClaveNueva).ClaveSegura()
            .NotEqual(x => x.ClaveActual).WithMessage("La contraseña nueva debe ser distinta de la actual.");
    }
}
