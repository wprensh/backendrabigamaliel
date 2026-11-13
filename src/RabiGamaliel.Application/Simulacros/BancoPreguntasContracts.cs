using RabiGamaliel.Domain.Simulacros;

namespace RabiGamaliel.Application.Simulacros;

public sealed record PreguntaBancoResponse(
    Guid Id, AreaEvaluada Area, string? Competencia, string? Contexto, string Enunciado, string? ImagenUrl, IReadOnlyList<string> Opciones,
    int Correcta, string? Explicacion, IReadOnlyList<int> Grados, DificultadPregunta Dificultad, int Usos, DateTimeOffset CreadoEn)
{
    public static PreguntaBancoResponse Desde(PreguntaBanco p, int usos) => new(
        p.Id, p.Area, p.Competencia, p.Contexto, p.Enunciado, p.ImagenUrl, p.Opciones, p.Correcta, p.Explicacion, p.Grados, p.Dificultad, usos,
        p.CreadoEn);
}

public sealed record GuardarPreguntaBancoRequest(
    AreaEvaluada Area, string? Competencia, string? Contexto, string Enunciado, string? ImagenUrl, IReadOnlyList<string> Opciones, int Correcta,
    string? Explicacion, IReadOnlyList<int>? Grados, DificultadPregunta Dificultad);

/// <summary>Varias preguntas a la vez (p. ej. pegadas como texto).</summary>
public sealed record GuardarLoteBancoRequest(IReadOnlyList<GuardarPreguntaBancoRequest> Preguntas)
{
    public const int Maximo = 300;
}

/// <summary>Filtros de GET /api/simulacros/banco. Todos opcionales.</summary>
public sealed record FiltroBanco(AreaEvaluada? Area, int? Grado, DificultadPregunta? Dificultad, string? Texto);

/// <summary>
/// Agrega preguntas del banco al final del simulacro: las elegidas en <see cref="PreguntaIds"/> o, si no se envían,
/// <see cref="Cantidad"/> preguntas al azar repartidas por igual entre <see cref="Areas"/> (por defecto, las de la prueba).
/// Nunca repite una pregunta que el simulacro ya tenga.
/// </summary>
public sealed record AgregarDesdeBancoRequest(
    IReadOnlyList<Guid>? PreguntaIds, int? Cantidad, IReadOnlyList<AreaEvaluada>? Areas, DificultadPregunta? Dificultad);

public sealed record AgregadasDesdeBancoResponse(int Agregadas, int Solicitadas, SimulacroDetalleResponse Simulacro);
