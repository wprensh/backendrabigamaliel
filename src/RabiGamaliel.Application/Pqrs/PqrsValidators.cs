using FluentValidation;
using RabiGamaliel.Domain.Pqrs;

namespace RabiGamaliel.Application.Pqrs;

public sealed class RadicarPqrsValidator : AbstractValidator<RadicarPqrsRequest>
{
    public RadicarPqrsValidator()
    {
        RuleFor(x => x.NombreCompleto)
            .NotEmpty().WithMessage("El nombre completo es obligatorio.")
            .MaximumLength(SolicitudPqrs.NombreMaxLength);

        RuleFor(x => x.Correo)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El correo electrónico no es válido.")
            .MaximumLength(SolicitudPqrs.CorreoMaxLength);

        RuleFor(x => x.Mensaje)
            .NotEmpty().WithMessage("Escriba su consulta o mensaje.")
            .MinimumLength(10).WithMessage("El mensaje debe tener al menos 10 caracteres.")
            .MaximumLength(SolicitudPqrs.MensajeMaxLength);

        RuleFor(x => x.Tipo).IsInEnum();
    }
}

public sealed class ResponderPqrsValidator : AbstractValidator<ResponderPqrsRequest>
{
    public ResponderPqrsValidator()
    {
        RuleFor(x => x.Respuesta)
            .NotEmpty().WithMessage("La respuesta es obligatoria.")
            .MaximumLength(SolicitudPqrs.RespuestaMaxLength);
    }
}
