using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Convivencia;

/// <summary>Clases de anotación del observador, con las situaciones de la Ley 1620 de 2013 (Decreto 1965).</summary>
public enum TipoAnotacion
{
    /// <summary>Reconocimiento o felicitación.</summary>
    Positiva = 1,

    /// <summary>Observación académica o de comportamiento que no es una situación de convivencia.</summary>
    Observacion,

    /// <summary>Conflicto manejado de forma inadecuada, sin daño al cuerpo o a la salud.</summary>
    SituacionTipoI,

    /// <summary>Agresión, acoso o ciberacoso repetido o que causa daño sin incapacidad.</summary>
    SituacionTipoII,

    /// <summary>Presunto delito: se remite a las autoridades (Policía de Infancia, ICBF, Comisaría).</summary>
    SituacionTipoIII,
}

public enum EstadoCaso
{
    Abierto = 1,
    EnSeguimiento,
    Cerrado,
}

/// <summary>Anotación del observador del estudiante. Las situaciones abren un caso con seguimiento hasta cerrarlo.</summary>
public sealed class Anotacion : Entity
{
    public const int DescripcionMaxLength = 3000;
    public const int CompromisosMaxLength = 1500;
    public const int AutorMaxLength = 120;

    private readonly List<SeguimientoAnotacion> _seguimientos = [];

    private Anotacion()
    {
    }

    public Guid EstudianteId { get; private set; }

    /// <summary>Curso del estudiante cuando ocurrió (el estudiante puede cambiar de curso después).</summary>
    public int? Grado { get; private set; }

    public string? Grupo { get; private set; }

    public TipoAnotacion Tipo { get; private set; }

    public DateOnly Fecha { get; private set; }

    public string Descripcion { get; private set; } = string.Empty;

    /// <summary>Compromisos del estudiante y la familia, o acciones pedagógicas acordadas.</summary>
    public string? Compromisos { get; private set; }

    public string AutorNombre { get; private set; } = string.Empty;

    /// <summary>Docente que la escribió desde el campus; null si la escribió coordinación desde el panel.</summary>
    public Guid? AutorMiembroId { get; private set; }

    public EstadoCaso Estado { get; private set; }

    /// <summary>Cuándo el acudiente confirmó que la leyó.</summary>
    public DateTimeOffset? EnteradoEn { get; private set; }

    public string? EnteradoPor { get; private set; }

    public IReadOnlyList<SeguimientoAnotacion> Seguimientos => _seguimientos;

    public bool EsSituacion => Tipo is TipoAnotacion.SituacionTipoI or TipoAnotacion.SituacionTipoII or TipoAnotacion.SituacionTipoIII;

    public static Anotacion Crear(
        Guid estudianteId, int? grado, string? grupo, TipoAnotacion tipo, DateOnly fecha, string descripcion, string? compromisos,
        string autorNombre, Guid? autorMiembroId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(descripcion);
        var anotacion = new Anotacion
        {
            EstudianteId = estudianteId,
            Grado = grado,
            Grupo = grupo,
            Tipo = tipo,
            Fecha = fecha,
            Descripcion = descripcion.Trim(),
            Compromisos = string.IsNullOrWhiteSpace(compromisos) ? null : compromisos.Trim(),
            AutorNombre = Recortar(autorNombre, AutorMaxLength),
            AutorMiembroId = autorMiembroId,
        };
        anotacion.Estado = anotacion.EsSituacion ? EstadoCaso.Abierto : EstadoCaso.Cerrado;
        return anotacion;
    }

    public SeguimientoAnotacion AgregarSeguimiento(string texto, string autorNombre, EstadoCaso? nuevoEstado)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(texto);
        var seguimiento = new SeguimientoAnotacion(Id, texto.Trim(), Recortar(autorNombre, AutorMaxLength));
        _seguimientos.Add(seguimiento);
        if (EsSituacion)
        {
            Estado = nuevoEstado ?? (Estado == EstadoCaso.Abierto ? EstadoCaso.EnSeguimiento : Estado);
        }

        return seguimiento;
    }

    public void MarcarEnterado(string acudiente, DateTimeOffset ahora)
    {
        if (EnteradoEn is null)
        {
            EnteradoEn = ahora;
            EnteradoPor = Recortar(acudiente, AutorMaxLength);
        }
    }

    private static string Recortar(string texto, int maximo) => texto.Length > maximo ? texto[..maximo] : texto;
}

public sealed class SeguimientoAnotacion : Entity
{
    public const int TextoMaxLength = 2000;

    private SeguimientoAnotacion()
    {
    }

    internal SeguimientoAnotacion(Guid anotacionId, string texto, string autorNombre)
    {
        AnotacionId = anotacionId;
        Texto = texto;
        AutorNombre = autorNombre;
    }

    public Guid AnotacionId { get; private set; }

    public string Texto { get; private set; } = string.Empty;

    public string AutorNombre { get; private set; } = string.Empty;
}
