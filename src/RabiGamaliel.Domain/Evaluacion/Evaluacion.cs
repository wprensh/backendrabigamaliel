using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Evaluacion;

public enum EstadoRecuperacion
{
    /// <summary>Plan de mejoramiento asignado; falta la nota de la recuperación.</summary>
    Pendiente = 1,
    Aprobada,
    Reprobada,
}

/// <summary>
/// Plan de mejoramiento y recuperación de una asignatura perdida en un periodo (Decreto 1290 de 2009, art. 4 y 11).
/// Guarda la nota original; si el estudiante recupera, la nota del periodo queda en la mínima aprobatoria.
/// </summary>
public sealed class Recuperacion : Entity
{
    public const int PlanMaxLength = 3000;
    public const int ObservacionMaxLength = 1000;
    public const int AutorMaxLength = 120;

    private Recuperacion()
    {
    }

    public Guid EstudianteId { get; private set; }

    public Guid AsignaturaId { get; private set; }

    public Guid PeriodoId { get; private set; }

    public Guid? DocenteId { get; private set; }

    public decimal NotaOriginal { get; private set; }

    /// <summary>Actividades del plan de mejoramiento.</summary>
    public string Plan { get; private set; } = string.Empty;

    public DateOnly? FechaLimite { get; private set; }

    public decimal? NotaRecuperacion { get; private set; }

    /// <summary>Nota que queda en el periodo después de la recuperación.</summary>
    public decimal? NotaDefinitiva { get; private set; }

    public string? Observacion { get; private set; }

    public EstadoRecuperacion Estado { get; private set; } = EstadoRecuperacion.Pendiente;

    public string RegistradaPor { get; private set; } = string.Empty;

    public static Recuperacion Crear(Guid estudianteId, Guid asignaturaId, Guid periodoId, Guid? docenteId, decimal notaOriginal) => new()
    {
        EstudianteId = estudianteId,
        AsignaturaId = asignaturaId,
        PeriodoId = periodoId,
        DocenteId = docenteId,
        NotaOriginal = notaOriginal,
    };

    public void AsignarPlan(string plan, DateOnly? fechaLimite, string autor)
    {
        Plan = plan.Trim();
        FechaLimite = fechaLimite;
        RegistradaPor = Recortar(autor);
    }

    /// <summary>
    /// Registra la nota de la recuperación. Si aprueba, la definitiva es la mínima aprobatoria (3.0); si no, queda la
    /// mayor entre la original y la de la recuperación.
    /// </summary>
    public decimal Calificar(decimal nota, string? observacion, string autor)
    {
        if (!EscalaValorativa.EsValida(nota))
        {
            throw new ArgumentOutOfRangeException(nameof(nota), $"La nota va de {EscalaValorativa.Minima} a {EscalaValorativa.Maxima}.");
        }

        NotaRecuperacion = Math.Round(nota, 1, MidpointRounding.AwayFromZero);
        NotaDefinitiva = Definitiva(NotaOriginal, NotaRecuperacion.Value);
        Estado = NotaRecuperacion >= EscalaValorativa.Aprobatoria ? EstadoRecuperacion.Aprobada : EstadoRecuperacion.Reprobada;
        Observacion = string.IsNullOrWhiteSpace(observacion) ? null : observacion.Trim();
        RegistradaPor = Recortar(autor);
        return NotaDefinitiva.Value;
    }

    public static decimal Definitiva(decimal original, decimal recuperacion) =>
        recuperacion >= EscalaValorativa.Aprobatoria ? Math.Max(original, EscalaValorativa.Aprobatoria) : Math.Max(original, recuperacion);

    private static string Recortar(string autor) => autor.Length > AutorMaxLength ? autor[..AutorMaxLength] : autor;
}

public enum DecisionComision
{
    /// <summary>Aprueba el periodo o es promovido al grado siguiente.</summary>
    Promovido = 1,

    /// <summary>Debe presentar plan de mejoramiento o actividades de recuperación.</summary>
    PlanMejoramiento,

    /// <summary>Firma compromiso académico con el acudiente.</summary>
    CompromisoAcademico,

    /// <summary>Se remite a orientación escolar.</summary>
    RemitirOrientacion,

    /// <summary>Promoción anticipada (Decreto 1290, art. 7).</summary>
    PromocionAnticipada,

    /// <summary>Reprueba el grado.</summary>
    NoPromovido,
}

/// <summary>Acta de la comisión de evaluación y promoción de un curso, por periodo o al cierre del año.</summary>
public sealed class ActaComision : Entity
{
    public const int IntegrantesMaxLength = 1500;
    public const int ObservacionesMaxLength = 4000;

    private ActaComision()
    {
    }

    public int Anio { get; private set; }

    /// <summary>Periodo evaluado; null en el acta final de promoción del año.</summary>
    public Guid? PeriodoId { get; private set; }

    public int Grado { get; private set; }

    public string? Grupo { get; private set; }

    public int Numero { get; private set; }

    public DateOnly Fecha { get; private set; }

    public string? Integrantes { get; private set; }

    public string? Observaciones { get; private set; }

    public static ActaComision Crear(int anio, Guid? periodoId, int grado, string? grupo, int numero) => new()
    {
        Anio = anio,
        PeriodoId = periodoId,
        Grado = grado,
        Grupo = string.IsNullOrWhiteSpace(grupo) ? null : grupo.Trim().ToUpperInvariant(),
        Numero = numero,
    };

    public void Actualizar(DateOnly fecha, string? integrantes, string? observaciones)
    {
        Fecha = fecha;
        Integrantes = string.IsNullOrWhiteSpace(integrantes) ? null : integrantes.Trim();
        Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones.Trim();
    }
}

/// <summary>Decisión de la comisión sobre un estudiante.</summary>
public sealed class DecisionEstudiante : Entity
{
    public const int NombreMaxLength = 120;
    public const int DetalleMaxLength = 500;
    public const int ObservacionMaxLength = 1000;

    private DecisionEstudiante()
    {
    }

    public Guid ActaId { get; private set; }

    public Guid EstudianteId { get; private set; }

    /// <summary>Nombre al momento del acta (el acta no cambia si luego se corrige el nombre).</summary>
    public string NombreEstudiante { get; private set; } = string.Empty;

    /// <summary>Asignaturas perdidas y fallas, como se presentaron a la comisión.</summary>
    public string? Situacion { get; private set; }

    public DecisionComision Decision { get; private set; }

    public string? Observacion { get; private set; }

    public static DecisionEstudiante Crear(
        Guid actaId, Guid estudianteId, string nombre, string? situacion, DecisionComision decision, string? observacion) => new()
    {
        ActaId = actaId,
        EstudianteId = estudianteId,
        NombreEstudiante = nombre.Length > NombreMaxLength ? nombre[..NombreMaxLength] : nombre,
        Situacion = string.IsNullOrWhiteSpace(situacion) ? null : Recortar(situacion.Trim(), DetalleMaxLength),
        Decision = decision,
        Observacion = string.IsNullOrWhiteSpace(observacion) ? null : Recortar(observacion.Trim(), ObservacionMaxLength),
    };

    private static string Recortar(string texto, int maximo) => texto.Length > maximo ? texto[..maximo] : texto;
}
