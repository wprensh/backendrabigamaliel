using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Asistencia;

/// <summary>Novedad de un estudiante en una clase. Quien no tiene novedad estuvo presente.</summary>
public enum EstadoAsistencia
{
    Ausente = 1,
    Tarde,

    /// <summary>Faltó con excusa: no genera aviso al acudiente.</summary>
    Excusa,
}

/// <summary>
/// Asistencia de una clase en una fecha: la toma el docente de la asignatura desde el campus. Solo se guardan
/// las novedades (ausencias, llegadas tarde y excusas); el resto del curso estuvo presente.
/// </summary>
public sealed class TomaAsistencia : Entity
{
    private readonly List<NovedadAsistencia> _novedades = [];

    private TomaAsistencia()
    {
    }

    public Guid AsignacionId { get; private set; }

    public Guid AsignaturaId { get; private set; }

    public Guid DocenteId { get; private set; }

    public DateOnly Fecha { get; private set; }

    public int Grado { get; private set; }

    public string? Grupo { get; private set; }

    /// <summary>Estudiantes del curso ese día, para calcular el porcentaje de asistencia.</summary>
    public int TotalEstudiantes { get; private set; }

    public IReadOnlyList<NovedadAsistencia> Novedades => _novedades;

    public static TomaAsistencia Crear(Guid asignacionId, Guid asignaturaId, Guid docenteId, DateOnly fecha, int grado, string? grupo) => new()
    {
        AsignacionId = asignacionId,
        AsignaturaId = asignaturaId,
        DocenteId = docenteId,
        Fecha = fecha,
        Grado = grado,
        Grupo = grupo,
    };

    /// <summary>Reemplaza las novedades. Devuelve las que son nuevas o cambiaron (para avisar a los acudientes).</summary>
    public IReadOnlyList<NovedadAsistencia> Registrar(
        Guid docenteId, int totalEstudiantes, IEnumerable<(Guid EstudianteId, EstadoAsistencia Estado, string? Observacion)> novedades)
    {
        DocenteId = docenteId;
        TotalEstudiantes = Math.Max(0, totalEstudiantes);
        var anteriores = _novedades.ToDictionary(n => n.EstudianteId);
        var cambiadas = new List<NovedadAsistencia>();
        _novedades.Clear();
        foreach (var (estudianteId, estado, observacion) in novedades.DistinctBy(n => n.EstudianteId))
        {
            if (anteriores.TryGetValue(estudianteId, out var anterior))
            {
                var cambio = anterior.Estado != estado;
                anterior.Cambiar(estado, observacion);
                _novedades.Add(anterior);
                if (cambio)
                {
                    cambiadas.Add(anterior);
                }
            }
            else
            {
                var nueva = new NovedadAsistencia(Id, estudianteId, estado, observacion);
                _novedades.Add(nueva);
                cambiadas.Add(nueva);
            }
        }

        return cambiadas;
    }
}

public sealed class NovedadAsistencia : Entity
{
    public const int ObservacionMaxLength = 200;

    private NovedadAsistencia()
    {
    }

    internal NovedadAsistencia(Guid tomaId, Guid estudianteId, EstadoAsistencia estado, string? observacion)
    {
        TomaId = tomaId;
        EstudianteId = estudianteId;
        Cambiar(estado, observacion);
    }

    public Guid TomaId { get; private set; }

    public Guid EstudianteId { get; private set; }

    public EstadoAsistencia Estado { get; private set; }

    public string? Observacion { get; private set; }

    internal void Cambiar(EstadoAsistencia estado, string? observacion)
    {
        Estado = estado;
        Observacion = string.IsNullOrWhiteSpace(observacion)
            ? null
            : observacion.Trim()[..Math.Min(observacion.Trim().Length, ObservacionMaxLength)];
    }
}
