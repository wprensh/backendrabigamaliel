using RabiGamaliel.Domain.Simulacros;

namespace RabiGamaliel.Application.Simulacros;

public sealed record SimulacroResponse(
    Guid Id, string Titulo, string? Instrucciones, TipoPrueba Tipo, IReadOnlyList<int> Grados, DateTimeOffset Inicio, DateTimeOffset Fin,
    int DuracionMinutos, bool MostrarRespuestas, EstadoSimulacro Estado, int TotalPreguntas, int Presentados, DateTimeOffset CreadoEn)
{
    public static SimulacroResponse Desde(Simulacro s, DateTimeOffset ahora, int preguntas, int presentados) => new(
        s.Id, s.Titulo, s.Instrucciones, s.Tipo, s.Grados, s.Inicio, s.Fin, s.DuracionMinutos, s.MostrarRespuestas, s.Estado(ahora), preguntas,
        presentados, s.CreadoEn);
}

public sealed record GuardarSimulacroRequest(
    string Titulo, string? Instrucciones, TipoPrueba Tipo, IReadOnlyList<int> Grados, DateTimeOffset Inicio, DateTimeOffset Fin,
    int DuracionMinutos, bool MostrarRespuestas);

public sealed record PreguntaResponse(
    Guid Id, int Orden, AreaEvaluada Area, string? Competencia, string? Contexto, string Enunciado, string? ImagenUrl,
    IReadOnlyList<string> Opciones, int Correcta, string? Explicacion, Guid? PreguntaBancoId = null)
{
    public static PreguntaResponse Desde(PreguntaSimulacro p) =>
        new(p.Id, p.Orden, p.Area, p.Competencia, p.Contexto, p.Enunciado, p.ImagenUrl, p.Opciones, p.Correcta, p.Explicacion, p.PreguntaBancoId);
}

public sealed record PreguntaRequest(
    Guid? Id, AreaEvaluada Area, string? Competencia, string? Contexto, string Enunciado, string? ImagenUrl, IReadOnlyList<string> Opciones,
    int Correcta, string? Explicacion);

/// <summary>Cuerpo de PUT /api/simulacros/{id}/preguntas: la lista completa en orden.</summary>
public sealed record GuardarPreguntasRequest(IReadOnlyList<PreguntaRequest> Preguntas)
{
    public const int Maximo = 300;
}

public sealed record SimulacroDetalleResponse(SimulacroResponse Simulacro, IReadOnlyList<PreguntaResponse> Preguntas);

// ---------- Resultados ----------

public sealed record PuntajeArea(AreaEvaluada Area, int Aciertos, int Total, double Porcentaje);

public sealed record PuntajeCompetencia(AreaEvaluada Area, string Competencia, int Aciertos, int Total, double Porcentaje);

public sealed record PromedioArea(AreaEvaluada Area, int Preguntas, double Promedio);

public sealed record PromedioCompetencia(AreaEvaluada Area, string Competencia, double Promedio);

public sealed record PromedioCurso(int Grado, string? Grupo, int Presentados, double Promedio, int? PromedioGlobal);

/// <summary>Análisis de la pregunta: porcentaje de acierto y cuántos eligieron cada opción.</summary>
public sealed record AnalisisPregunta(
    Guid PreguntaId, int Orden, AreaEvaluada Area, string? Competencia, string Enunciado, int Correcta, double PorcentajeAcierto,
    IReadOnlyList<int> Distribucion, int SinResponder);

public sealed record ResultadoEstudiante(
    Guid EstudianteId, string NombreCompleto, int Grado, string? Grupo, int Aciertos, int Total, double Porcentaje, int? Global,
    IReadOnlyList<PuntajeArea> PorArea, DateTimeOffset? EntregadoEn);

public sealed record ResultadosSimulacroResponse(
    SimulacroResponse Simulacro, int Habilitados, int Presentados, int EnCurso, double PromedioPorcentaje, int? PromedioGlobal,
    IReadOnlyList<PromedioArea> PorArea, IReadOnlyList<PromedioCompetencia> PorCompetencia, IReadOnlyList<PromedioCurso> PorCurso,
    IReadOnlyList<AnalisisPregunta> PorPregunta, IReadOnlyList<ResultadoEstudiante> Estudiantes);

// ---------- Campus ----------

public enum EstadoIntento
{
    SinIniciar = 1,
    EnCurso,
    Entregado,
}

public sealed record SimulacroCampusResponse(
    Guid Id, string Titulo, TipoPrueba Tipo, IReadOnlyList<int> Grados, DateTimeOffset Inicio, DateTimeOffset Fin, int DuracionMinutos,
    EstadoSimulacro Estado, int TotalPreguntas, EstadoIntento? Intento, double? Porcentaje, int? Global, int? Presentados,
    Guid? EstudianteId = null, string? EstudianteNombre = null);

public sealed record PreguntaPresentacion(
    Guid Id, int Orden, AreaEvaluada Area, string? Competencia, string? Contexto, string Enunciado, string? ImagenUrl, IReadOnlyList<string> Opciones);

public sealed record PresentacionResponse(
    Guid Id, string Titulo, string? Instrucciones, TipoPrueba Tipo, DateTimeOffset LimiteEn, DateTimeOffset Ahora,
    IReadOnlyList<PreguntaPresentacion> Preguntas, IReadOnlyDictionary<Guid, int> Respuestas);

public sealed record ResponderRequest(Guid PreguntaId, int? Opcion);

public sealed record RevisionPregunta(
    Guid PreguntaId, int Orden, AreaEvaluada Area, string? Contexto, string Enunciado, string? ImagenUrl, IReadOnlyList<string> Opciones,
    int? Elegida, int Correcta, string? Explicacion);

public sealed record ResultadoIntentoResponse(
    Guid SimulacroId, string Titulo, TipoPrueba Tipo, int Aciertos, int Total, double Porcentaje, int? Global, IReadOnlyList<PuntajeArea> PorArea,
    IReadOnlyList<PuntajeCompetencia> PorCompetencia, DateTimeOffset? EntregadoEn, IReadOnlyList<RevisionPregunta>? Revision,
    string? MensajeRevision);
