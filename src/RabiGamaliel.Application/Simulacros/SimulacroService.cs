using FluentValidation;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Simulacros;

namespace RabiGamaliel.Application.Simulacros;

public interface ISimulacroService
{
    Task<IReadOnlyList<SimulacroResponse>> ListarAsync(CancellationToken ct);

    Task<Result<SimulacroDetalleResponse>> ObtenerAsync(Guid id, CancellationToken ct);

    Task<Result<SimulacroResponse>> CrearAsync(GuardarSimulacroRequest request, CancellationToken ct);

    Task<Result<SimulacroResponse>> ActualizarAsync(Guid id, GuardarSimulacroRequest request, CancellationToken ct);

    Task<Result<Unit>> EliminarAsync(Guid id, CancellationToken ct);

    Task<Result<SimulacroDetalleResponse>> GuardarPreguntasAsync(Guid id, GuardarPreguntasRequest request, CancellationToken ct);

    /// <summary>Resultados completos; con <paramref name="cursos"/> solo los de esos cursos (vista del docente).</summary>
    Task<Result<ResultadosSimulacroResponse>> ResultadosAsync(
        Guid id, IReadOnlyCollection<(int Grado, string? Grupo)>? cursos, CancellationToken ct);
}

/// <summary>Simulacros tipo Saber: creación, banco de preguntas y análisis de resultados por área, curso y pregunta.</summary>
public sealed class SimulacroService(
    ISimulacroRepository repository,
    IMiembroCampusRepository miembros,
    IUnitOfWork unitOfWork,
    TimeProvider reloj,
    IValidator<GuardarSimulacroRequest> validator,
    IValidator<GuardarPreguntasRequest> preguntasValidator) : ISimulacroService
{
    internal static Error NoEncontrado(Guid id) => Error.NoEncontrado("Simulacro.NoEncontrado", $"No existe el simulacro '{id}'.");

    public async Task<IReadOnlyList<SimulacroResponse>> ListarAsync(CancellationToken ct)
    {
        var simulacros = await repository.ListarAsync(ct);
        var preguntas = await repository.ConteoPreguntasAsync(ct);
        var presentados = await repository.ConteoPresentadosAsync(ct);
        var ahora = reloj.GetUtcNow();
        return
        [
            .. simulacros.Select(s => SimulacroResponse.Desde(s, ahora, preguntas.GetValueOrDefault(s.Id), presentados.GetValueOrDefault(s.Id))),
        ];
    }

    public async Task<Result<SimulacroDetalleResponse>> ObtenerAsync(Guid id, CancellationToken ct)
    {
        var simulacro = await repository.ObtenerAsync(id, ct);
        return simulacro is null ? NoEncontrado(id) : await DetalleAsync(simulacro, ct);
    }

    public async Task<Result<SimulacroResponse>> CrearAsync(GuardarSimulacroRequest request, CancellationToken ct)
    {
        if (await validator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var simulacro = Simulacro.Crear(
            request.Titulo, request.Instrucciones, request.Tipo, request.Grados, request.Inicio, request.Fin, request.DuracionMinutos,
            request.MostrarRespuestas);
        repository.Agregar(simulacro);
        await unitOfWork.SaveChangesAsync(ct);
        return SimulacroResponse.Desde(simulacro, reloj.GetUtcNow(), 0, 0);
    }

    public async Task<Result<SimulacroResponse>> ActualizarAsync(Guid id, GuardarSimulacroRequest request, CancellationToken ct)
    {
        if (await validator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var simulacro = await repository.ObtenerAsync(id, ct);
        if (simulacro is null)
        {
            return NoEncontrado(id);
        }

        simulacro.Actualizar(
            request.Titulo, request.Instrucciones, request.Tipo, request.Grados, request.Inicio, request.Fin, request.DuracionMinutos,
            request.MostrarRespuestas);
        await unitOfWork.SaveChangesAsync(ct);
        var preguntas = await repository.PreguntasAsync(id, ct);
        var presentados = await repository.ConteoPresentadosAsync(ct);
        return SimulacroResponse.Desde(simulacro, reloj.GetUtcNow(), preguntas.Count, presentados.GetValueOrDefault(id));
    }

    public async Task<Result<Unit>> EliminarAsync(Guid id, CancellationToken ct)
    {
        var simulacro = await repository.ObtenerAsync(id, ct);
        if (simulacro is null)
        {
            return NoEncontrado(id);
        }

        if (await repository.TieneIntentosAsync(id, ct))
        {
            return Error.Conflicto("Simulacro.ConIntentos", "Ya hay estudiantes que presentaron este simulacro; no se puede eliminar.");
        }

        foreach (var pregunta in await repository.PreguntasAsync(id, ct))
        {
            repository.Eliminar(pregunta);
        }

        repository.Eliminar(simulacro);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Result<SimulacroDetalleResponse>> GuardarPreguntasAsync(Guid id, GuardarPreguntasRequest request, CancellationToken ct)
    {
        if (await preguntasValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var simulacro = await repository.ObtenerAsync(id, ct);
        if (simulacro is null)
        {
            return NoEncontrado(id);
        }

        var actuales = (await repository.PreguntasAsync(id, ct)).ToDictionary(p => p.Id);
        if (await repository.TieneIntentosAsync(id, ct))
        {
            // Con estudiantes presentándolo solo se corrigen textos: mismas preguntas y mismas respuestas correctas.
            var mismas = request.Preguntas.Count == actuales.Count
                && request.Preguntas.All(p => p.Id is { } pid && actuales.TryGetValue(pid, out var a)
                    && a.Correcta == p.Correcta && a.Opciones.Count == p.Opciones.Count);
            if (!mismas)
            {
                return Error.Conflicto(
                    "Simulacro.ConIntentos",
                    "Ya hay estudiantes que lo presentaron: puede corregir textos, pero no agregar, quitar preguntas ni cambiar la respuesta correcta.");
            }
        }

        var conservadas = new HashSet<Guid>();
        for (var i = 0; i < request.Preguntas.Count; i++)
        {
            var p = request.Preguntas[i];
            if (p.Id is { } pid && actuales.TryGetValue(pid, out var existente))
            {
                existente.Actualizar(i + 1, p.Area, p.Competencia, p.Contexto, p.Enunciado, p.ImagenUrl, p.Opciones, p.Correcta, p.Explicacion);
                conservadas.Add(pid);
            }
            else
            {
                repository.Agregar(PreguntaSimulacro.Crear(
                    simulacro.Id, i + 1, p.Area, p.Competencia, p.Contexto, p.Enunciado, p.ImagenUrl, p.Opciones, p.Correcta, p.Explicacion));
            }
        }

        foreach (var sobrante in actuales.Values.Where(p => !conservadas.Contains(p.Id)))
        {
            repository.Eliminar(sobrante);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await DetalleAsync(simulacro, ct);
    }

    public async Task<Result<ResultadosSimulacroResponse>> ResultadosAsync(
        Guid id, IReadOnlyCollection<(int Grado, string? Grupo)>? cursos, CancellationToken ct)
    {
        var simulacro = await repository.ObtenerAsync(id, ct);
        if (simulacro is null)
        {
            return NoEncontrado(id);
        }

        var ahora = reloj.GetUtcNow();
        var preguntas = (await repository.PreguntasAsync(id, ct)).OrderBy(p => p.Orden).ToList();
        var intentos = await repository.IntentosAsync(id, ct);

        // Quien se quedó sin tiempo y no entregó, queda entregado con lo que alcanzó a responder.
        var vencidos = intentos.Where(i => !i.Entregado && i.TiempoAgotado(ahora)).ToList();
        foreach (var vencido in vencidos)
        {
            vencido.Entregar(preguntas, ahora);
        }

        if (vencidos.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(ct);
        }

        bool EnCursos(int grado, string? grupo) => cursos is null || cursos.Any(c => c.Grado == grado && (c.Grupo is null || c.Grupo == grupo));
        intentos = [.. intentos.Where(i => EnCursos(i.Grado, i.Grupo))];
        var entregados = intentos.Where(i => i.Entregado).ToList();
        var nombres = (await miembros.ObtenerPorIdsAsync([.. entregados.Select(i => i.EstudianteId)], ct)).ToDictionary(m => m.Id, m => m.NombreCompleto);

        var habilitados = 0;
        foreach (var grado in simulacro.Grados)
        {
            habilitados += (await miembros.EstudiantesDelCursoAsync(grado, null, ct)).Count(e => EnCursos(grado, e.Grupo));
        }

        var estudiantes = entregados.Select(i =>
        {
            var porArea = CalculoSimulacro.PorArea(preguntas, i.Respuestas);
            return new ResultadoEstudiante(
                i.EstudianteId, nombres.GetValueOrDefault(i.EstudianteId, "(estudiante)"), i.Grado, i.Grupo, i.Aciertos, i.TotalPreguntas,
                PuntajeSaber.Porcentaje(i.Aciertos, i.TotalPreguntas), CalculoSimulacro.Global(simulacro.Tipo, porArea), porArea, i.EntregadoEn);
        }).OrderByDescending(r => r.Porcentaje).ThenBy(r => r.NombreCompleto).ToList();

        var porArea = preguntas.GroupBy(p => p.Area).OrderBy(g => g.Key).Select(g => new PromedioArea(
            g.Key, g.Count(),
            entregados.Count == 0 ? 0 : Math.Round(entregados.Average(i => PuntajeSaber.Porcentaje(g.Count(p => CalculoSimulacro.Acerto(p, i.Respuestas)), g.Count())), 1)))
            .ToList();

        var porCompetencia = preguntas.Where(p => p.Competencia is not null).GroupBy(p => (p.Area, Competencia: p.Competencia!))
            .OrderBy(g => g.Key.Area).ThenBy(g => g.Key.Competencia)
            .Select(g => new PromedioCompetencia(
                g.Key.Area, g.Key.Competencia,
                entregados.Count == 0 ? 0 : Math.Round(entregados.Average(i => PuntajeSaber.Porcentaje(g.Count(p => CalculoSimulacro.Acerto(p, i.Respuestas)), g.Count())), 1)))
            .ToList();

        var porCurso = estudiantes.GroupBy(e => (e.Grado, e.Grupo)).OrderBy(g => g.Key.Grado).ThenBy(g => g.Key.Grupo)
            .Select(g => new PromedioCurso(
                g.Key.Grado, g.Key.Grupo, g.Count(), Math.Round(g.Average(e => e.Porcentaje), 1),
                g.All(e => e.Global is not null) ? (int)Math.Round(g.Average(e => e.Global!.Value)) : null))
            .ToList();

        var porPregunta = preguntas.Select(p =>
        {
            var distribucion = new int[p.Opciones.Count];
            var sinResponder = 0;
            foreach (var i in entregados)
            {
                if (i.Respuestas.TryGetValue(p.Id, out var r) && r >= 0 && r < distribucion.Length)
                {
                    distribucion[r]++;
                }
                else
                {
                    sinResponder++;
                }
            }

            return new AnalisisPregunta(
                p.Id, p.Orden, p.Area, p.Competencia, p.Enunciado, p.Correcta,
                PuntajeSaber.Porcentaje(distribucion[p.Correcta], entregados.Count), distribucion, sinResponder);
        }).ToList();

        var presentados = await repository.ConteoPresentadosAsync(ct);
        return new ResultadosSimulacroResponse(
            SimulacroResponse.Desde(simulacro, ahora, preguntas.Count, presentados.GetValueOrDefault(id)),
            habilitados,
            entregados.Count,
            intentos.Count - entregados.Count,
            estudiantes.Count == 0 ? 0 : Math.Round(estudiantes.Average(e => e.Porcentaje), 1),
            estudiantes.Count > 0 && estudiantes.All(e => e.Global is not null) ? (int)Math.Round(estudiantes.Average(e => e.Global!.Value)) : null,
            porArea, porCompetencia, porCurso, porPregunta, estudiantes);
    }

    private async Task<SimulacroDetalleResponse> DetalleAsync(Simulacro simulacro, CancellationToken ct)
    {
        var preguntas = (await repository.PreguntasAsync(simulacro.Id, ct)).OrderBy(p => p.Orden).ToList();
        var presentados = await repository.ConteoPresentadosAsync(ct);
        return new SimulacroDetalleResponse(
            SimulacroResponse.Desde(simulacro, reloj.GetUtcNow(), preguntas.Count, presentados.GetValueOrDefault(simulacro.Id)),
            [.. preguntas.Select(PreguntaResponse.Desde)]);
    }
}
