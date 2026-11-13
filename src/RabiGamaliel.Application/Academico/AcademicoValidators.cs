using FluentValidation;
using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Academico;

public sealed class GuardarAsignaturaValidator : AbstractValidator<GuardarAsignaturaRequest>
{
    public GuardarAsignaturaValidator()
    {
        RuleFor(x => x.Nombre).NotEmpty().WithMessage("Escriba el nombre de la asignatura.").MaximumLength(Asignatura.NombreMaxLength);
        RuleFor(x => x.Area).MaximumLength(Asignatura.AreaMaxLength);
        RuleFor(x => x.Orden).InclusiveBetween(0, 999);
    }
}

public sealed class GuardarPeriodoValidator : AbstractValidator<GuardarPeriodoRequest>
{
    public GuardarPeriodoValidator()
    {
        RuleFor(x => x.Anio).InclusiveBetween(2020, 2100).WithMessage("Año lectivo no válido.");
        RuleFor(x => x.Numero).InclusiveBetween(1, PeriodoAcademico.NumeroMaximo).WithMessage($"El periodo va de 1 a {PeriodoAcademico.NumeroMaximo}.");
        RuleFor(x => x.Nombre).MaximumLength(PeriodoAcademico.NombreMaxLength);
        RuleFor(x => x.Fin).GreaterThanOrEqualTo(x => x.Inicio).WithMessage("El periodo debe terminar después de empezar.");
        RuleFor(x => x.Porcentaje).InclusiveBetween(0, 100).WithMessage("El porcentaje va de 0 a 100.");
    }
}

public sealed class CrearAsignacionValidator : AbstractValidator<CrearAsignacionRequest>
{
    public CrearAsignacionValidator()
    {
        RuleFor(x => x.Anio).InclusiveBetween(2020, 2100).WithMessage("Año lectivo no válido.");
        RuleFor(x => x.DocenteId).NotEmpty().WithMessage("Elija el docente.");
        RuleFor(x => x.AsignaturaId).NotEmpty().WithMessage("Elija la asignatura.");
        RuleFor(x => x.Grado).InclusiveBetween(GradosEscolares.Transicion, GradosEscolares.Ultimo).WithMessage("El grado va de transición a 11°.");
        RuleFor(x => x.Grupo).MaximumLength(MiembroCampus.GrupoMaxLength).Matches("^[A-Za-z0-9]*$").WithMessage("El grupo solo admite letras y números.");
        RuleFor(x => x.IntensidadHoraria).InclusiveBetween(0, 40);
    }
}

public sealed class GuardarNotasValidator : AbstractValidator<GuardarNotasRequest>
{
    public GuardarNotasValidator()
    {
        RuleFor(x => x.Notas).NotNull().Must(n => n.Count <= 200).WithMessage("Demasiadas filas.");
        RuleForEach(x => x.Notas).ChildRules(fila =>
        {
            fila.RuleFor(f => f.Nota)
                .InclusiveBetween(EscalaValorativa.Minima, EscalaValorativa.Maxima)
                .WithMessage($"Las notas van de {EscalaValorativa.Minima:0.0} a {EscalaValorativa.Maxima:0.0}.");
            fila.RuleFor(f => f.Fallas).InclusiveBetween(0, 500).WithMessage("Fallas no válidas.");
            fila.RuleFor(f => f.Observacion).MaximumLength(Calificacion.ObservacionMaxLength)
                .WithMessage($"La observación admite hasta {Calificacion.ObservacionMaxLength} caracteres.");
        });
    }
}

public sealed class GuardarBloqueValidator : AbstractValidator<GuardarBloqueRequest>
{
    public GuardarBloqueValidator()
    {
        RuleFor(x => x.AsignacionId).NotEmpty().WithMessage("Elija la clase de la carga docente.");
        RuleFor(x => x).Must(x => ReglasHorario.FranjaValida(x.Dia, x.Inicio, x.Fin)).OverridePropertyName("Fin")
            .WithMessage("La clase debe ser de lunes a sábado, entre 5:00 a. m. y 10:00 p. m., y durar al menos 15 minutos.");
    }
}

public sealed class MoverBloqueValidator : AbstractValidator<MoverBloqueRequest>
{
    public MoverBloqueValidator()
    {
        RuleFor(x => x).Must(x => ReglasHorario.FranjaValida(x.Dia, x.Inicio, x.Fin)).OverridePropertyName("Fin")
            .WithMessage("La clase debe ser de lunes a sábado, entre 5:00 a. m. y 10:00 p. m., y durar al menos 15 minutos.");
    }
}
