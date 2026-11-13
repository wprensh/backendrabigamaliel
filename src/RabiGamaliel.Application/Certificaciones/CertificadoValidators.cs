using FluentValidation;
using RabiGamaliel.Domain.Certificaciones;

namespace RabiGamaliel.Application.Certificaciones;

public sealed class ExpedirCertificadoValidator : AbstractValidator<ExpedirCertificadoRequest>
{
    public ExpedirCertificadoValidator()
    {
        RuleFor(x => x.Tipo).IsInEnum().WithMessage("El tipo de certificado no es válido.");

        RuleFor(x => x.Titulo)
            .NotEmpty().WithMessage("El título del documento es obligatorio.")
            .MaximumLength(Certificado.TituloMaxLength);

        RuleFor(x => x.NombreBeneficiario)
            .NotEmpty().WithMessage("El nombre del beneficiario es obligatorio.")
            .MaximumLength(Certificado.NombreMaxLength);

        RuleFor(x => x.TipoDocumento).IsInEnum().WithMessage("El tipo de documento no es válido.");

        RuleFor(x => x.NumeroDocumento)
            .NotEmpty().WithMessage("El número de documento es obligatorio.")
            .MaximumLength(Certificado.NumeroDocumentoMaxLength)
            .Matches("^[A-Za-z0-9.-]+$").WithMessage("El número de documento solo admite letras, números, puntos y guiones.");

        RuleFor(x => x.Grado).MaximumLength(Certificado.GradoMaxLength);

        RuleFor(x => x.AnioLectivo)
            .InclusiveBetween(1950, 2100).WithMessage("El año lectivo no es válido.")
            .When(x => x.AnioLectivo is not null);

        RuleFor(x => x.Contenido)
            .NotEmpty().WithMessage("El texto del certificado es obligatorio.")
            .MaximumLength(Certificado.ContenidoMaxLength);

        RuleFor(x => x.FechaExpedicion)
            .NotEmpty().WithMessage("La fecha de expedición es obligatoria.");

        RuleFor(x => x.FirmanteNombre)
            .NotEmpty().WithMessage("El nombre de quien firma es obligatorio.")
            .MaximumLength(Certificado.FirmanteNombreMaxLength);

        RuleFor(x => x.FirmanteCargo)
            .NotEmpty().WithMessage("El cargo de quien firma es obligatorio.")
            .MaximumLength(Certificado.FirmanteCargoMaxLength);
    }
}

public sealed class AnularCertificadoValidator : AbstractValidator<AnularCertificadoRequest>
{
    public AnularCertificadoValidator()
    {
        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("Indique el motivo de la anulación.")
            .MaximumLength(Certificado.MotivoAnulacionMaxLength);
    }
}
