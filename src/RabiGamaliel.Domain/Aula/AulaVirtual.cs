using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Aula;

public enum TipoActividad
{
    /// <summary>Guía, lectura, video o presentación para estudiar.</summary>
    Material = 1,

    /// <summary>Trabajo que el estudiante entrega y el docente califica.</summary>
    Tarea,
}

/// <summary>Material o tarea que el docente publica en el aula virtual de una de sus clases.</summary>
public sealed class ActividadAula : Entity
{
    public const int TituloMaxLength = 150;
    public const int DescripcionMaxLength = 4000;
    public const int EnlaceMaxLength = 500;

    private ActividadAula()
    {
    }

    public Guid AsignacionId { get; private set; }

    public Guid PeriodoId { get; private set; }

    public Guid DocenteId { get; private set; }

    public TipoActividad Tipo { get; private set; }

    public string Titulo { get; private set; } = string.Empty;

    public string? Descripcion { get; private set; }

    /// <summary>Enlace a un video, documento o página (https).</summary>
    public string? Enlace { get; private set; }

    public Guid? ArchivoId { get; private set; }

    /// <summary>Fecha y hora límite de entrega (solo tareas).</summary>
    public DateTimeOffset? FechaEntrega { get; private set; }

    /// <summary>Si la nota de la tarea cuenta para el promedio que se pasa a la planilla.</summary>
    public bool Calificable { get; private set; }

    public static ActividadAula Crear(Guid asignacionId, Guid periodoId, Guid docenteId, TipoActividad tipo) => new()
    {
        AsignacionId = asignacionId,
        PeriodoId = periodoId,
        DocenteId = docenteId,
        Tipo = tipo,
    };

    public void Actualizar(
        Guid periodoId, string titulo, string? descripcion, string? enlace, Guid? archivoId, DateTimeOffset? fechaEntrega, bool calificable)
    {
        PeriodoId = periodoId;
        Titulo = titulo.Trim();
        Descripcion = Limpiar(descripcion);
        Enlace = Limpiar(enlace);
        ArchivoId = archivoId;
        FechaEntrega = Tipo == TipoActividad.Tarea ? fechaEntrega?.ToUniversalTime() : null;
        Calificable = Tipo == TipoActividad.Tarea && calificable;
    }

    internal static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}

/// <summary>Entrega de un estudiante a una tarea, con la nota y el comentario del docente.</summary>
public sealed class EntregaAula : Entity
{
    public const int TextoMaxLength = 4000;
    public const int ComentarioMaxLength = 1000;

    private EntregaAula()
    {
    }

    public Guid ActividadId { get; private set; }

    public Guid EstudianteId { get; private set; }

    public string? Texto { get; private set; }

    public string? Enlace { get; private set; }

    public Guid? ArchivoId { get; private set; }

    /// <summary>Null si el docente calificó sin que el estudiante entregara (p. ej. trabajo en clase o no entregó).</summary>
    public DateTimeOffset? EntregadaEn { get; private set; }

    public decimal? Nota { get; private set; }

    public string? Comentario { get; private set; }

    public DateTimeOffset? CalificadaEn { get; private set; }

    public static EntregaAula Crear(Guid actividadId, Guid estudianteId) => new() { ActividadId = actividadId, EstudianteId = estudianteId };

    /// <summary>El estudiante entrega o reemplaza su entrega (mientras no esté calificada).</summary>
    public void Entregar(string? texto, string? enlace, Guid? archivoId, DateTimeOffset ahora)
    {
        if (CalificadaEn is not null)
        {
            throw new InvalidOperationException("La entrega ya fue calificada.");
        }

        Texto = ActividadAula.Limpiar(texto);
        Enlace = ActividadAula.Limpiar(enlace);
        ArchivoId = archivoId;
        EntregadaEn = ahora;
    }

    public void Calificar(decimal? nota, string? comentario, DateTimeOffset ahora)
    {
        if (nota is { } n && !EscalaValorativa.EsValida(n))
        {
            throw new ArgumentOutOfRangeException(nameof(nota), $"La nota va de {EscalaValorativa.Minima} a {EscalaValorativa.Maxima}.");
        }

        Nota = nota is { } valor ? Math.Round(valor, 1, MidpointRounding.AwayFromZero) : null;
        Comentario = ActividadAula.Limpiar(comentario);
        CalificadaEn = nota is null && Comentario is null ? null : ahora;
    }

    public bool Tarde(DateTimeOffset? limite) => limite is { } l && EntregadaEn is { } e && e > l;
}

/// <summary>Archivo adjunto del aula virtual (guía del docente o entrega del estudiante), guardado en la base de datos.</summary>
public sealed class ArchivoAula : Entity
{
    public const int NombreArchivoMaxLength = 150;
    public const int TamanoMaximoBytes = 5 * 1024 * 1024;

    private ArchivoAula()
    {
    }

    public Guid SubidoPorId { get; private set; }

    public string NombreArchivo { get; private set; } = string.Empty;

    public string TipoContenido { get; private set; } = string.Empty;

    public int Tamano { get; private set; }

#pragma warning disable CA1819 // El contenido del archivo es un arreglo de bytes.
    public byte[] Contenido { get; private set; } = [];
#pragma warning restore CA1819

    public static ArchivoAula Crear(Guid subidoPorId, string nombreArchivo, string tipoContenido, byte[] contenido)
    {
        var nombre = Path.GetFileName(nombreArchivo).Trim();
        return new ArchivoAula
        {
            SubidoPorId = subidoPorId,
            NombreArchivo = nombre.Length > NombreArchivoMaxLength ? nombre[^NombreArchivoMaxLength..] : nombre,
            TipoContenido = tipoContenido,
            Tamano = contenido.Length,
            Contenido = contenido,
        };
    }
}
