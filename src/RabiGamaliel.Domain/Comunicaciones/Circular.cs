using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Comunicaciones;

/// <summary>A quién va dirigida una circular.</summary>
public enum PublicoCircular
{
    /// <summary>Acudientes y estudiantes.</summary>
    Familias = 1,
    Acudientes,
    Estudiantes,
    Docentes,
    Todos,
}

/// <summary>
/// Circular institucional numerada ("Circular 012 de 2027"). Se publica en el campus para su público y, si se pide,
/// cada destinatario confirma que la leyó: rectoría ve quién falta.
/// </summary>
public sealed class Circular : Entity
{
    public const int TituloMaxLength = 150;
    public const int ContenidoMaxLength = 10000;
    public const int EnlaceMaxLength = 500;

    private List<int> _grados = [];

    private Circular()
    {
    }

    public int Anio { get; private set; }

    public int Numero { get; private set; }

    public string Titulo { get; private set; } = string.Empty;

    public string Contenido { get; private set; } = string.Empty;

    public PublicoCircular Publico { get; private set; }

    /// <summary>Grados a los que va dirigida; vacío = todos.</summary>
    public IReadOnlyList<int> Grados => _grados;

    public bool RequiereConfirmacion { get; private set; }

    /// <summary>Enlace a un documento adjunto (PDF en Drive, formulario, etc.).</summary>
    public string? EnlaceAdjunto { get; private set; }

    /// <summary>Null mientras es borrador.</summary>
    public DateTimeOffset? PublicadaEn { get; private set; }

    public string Codigo => $"Circular {Numero:000} de {Anio}";

    public static Circular Crear(
        int anio, int numero, string titulo, string contenido, PublicoCircular publico, IEnumerable<int> grados, bool requiereConfirmacion,
        string? enlaceAdjunto)
    {
        var circular = new Circular { Anio = anio, Numero = numero };
        circular.Actualizar(titulo, contenido, publico, grados, requiereConfirmacion, enlaceAdjunto);
        return circular;
    }

    public void Actualizar(
        string titulo, string contenido, PublicoCircular publico, IEnumerable<int> grados, bool requiereConfirmacion, string? enlaceAdjunto)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titulo);
        ArgumentException.ThrowIfNullOrWhiteSpace(contenido);
        Titulo = titulo.Trim();
        Contenido = contenido.Trim();
        Publico = publico;
        _grados = [.. grados.Distinct().Order()];
        RequiereConfirmacion = requiereConfirmacion;
        EnlaceAdjunto = string.IsNullOrWhiteSpace(enlaceAdjunto) ? null : enlaceAdjunto.Trim();
    }

    public void Publicar(DateTimeOffset ahora) => PublicadaEn ??= ahora;

    public bool EsPara(RolCampus rol) => Publico switch
    {
        PublicoCircular.Todos => true,
        PublicoCircular.Familias => rol is RolCampus.Acudiente or RolCampus.Estudiante,
        PublicoCircular.Acudientes => rol == RolCampus.Acudiente,
        PublicoCircular.Estudiantes => rol == RolCampus.Estudiante,
        _ => rol == RolCampus.Docente,
    };

    /// <summary>¿Aplica a alguno de esos grados? Para docentes no se filtra por grado.</summary>
    public bool AplicaAGrados(RolCampus rol, IReadOnlyCollection<int> grados) =>
        rol == RolCampus.Docente || _grados.Count == 0 || grados.Any(_grados.Contains);
}

/// <summary>Constancia de que un miembro del campus leyó (y, si se pidió, confirmó) una circular.</summary>
public sealed class LecturaCircular : Entity
{
    private LecturaCircular()
    {
    }

    public Guid CircularId { get; private set; }

    public Guid MiembroId { get; private set; }

    public DateTimeOffset LeidaEn { get; private set; }

    public DateTimeOffset? ConfirmadaEn { get; private set; }

    public static LecturaCircular Registrar(Guid circularId, Guid miembroId, DateTimeOffset ahora) =>
        new() { CircularId = circularId, MiembroId = miembroId, LeidaEn = ahora };

    public void Confirmar(DateTimeOffset ahora) => ConfirmadaEn ??= ahora;
}

public enum TipoEvento
{
    Academico = 1,
    Institucional,
    Reunion,
    Evaluacion,
    Festivo,
    Deportivo,
    Cultural,
}

/// <summary>Evento del calendario escolar; los públicos se ven en el sitio web, todos en el campus.</summary>
public sealed class EventoCalendario : Entity
{
    public const int TituloMaxLength = 150;
    public const int DescripcionMaxLength = 1000;
    public const int LugarMaxLength = 120;
    public const int HoraMaxLength = 30;

    private EventoCalendario()
    {
    }

    public string Titulo { get; private set; } = string.Empty;

    public string? Descripcion { get; private set; }

    public DateOnly Inicio { get; private set; }

    public DateOnly Fin { get; private set; }

    /// <summary>Hora en texto libre ("7:00 a. m.", "Jornada de la tarde").</summary>
    public string? Hora { get; private set; }

    public string? Lugar { get; private set; }

    public TipoEvento Tipo { get; private set; }

    /// <summary>Visible en el sitio web sin iniciar sesión.</summary>
    public bool Publico { get; private set; }

    public static EventoCalendario Crear(
        string titulo, string? descripcion, DateOnly inicio, DateOnly? fin, string? hora, string? lugar, TipoEvento tipo, bool publico)
    {
        var evento = new EventoCalendario();
        evento.Actualizar(titulo, descripcion, inicio, fin, hora, lugar, tipo, publico);
        return evento;
    }

    public void Actualizar(
        string titulo, string? descripcion, DateOnly inicio, DateOnly? fin, string? hora, string? lugar, TipoEvento tipo, bool publico)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titulo);
        var hasta = fin ?? inicio;
        if (hasta < inicio)
        {
            throw new ArgumentException("El evento debe terminar después de empezar.", nameof(fin));
        }

        Titulo = titulo.Trim();
        Descripcion = Limpiar(descripcion);
        Inicio = inicio;
        Fin = hasta;
        Hora = Limpiar(hora);
        Lugar = Limpiar(lugar);
        Tipo = tipo;
        Publico = publico;
    }

    private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
