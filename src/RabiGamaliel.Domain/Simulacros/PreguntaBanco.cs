using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Simulacros;

public enum DificultadPregunta
{
    Baja = 1,
    Media,
    Alta,
}

/// <summary>
/// Pregunta del banco del colegio, tipo Saber (selección múltiple con única respuesta). Se reutiliza en varios
/// simulacros: al armar uno, se copia al simulacro, así corregir el banco no cambia pruebas ya presentadas.
/// </summary>
public sealed class PreguntaBanco : Entity
{
    private List<string> _opciones = [];
    private List<int> _grados = [];

    private PreguntaBanco()
    {
    }

    public AreaEvaluada Area { get; private set; }

    public string? Competencia { get; private set; }

    public string? Contexto { get; private set; }

    public string Enunciado { get; private set; } = string.Empty;

    public string? ImagenUrl { get; private set; }

    public IReadOnlyList<string> Opciones => _opciones;

    /// <summary>Posición (desde 0) de la opción correcta.</summary>
    public int Correcta { get; private set; }

    public string? Explicacion { get; private set; }

    /// <summary>Grados para los que sirve la pregunta (p. ej. 10 y 11 para Saber 11).</summary>
    public IReadOnlyList<int> Grados => _grados;

    public DificultadPregunta Dificultad { get; private set; }

    public static PreguntaBanco Crear(
        AreaEvaluada area, string? competencia, string? contexto, string enunciado, string? imagenUrl, IReadOnlyList<string> opciones,
        int correcta, string? explicacion, IEnumerable<int> grados, DificultadPregunta dificultad)
    {
        var pregunta = new PreguntaBanco();
        pregunta.Actualizar(area, competencia, contexto, enunciado, imagenUrl, opciones, correcta, explicacion, grados, dificultad);
        return pregunta;
    }

    public void Actualizar(
        AreaEvaluada area, string? competencia, string? contexto, string enunciado, string? imagenUrl, IReadOnlyList<string> opciones,
        int correcta, string? explicacion, IEnumerable<int> grados, DificultadPregunta dificultad)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(enunciado);
        var limpias = opciones.Select(o => o?.Trim() ?? string.Empty).ToList();
        if (limpias.Count is < PreguntaSimulacro.OpcionesMinimas or > PreguntaSimulacro.OpcionesMaximas || limpias.Any(string.IsNullOrEmpty))
        {
            throw new ArgumentException(
                $"Una pregunta lleva de {PreguntaSimulacro.OpcionesMinimas} a {PreguntaSimulacro.OpcionesMaximas} opciones.", nameof(opciones));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(correcta);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(correcta, limpias.Count);
        Area = area;
        Competencia = Limpiar(competencia);
        Contexto = Limpiar(contexto);
        Enunciado = enunciado.Trim();
        ImagenUrl = Limpiar(imagenUrl);
        _opciones = limpias;
        Correcta = correcta;
        Explicacion = Limpiar(explicacion);
        _grados = [.. grados.Distinct().Order()];
        Dificultad = dificultad;
    }

    /// <summary>Sin grados indicados, sirve para cualquiera.</summary>
    public bool SirveParaGrado(int? grado) => grado is null || _grados.Count == 0 || _grados.Contains(grado.Value);

    /// <summary>Copia la pregunta al final de un simulacro.</summary>
    public PreguntaSimulacro CopiarA(Guid simulacroId, int orden) => PreguntaSimulacro.Crear(
        simulacroId, orden, Area, Competencia, Contexto, Enunciado, ImagenUrl, _opciones, Correcta, Explicacion, Id);

    private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
