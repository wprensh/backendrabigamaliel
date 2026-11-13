using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Planeacion;

/// <summary>
/// Malla curricular de una asignatura en un grado y periodo: estándares básicos de competencias, Derechos Básicos de
/// Aprendizaje (DBA), competencias, contenidos e indicadores de desempeño. La consultan los docentes al planear.
/// </summary>
public sealed class PlanEstudios : Entity
{
    public const int TextoMaxLength = 6000;
    public const int AutorMaxLength = 120;

    private PlanEstudios()
    {
    }

    public Guid AsignaturaId { get; private set; }

    public int Grado { get; private set; }

    /// <summary>Número del periodo (1 a 4).</summary>
    public int Periodo { get; private set; }

    public string? Estandares { get; private set; }

    public string? Dba { get; private set; }

    public string? Competencias { get; private set; }

    public string? Contenidos { get; private set; }

    public string? Indicadores { get; private set; }

    public string ActualizadoPor { get; private set; } = string.Empty;

    public static PlanEstudios Crear(Guid asignaturaId, int grado, int periodo) => new() { AsignaturaId = asignaturaId, Grado = grado, Periodo = periodo };

    public void Actualizar(string? estandares, string? dba, string? competencias, string? contenidos, string? indicadores, string autor)
    {
        Estandares = Texto(estandares);
        Dba = Texto(dba);
        Competencias = Texto(competencias);
        Contenidos = Texto(contenidos);
        Indicadores = Texto(indicadores);
        ActualizadoPor = autor.Length > AutorMaxLength ? autor[..AutorMaxLength] : autor;
    }

    internal static string? Texto(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}

public enum EstadoPlaneacion
{
    Borrador = 1,
    Enviada,
    Aprobada,
    Devuelta,
}

/// <summary>Planeación semanal de clase de un docente para uno de sus cursos; la revisa coordinación académica.</summary>
public sealed class PlaneacionClase : Entity
{
    public const int TemaMaxLength = 200;
    public const int TextoMaxLength = 4000;
    public const int ObservacionMaxLength = 1000;
    public const int RevisorMaxLength = 120;

    private PlaneacionClase()
    {
    }

    public Guid AsignacionId { get; private set; }

    public Guid DocenteId { get; private set; }

    public Guid? PeriodoId { get; private set; }

    /// <summary>Lunes de la semana planeada.</summary>
    public DateOnly Semana { get; private set; }

    public string Tema { get; private set; } = string.Empty;

    /// <summary>Estándar, DBA o aprendizaje esperado que se trabaja.</summary>
    public string? Aprendizajes { get; private set; }

    /// <summary>Momentos de la clase: inicio, desarrollo y cierre.</summary>
    public string? Actividades { get; private set; }

    public string? Recursos { get; private set; }

    public string? Evaluacion { get; private set; }

    public EstadoPlaneacion Estado { get; private set; } = EstadoPlaneacion.Borrador;

    public DateTimeOffset? EnviadaEn { get; private set; }

    public string? Observacion { get; private set; }

    public string? RevisadaPor { get; private set; }

    public DateTimeOffset? RevisadaEn { get; private set; }

    /// <summary>El docente solo edita mientras está en borrador o se la devolvieron.</summary>
    public bool Editable => Estado is EstadoPlaneacion.Borrador or EstadoPlaneacion.Devuelta;

    public static PlaneacionClase Crear(Guid asignacionId, Guid docenteId) => new() { AsignacionId = asignacionId, DocenteId = docenteId };

    public static DateOnly LunesDe(DateOnly fecha) => fecha.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));

    public void Actualizar(
        Guid? periodoId, DateOnly semana, string tema, string? aprendizajes, string? actividades, string? recursos, string? evaluacion)
    {
        if (!Editable)
        {
            throw new InvalidOperationException("La planeación ya fue enviada.");
        }

        PeriodoId = periodoId;
        Semana = LunesDe(semana);
        Tema = tema.Trim();
        Aprendizajes = PlanEstudios.Texto(aprendizajes);
        Actividades = PlanEstudios.Texto(actividades);
        Recursos = PlanEstudios.Texto(recursos);
        Evaluacion = PlanEstudios.Texto(evaluacion);
    }

    public void Enviar(DateTimeOffset ahora)
    {
        if (!Editable)
        {
            throw new InvalidOperationException("La planeación ya fue enviada.");
        }

        Estado = EstadoPlaneacion.Enviada;
        EnviadaEn = ahora;
    }

    /// <summary>Coordinación aprueba o devuelve con observaciones.</summary>
    public void Revisar(bool aprobar, string? observacion, string revisor, DateTimeOffset ahora)
    {
        if (Estado != EstadoPlaneacion.Enviada)
        {
            throw new InvalidOperationException("Solo se revisan planeaciones enviadas.");
        }

        Estado = aprobar ? EstadoPlaneacion.Aprobada : EstadoPlaneacion.Devuelta;
        Observacion = PlanEstudios.Texto(observacion);
        RevisadaPor = revisor.Length > RevisorMaxLength ? revisor[..RevisorMaxLength] : revisor;
        RevisadaEn = ahora;
    }
}
