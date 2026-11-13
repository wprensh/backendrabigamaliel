using FluentValidation;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Simulacros;

namespace RabiGamaliel.Application.Simulacros;

public sealed class GuardarSimulacroValidator : AbstractValidator<GuardarSimulacroRequest>
{
    public GuardarSimulacroValidator()
    {
        RuleFor(x => x.Titulo).NotEmpty().WithMessage("Escriba el título del simulacro.").MaximumLength(Simulacro.TituloMaxLength);
        RuleFor(x => x.Instrucciones).MaximumLength(Simulacro.InstruccionesMaxLength);
        RuleFor(x => x.Tipo).IsInEnum().WithMessage("Elija el tipo de prueba.");
        RuleFor(x => x.Grados)
            .NotEmpty().WithMessage("Elija al menos un grado.")
            .Must(g => g.All(MiembroCampus.GradoValido)).WithMessage("Los grados van de transición a 11°.");
        RuleFor(x => x.Fin).GreaterThan(x => x.Inicio).WithMessage("El cierre debe ser posterior a la apertura.");
        RuleFor(x => x.DuracionMinutos).InclusiveBetween(5, Simulacro.DuracionMaximaMinutos)
            .WithMessage($"La duración va de 5 a {Simulacro.DuracionMaximaMinutos} minutos.");
    }
}

public sealed class GuardarPreguntasValidator : AbstractValidator<GuardarPreguntasRequest>
{
    public GuardarPreguntasValidator()
    {
        RuleFor(x => x.Preguntas).NotNull()
            .Must(p => p.Count <= GuardarPreguntasRequest.Maximo).WithMessage($"Un simulacro admite hasta {GuardarPreguntasRequest.Maximo} preguntas.");
        RuleForEach(x => x.Preguntas).ChildRules(p =>
        {
            p.RuleFor(x => x.Area).IsInEnum().WithMessage("Elija el área de la pregunta.");
            p.RuleFor(x => x.Enunciado).NotEmpty().WithMessage("Cada pregunta necesita enunciado.").MaximumLength(PreguntaSimulacro.EnunciadoMaxLength);
            p.RuleFor(x => x.Contexto).MaximumLength(PreguntaSimulacro.ContextoMaxLength);
            p.RuleFor(x => x.Competencia).MaximumLength(PreguntaSimulacro.CompetenciaMaxLength);
            p.RuleFor(x => x.Explicacion).MaximumLength(PreguntaSimulacro.ExplicacionMaxLength);
            p.RuleFor(x => x.ImagenUrl).MaximumLength(PreguntaSimulacro.ImagenUrlMaxLength)
                .Must(u => u is null || u.Length == 0 || Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
                .WithMessage("La imagen debe ser un enlace https.");
            p.RuleFor(x => x.Opciones)
                .NotNull()
                .Must(o => o.Count is >= PreguntaSimulacro.OpcionesMinimas and <= PreguntaSimulacro.OpcionesMaximas)
                .WithMessage($"Cada pregunta lleva de {PreguntaSimulacro.OpcionesMinimas} a {PreguntaSimulacro.OpcionesMaximas} opciones.")
                .Must(o => o.All(t => !string.IsNullOrWhiteSpace(t) && t.Length <= PreguntaSimulacro.OpcionMaxLength))
                .WithMessage("Las opciones no pueden quedar vacías.");
            p.RuleFor(x => x.Correcta).Must((x, c) => x.Opciones is not null && c >= 0 && c < x.Opciones.Count)
                .WithMessage("Marque la respuesta correcta.");
        });
    }
}

public sealed class GuardarPreguntaBancoValidator : AbstractValidator<GuardarPreguntaBancoRequest>
{
    public GuardarPreguntaBancoValidator()
    {
        RuleFor(x => x.Area).IsInEnum().WithMessage("Elija el área de la pregunta.");
        RuleFor(x => x.Dificultad).IsInEnum().WithMessage("Elija la dificultad.");
        RuleFor(x => x.Enunciado).NotEmpty().WithMessage("La pregunta necesita enunciado.").MaximumLength(PreguntaSimulacro.EnunciadoMaxLength);
        RuleFor(x => x.Contexto).MaximumLength(PreguntaSimulacro.ContextoMaxLength);
        RuleFor(x => x.Competencia).MaximumLength(PreguntaSimulacro.CompetenciaMaxLength);
        RuleFor(x => x.Explicacion).MaximumLength(PreguntaSimulacro.ExplicacionMaxLength);
        RuleFor(x => x.ImagenUrl).MaximumLength(PreguntaSimulacro.ImagenUrlMaxLength)
            .Must(u => u is null || u.Length == 0 || Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            .WithMessage("La imagen debe ser un enlace https.");
        RuleFor(x => x.Grados).Must(g => g is null || g.All(MiembroCampus.GradoValido)).WithMessage("Los grados van de transición a 11°.");
        RuleFor(x => x.Opciones)
            .NotNull()
            .Must(o => o.Count is >= PreguntaSimulacro.OpcionesMinimas and <= PreguntaSimulacro.OpcionesMaximas)
            .WithMessage($"La pregunta lleva de {PreguntaSimulacro.OpcionesMinimas} a {PreguntaSimulacro.OpcionesMaximas} opciones.")
            .Must(o => o.All(t => !string.IsNullOrWhiteSpace(t) && t.Length <= PreguntaSimulacro.OpcionMaxLength))
            .WithMessage("Las opciones no pueden quedar vacías.");
        RuleFor(x => x.Correcta).Must((x, c) => x.Opciones is not null && c >= 0 && c < x.Opciones.Count)
            .WithMessage("Marque la respuesta correcta.");
    }
}

public sealed class GuardarLoteBancoValidator : AbstractValidator<GuardarLoteBancoRequest>
{
    public GuardarLoteBancoValidator()
    {
        RuleFor(x => x.Preguntas).NotEmpty().WithMessage("Envíe al menos una pregunta.")
            .Must(p => p.Count <= GuardarLoteBancoRequest.Maximo).WithMessage($"Se cargan hasta {GuardarLoteBancoRequest.Maximo} preguntas a la vez.");
        RuleForEach(x => x.Preguntas).SetValidator(new GuardarPreguntaBancoValidator());
    }
}

public sealed class AgregarDesdeBancoValidator : AbstractValidator<AgregarDesdeBancoRequest>
{
    public AgregarDesdeBancoValidator()
    {
        RuleFor(x => x).Must(x => x.PreguntaIds is { Count: > 0 } || x.Cantidad is > 0)
            .WithMessage("Elija preguntas del banco o indique cuántas tomar al azar.");
        RuleFor(x => x.Cantidad).InclusiveBetween(1, GuardarPreguntasRequest.Maximo).When(x => x.Cantidad is not null)
            .WithMessage($"Se toman de 1 a {GuardarPreguntasRequest.Maximo} preguntas.");
        RuleFor(x => x.PreguntaIds).Must(p => p is null || p.Count <= GuardarPreguntasRequest.Maximo)
            .WithMessage($"Se agregan hasta {GuardarPreguntasRequest.Maximo} preguntas a la vez.");
        RuleForEach(x => x.Areas).IsInEnum();
        RuleFor(x => x.Dificultad).IsInEnum().When(x => x.Dificultad is not null);
    }
}
