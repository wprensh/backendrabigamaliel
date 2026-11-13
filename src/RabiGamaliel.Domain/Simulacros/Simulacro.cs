using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Simulacros;

public enum TipoPrueba
{
    Saber11 = 1,
    Saber9,
    Saber5,
    Saber3,

    /// <summary>Evaluación propia del colegio (bimestral, de periodo...).</summary>
    Interna,
}

/// <summary>Áreas que evalúan las pruebas Saber del ICFES.</summary>
public enum AreaEvaluada
{
    LecturaCritica = 1,
    Matematicas,
    SocialesCiudadanas,
    CienciasNaturales,
    Ingles,

    /// <summary>Lenguaje (Saber 3°, 5° y 9°).</summary>
    Lenguaje,
    CompetenciasCiudadanas,
    Otra,
}

/// <summary>
/// Simulacro tipo Saber que los estudiantes presentan en el campus, con tiempo límite y calificación automática
/// por área y competencia.
/// </summary>
public sealed class Simulacro : Entity
{
    public const int TituloMaxLength = 150;
    public const int InstruccionesMaxLength = 3000;
    public const int DuracionMaximaMinutos = 600;

    private List<int> _grados = [];

    private Simulacro()
    {
    }

    public string Titulo { get; private set; } = string.Empty;

    public string? Instrucciones { get; private set; }

    public TipoPrueba Tipo { get; private set; }

    public IReadOnlyList<int> Grados => _grados;

    /// <summary>Desde cuándo se puede presentar.</summary>
    public DateTimeOffset Inicio { get; private set; }

    /// <summary>Hasta cuándo se puede presentar; quien esté respondiendo a esa hora entrega lo que lleve.</summary>
    public DateTimeOffset Fin { get; private set; }

    public int DuracionMinutos { get; private set; }

    /// <summary>Al cerrar, el estudiante ve las respuestas correctas y la explicación de cada pregunta.</summary>
    public bool MostrarRespuestas { get; private set; }

    public static Simulacro Crear(
        string titulo, string? instrucciones, TipoPrueba tipo, IEnumerable<int> grados, DateTimeOffset inicio, DateTimeOffset fin,
        int duracionMinutos, bool mostrarRespuestas)
    {
        var simulacro = new Simulacro();
        simulacro.Actualizar(titulo, instrucciones, tipo, grados, inicio, fin, duracionMinutos, mostrarRespuestas);
        return simulacro;
    }

    public void Actualizar(
        string titulo, string? instrucciones, TipoPrueba tipo, IEnumerable<int> grados, DateTimeOffset inicio, DateTimeOffset fin,
        int duracionMinutos, bool mostrarRespuestas)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titulo);
        if (fin <= inicio)
        {
            throw new ArgumentException("El cierre debe ser posterior a la apertura.", nameof(fin));
        }

        Titulo = titulo.Trim();
        Instrucciones = string.IsNullOrWhiteSpace(instrucciones) ? null : instrucciones.Trim();
        Tipo = tipo;
        _grados = [.. grados.Distinct().Order()];
        // PostgreSQL (timestamptz) solo acepta fechas en UTC.
        Inicio = inicio.ToUniversalTime();
        Fin = fin.ToUniversalTime();
        DuracionMinutos = Math.Clamp(duracionMinutos, 1, DuracionMaximaMinutos);
        MostrarRespuestas = mostrarRespuestas;
    }

    public bool EsParaGrado(int? grado) => grado is { } g && _grados.Contains(g);

    public EstadoSimulacro Estado(DateTimeOffset ahora) =>
        ahora < Inicio ? EstadoSimulacro.Programado
        : ahora < Fin ? EstadoSimulacro.Abierto
        : EstadoSimulacro.Cerrado;
}

public enum EstadoSimulacro
{
    Programado = 1,
    Abierto,
    Cerrado,
}

/// <summary>Pregunta de selección múltiple con única respuesta, como en las pruebas Saber.</summary>
public sealed class PreguntaSimulacro : Entity
{
    public const int CompetenciaMaxLength = 120;
    public const int ContextoMaxLength = 6000;
    public const int EnunciadoMaxLength = 3000;
    public const int OpcionMaxLength = 1000;
    public const int ExplicacionMaxLength = 2000;
    public const int ImagenUrlMaxLength = 500;
    public const int OpcionesMinimas = 2;
    public const int OpcionesMaximas = 5;

    private List<string> _opciones = [];

    private PreguntaSimulacro()
    {
    }

    public Guid SimulacroId { get; private set; }

    public int Orden { get; private set; }

    public AreaEvaluada Area { get; private set; }

    /// <summary>Competencia o componente evaluado (p. ej. "Razonamiento cuantitativo").</summary>
    public string? Competencia { get; private set; }

    /// <summary>Texto, tabla o situación que comparten varias preguntas (opcional).</summary>
    public string? Contexto { get; private set; }

    public string Enunciado { get; private set; } = string.Empty;

    public string? ImagenUrl { get; private set; }

    public IReadOnlyList<string> Opciones => _opciones;

    /// <summary>Posición (desde 0) de la opción correcta.</summary>
    public int Correcta { get; private set; }

    public string? Explicacion { get; private set; }

    /// <summary>Pregunta del banco de la que se copió (null si se escribió en el simulacro).</summary>
    public Guid? PreguntaBancoId { get; private set; }

    public static PreguntaSimulacro Crear(
        Guid simulacroId, int orden, AreaEvaluada area, string? competencia, string? contexto, string enunciado, string? imagenUrl,
        IReadOnlyList<string> opciones, int correcta, string? explicacion, Guid? preguntaBancoId = null)
    {
        var pregunta = new PreguntaSimulacro { SimulacroId = simulacroId, PreguntaBancoId = preguntaBancoId };
        pregunta.Actualizar(orden, area, competencia, contexto, enunciado, imagenUrl, opciones, correcta, explicacion);
        return pregunta;
    }

    public void Actualizar(
        int orden, AreaEvaluada area, string? competencia, string? contexto, string enunciado, string? imagenUrl,
        IReadOnlyList<string> opciones, int correcta, string? explicacion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enunciado);
        var limpias = opciones.Select(o => o?.Trim() ?? string.Empty).ToList();
        if (limpias.Count is < OpcionesMinimas or > OpcionesMaximas || limpias.Any(string.IsNullOrEmpty))
        {
            throw new ArgumentException($"Una pregunta lleva de {OpcionesMinimas} a {OpcionesMaximas} opciones.", nameof(opciones));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(correcta);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(correcta, limpias.Count);
        Orden = orden;
        Area = area;
        Competencia = Limpiar(competencia);
        Contexto = Limpiar(contexto);
        Enunciado = enunciado.Trim();
        ImagenUrl = Limpiar(imagenUrl);
        _opciones = limpias;
        Correcta = correcta;
        Explicacion = Limpiar(explicacion);
    }

    private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}

/// <summary>Presentación de un simulacro por un estudiante. Las respuestas se guardan a medida que contesta.</summary>
public sealed class IntentoSimulacro : Entity
{
    private Dictionary<Guid, int> _respuestas = [];

    private IntentoSimulacro()
    {
    }

    public Guid SimulacroId { get; private set; }

    public Guid EstudianteId { get; private set; }

    public int Grado { get; private set; }

    public string? Grupo { get; private set; }

    public DateTimeOffset IniciadoEn { get; private set; }

    /// <summary>Hora límite: inicio + duración, sin pasar del cierre del simulacro.</summary>
    public DateTimeOffset LimiteEn { get; private set; }

    public DateTimeOffset? EntregadoEn { get; private set; }

    public int Aciertos { get; private set; }

    public int TotalPreguntas { get; private set; }

    /// <summary>Opción elegida (desde 0) por id de pregunta.</summary>
    public IReadOnlyDictionary<Guid, int> Respuestas => _respuestas;

    public static IntentoSimulacro Iniciar(Simulacro simulacro, Guid estudianteId, int grado, string? grupo, DateTimeOffset ahora)
    {
        var limite = ahora.AddMinutes(simulacro.DuracionMinutos);
        return new IntentoSimulacro
        {
            SimulacroId = simulacro.Id,
            EstudianteId = estudianteId,
            Grado = grado,
            Grupo = grupo,
            IniciadoEn = ahora,
            LimiteEn = limite < simulacro.Fin ? limite : simulacro.Fin,
        };
    }

    public bool Entregado => EntregadoEn is not null;

    public bool TiempoAgotado(DateTimeOffset ahora) => ahora >= LimiteEn;

    public void Responder(Guid preguntaId, int? opcion)
    {
        if (Entregado)
        {
            throw new InvalidOperationException("El simulacro ya fue entregado.");
        }

        // Diccionario nuevo en cada cambio: EF detecta la modificación por referencia.
        var copia = new Dictionary<Guid, int>(_respuestas);
        if (opcion is { } valor)
        {
            copia[preguntaId] = valor;
        }
        else
        {
            copia.Remove(preguntaId);
        }

        _respuestas = copia;
    }

    /// <summary>Califica y cierra el intento. Si se agotó el tiempo, la entrega queda con la hora límite.</summary>
    public void Entregar(IReadOnlyList<PreguntaSimulacro> preguntas, DateTimeOffset ahora)
    {
        if (Entregado)
        {
            return;
        }

        TotalPreguntas = preguntas.Count;
        Aciertos = preguntas.Count(p => _respuestas.TryGetValue(p.Id, out var r) && r == p.Correcta);
        EntregadoEn = ahora < LimiteEn ? ahora : LimiteEn;
    }
}

/// <summary>Puntajes con la estructura de las pruebas Saber.</summary>
public static class PuntajeSaber
{
    /// <summary>
    /// Puntaje global estimado de Saber 11 (0 a 500): promedio ponderado de las áreas (inglés pesa 1, las demás 3),
    /// multiplicado por 5. Es una aproximación: el ICFES usa modelos de respuesta al ítem.
    /// </summary>
    public static int? GlobalSaber11(IReadOnlyDictionary<AreaEvaluada, double> puntajePorArea)
    {
        AreaEvaluada[] obligatorias =
            [AreaEvaluada.LecturaCritica, AreaEvaluada.Matematicas, AreaEvaluada.SocialesCiudadanas, AreaEvaluada.CienciasNaturales, AreaEvaluada.Ingles];
        if (!obligatorias.All(puntajePorArea.ContainsKey))
        {
            return null;
        }

        var ponderado = obligatorias.Sum(a => puntajePorArea[a] * (a == AreaEvaluada.Ingles ? 1 : 3));
        return (int)Math.Round(ponderado / 13 * 5, MidpointRounding.AwayFromZero);
    }

    /// <summary>Puntaje de 0 a 100 del área.</summary>
    public static double Porcentaje(int aciertos, int total) => total == 0 ? 0 : Math.Round(aciertos * 100.0 / total, 1);
}

/// <summary>Áreas que evalúa cada prueba, en el orden del ICFES.</summary>
public static class AreasPrueba
{
    public static IReadOnlyList<AreaEvaluada> De(TipoPrueba tipo) => tipo switch
    {
        TipoPrueba.Saber11 =>
            [AreaEvaluada.LecturaCritica, AreaEvaluada.Matematicas, AreaEvaluada.SocialesCiudadanas, AreaEvaluada.CienciasNaturales, AreaEvaluada.Ingles],
        TipoPrueba.Saber9 or TipoPrueba.Saber5 =>
            [AreaEvaluada.Lenguaje, AreaEvaluada.Matematicas, AreaEvaluada.CienciasNaturales, AreaEvaluada.CompetenciasCiudadanas],
        TipoPrueba.Saber3 => [AreaEvaluada.Lenguaje, AreaEvaluada.Matematicas],
        _ => [.. Enum.GetValues<AreaEvaluada>().Where(a => a != AreaEvaluada.Otra)],
    };
}
