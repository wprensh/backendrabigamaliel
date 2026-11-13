using FluentValidation;
using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Campus;

internal static class ReglasMiembro
{
    public static IRuleBuilderOptions<T, string> DocumentoValido<T>(this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty().WithMessage("El número de documento es obligatorio.")
            .Matches(@"^[A-Za-z0-9.\- ]+$").WithMessage("El documento solo admite letras, números, puntos y guiones.")
            .Must(d => MiembroCampus.NormalizarDocumento(d).Length is >= MiembroCampus.DocumentoMinLength and <= MiembroCampus.DocumentoMaxLength)
            .WithMessage($"El documento debe tener entre {MiembroCampus.DocumentoMinLength} y {MiembroCampus.DocumentoMaxLength} letras o números.");

    public static IRuleBuilderOptions<T, string> NombreValido<T>(this IRuleBuilder<T, string> regla) =>
        regla
            .NotEmpty().WithMessage("El nombre completo es obligatorio.")
            .MaximumLength(MiembroCampus.NombreMaxLength);

    public static IRuleBuilderOptions<T, string?> GrupoValido<T>(this IRuleBuilder<T, string?> regla) =>
        regla
            .MaximumLength(MiembroCampus.GrupoMaxLength).WithMessage($"El grupo admite hasta {MiembroCampus.GrupoMaxLength} caracteres.")
            .Matches("^[A-Za-z0-9]*$").WithMessage("El grupo solo admite letras y números (p. ej. A o 01).");

    public static IRuleBuilderOptions<T, string?> CorreoValido<T>(this IRuleBuilder<T, string?> regla) =>
        regla
            .MaximumLength(MiembroCampus.CorreoMaxLength)
            .EmailAddress().WithMessage("Escriba un correo válido.");

    public static IRuleBuilderOptions<T, string?> TelefonoValido<T>(this IRuleBuilder<T, string?> regla) =>
        regla
            .MaximumLength(MiembroCampus.TelefonoMaxLength)
            .Matches(@"^[0-9+ ()\-]*$").WithMessage("El teléfono solo admite números.");

    /// <summary>Mensaje de error de una fila de importación, o null si es válida.</summary>
    public static string? ErrorDeFila(RolCampus rol, FilaImportacion fila)
    {
        var documento = MiembroCampus.NormalizarDocumento(fila.Documento ?? string.Empty);
        if (documento.Length is < MiembroCampus.DocumentoMinLength or > MiembroCampus.DocumentoMaxLength)
        {
            return "Documento vacío o con un largo no válido.";
        }

        if (string.IsNullOrWhiteSpace(fila.NombreCompleto) || fila.NombreCompleto.Trim().Length > MiembroCampus.NombreMaxLength)
        {
            return "Falta el nombre o es demasiado largo.";
        }

        if (rol == RolCampus.Estudiante && (fila.Grado is not { } grado || !MiembroCampus.GradoValido(grado)))
        {
            return "El grado debe ser un número de 0 (transición) a 11.";
        }

        var grupo = fila.Grupo?.Trim() ?? string.Empty;
        return grupo.Length > MiembroCampus.GrupoMaxLength || !grupo.All(char.IsLetterOrDigit)
            ? "El grupo solo admite letras y números (p. ej. A o 01)."
            : null;
    }
}

public sealed class CrearMiembroValidator : AbstractValidator<CrearMiembroRequest>
{
    public CrearMiembroValidator()
    {
        RuleFor(x => x.Rol).IsInEnum().WithMessage("Elija si es estudiante, docente o acudiente.");
        RuleFor(x => x.Documento).DocumentoValido();
        RuleFor(x => x.NombreCompleto).NombreValido();
        RuleFor(x => x.Grado)
            .NotNull().WithMessage("Elija el grado del estudiante.")
            .InclusiveBetween(GradosEscolares.Transicion, GradosEscolares.Ultimo).WithMessage("El grado va de transición a 11°.")
            .When(x => x.Rol == RolCampus.Estudiante);
        RuleFor(x => x.Grupo).GrupoValido();
        RuleFor(x => x.Correo).CorreoValido();
        RuleFor(x => x.Telefono).TelefonoValido();
    }
}

public sealed class ActualizarMiembroValidator : AbstractValidator<ActualizarMiembroRequest>
{
    public ActualizarMiembroValidator()
    {
        RuleFor(x => x.Rol).IsInEnum().WithMessage("Elija si es estudiante, docente o acudiente.");
        RuleFor(x => x.NombreCompleto).NombreValido();
        RuleFor(x => x.Grado)
            .NotNull().WithMessage("Elija el grado del estudiante.")
            .InclusiveBetween(GradosEscolares.Transicion, GradosEscolares.Ultimo).WithMessage("El grado va de transición a 11°.")
            .When(x => x.Rol == RolCampus.Estudiante);
        RuleFor(x => x.Grupo).GrupoValido();
        RuleFor(x => x.Correo).CorreoValido();
        RuleFor(x => x.Telefono).TelefonoValido();
    }
}

public sealed class ImportarMiembrosValidator : AbstractValidator<ImportarMiembrosRequest>
{
    public ImportarMiembrosValidator()
    {
        RuleFor(x => x.Rol).IsInEnum().WithMessage("Elija si la lista es de estudiantes, docentes o acudientes.");
        RuleFor(x => x.Filas)
            .NotEmpty().WithMessage("La lista está vacía.")
            .Must(f => f.Count <= ImportarMiembrosRequest.FilasMaximas)
            .WithMessage($"Importe como máximo {ImportarMiembrosRequest.FilasMaximas} filas a la vez.");
    }
}

public sealed class ImportarAcudientesValidator : AbstractValidator<ImportarAcudientesRequest>
{
    public ImportarAcudientesValidator()
    {
        RuleFor(x => x.Filas)
            .NotEmpty().WithMessage("La lista está vacía.")
            .Must(f => f.Count <= ImportarAcudientesRequest.FilasMaximas)
            .WithMessage($"Importe como máximo {ImportarAcudientesRequest.FilasMaximas} filas a la vez.");
    }
}
