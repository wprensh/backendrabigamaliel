using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Avisos;

public enum TipoAviso
{
    Asistencia = 1,
    Convivencia,
    Calificaciones,
    Circular,
    Simulacro,
    General,
    Tarea,
    Mensaje,
    Planeacion,
    Recuperacion,
}

/// <summary>Notificación para un estudiante, docente o acudiente; se ve en la campana del campus y, si hay correo, llega por email.</summary>
public sealed class Aviso : Entity
{
    public const int TituloMaxLength = 150;
    public const int MensajeMaxLength = 1000;
    public const int EnlaceMaxLength = 200;

    private Aviso()
    {
    }

    public Guid DestinatarioId { get; private set; }

    public TipoAviso Tipo { get; private set; }

    public string Titulo { get; private set; } = string.Empty;

    public string Mensaje { get; private set; } = string.Empty;

    /// <summary>Ruta del sitio a la que lleva el aviso (p. ej. "/campus/asistencia").</summary>
    public string? Enlace { get; private set; }

    /// <summary>Estudiante al que se refiere (para los acudientes con varios hijos).</summary>
    public Guid? EstudianteId { get; private set; }

    public DateTimeOffset? LeidoEn { get; private set; }

    public static Aviso Crear(Guid destinatarioId, TipoAviso tipo, string titulo, string mensaje, string? enlace, Guid? estudianteId) => new()
    {
        DestinatarioId = destinatarioId,
        Tipo = tipo,
        Titulo = Recortar(titulo, TituloMaxLength),
        Mensaje = Recortar(mensaje, MensajeMaxLength),
        Enlace = enlace is null ? null : Recortar(enlace, EnlaceMaxLength),
        EstudianteId = estudianteId,
    };

    public void MarcarLeido(DateTimeOffset ahora) => LeidoEn ??= ahora;

    private static string Recortar(string texto, int maximo) => texto.Length > maximo ? texto[..maximo] : texto;
}
