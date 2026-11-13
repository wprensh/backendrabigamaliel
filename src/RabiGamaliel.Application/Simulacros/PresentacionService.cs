using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Simulacros;

namespace RabiGamaliel.Application.Simulacros;

public interface IPresentacionService
{
    Task<IReadOnlyList<SimulacroCampusResponse>> DisponiblesAsync(MiembroCampus actor, CancellationToken ct);

    Task<Result<PresentacionResponse>> IniciarAsync(MiembroCampus estudiante, Guid simulacroId, CancellationToken ct);

    Task<Result<Unit>> ResponderAsync(MiembroCampus estudiante, Guid simulacroId, ResponderRequest request, CancellationToken ct);

    Task<Result<ResultadoIntentoResponse>> EntregarAsync(MiembroCampus estudiante, Guid simulacroId, CancellationToken ct);

    Task<Result<ResultadoIntentoResponse>> ResultadoAsync(MiembroCampus actor, Guid simulacroId, Guid? estudianteId, CancellationToken ct);

    Task<Result<ResultadosSimulacroResponse>> ResultadosDocenteAsync(MiembroCampus docente, Guid simulacroId, CancellationToken ct);
}

/// <summary>Presentación de simulacros en el campus: tiempo límite, guardado automático de cada respuesta y calificación al entregar.</summary>
public sealed class PresentacionService(
    ISimulacroRepository repository,
    ISimulacroService simulacros,
    IAcademicoRepository academico,
    IAccesoEstudiantes acceso,
    IUnitOfWork unitOfWork,
    TimeProvider reloj) : IPresentacionService
{
    private static readonly Error SoloEstudiantes = Error.Conflicto("Simulacro.SoloEstudiantes", "Los simulacros los presentan los estudiantes.");

    private static readonly Error NoEsSuGrado = Error.NoEncontrado("Simulacro.NoDisponible", "Este simulacro no está habilitado para su grado.");

    private static readonly Error TiempoAgotado = Error.Conflicto("Simulacro.TiempoAgotado", "Se acabó el tiempo: su simulacro quedó entregado.");

    public async Task<IReadOnlyList<SimulacroCampusResponse>> DisponiblesAsync(MiembroCampus actor, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        var todos = await repository.ListarAsync(ct);
        var preguntas = await repository.ConteoPreguntasAsync(ct);

        if (actor.Rol == RolCampus.Docente)
        {
            var grados = (await academico.AsignacionesAsync(FechaColombia.AnioLectivo(reloj), actor.Id, null, null, ct))
                .Select(a => a.Grado).ToHashSet();
            var presentados = await repository.ConteoPresentadosAsync(ct);
            return
            [
                .. todos.Where(s => s.Grados.Any(grados.Contains))
                    .Select(s => new SimulacroCampusResponse(
                        s.Id, s.Titulo, s.Tipo, s.Grados, s.Inicio, s.Fin, s.DuracionMinutos, s.Estado(ahora), preguntas.GetValueOrDefault(s.Id),
                        null, null, null, presentados.GetValueOrDefault(s.Id))),
            ];
        }

        var estudiantes = await acceso.EstudiantesPropiosAsync(actor, ct);
        var resultado = new List<SimulacroCampusResponse>();
        foreach (var estudiante in estudiantes)
        {
            var intentos = (await repository.IntentosDeEstudianteAsync(estudiante.Id, ct)).ToDictionary(i => i.SimulacroId);
            foreach (var s in todos.Where(s => s.EsParaGrado(estudiante.Grado) && preguntas.GetValueOrDefault(s.Id) > 0))
            {
                var intento = intentos.GetValueOrDefault(s.Id);
                if (intento is { Entregado: false } && intento.TiempoAgotado(ahora))
                {
                    intento.Entregar(await repository.PreguntasAsync(s.Id, ct), ahora);
                    await unitOfWork.SaveChangesAsync(ct);
                }

                var estado = intento is null ? EstadoIntento.SinIniciar : intento.Entregado ? EstadoIntento.Entregado : EstadoIntento.EnCurso;
                double? porcentaje = intento is { Entregado: true } ? PuntajeSaber.Porcentaje(intento.Aciertos, intento.TotalPreguntas) : null;
                int? global = null;
                if (intento is { Entregado: true } && s.Tipo == TipoPrueba.Saber11)
                {
                    global = CalculoSimulacro.Global(s.Tipo, CalculoSimulacro.PorArea(await repository.PreguntasAsync(s.Id, ct), intento.Respuestas));
                }

                resultado.Add(new SimulacroCampusResponse(
                    s.Id, s.Titulo, s.Tipo, s.Grados, s.Inicio, s.Fin, s.DuracionMinutos, s.Estado(ahora), preguntas.GetValueOrDefault(s.Id),
                    estado, porcentaje, global, null, estudiante.Id, estudiante.NombreCompleto));
            }
        }

        return resultado;
    }

    public async Task<Result<PresentacionResponse>> IniciarAsync(MiembroCampus estudiante, Guid simulacroId, CancellationToken ct)
    {
        if (estudiante.Rol != RolCampus.Estudiante)
        {
            return SoloEstudiantes;
        }

        var simulacro = await repository.ObtenerAsync(simulacroId, ct);
        if (simulacro is null || !simulacro.EsParaGrado(estudiante.Grado))
        {
            return NoEsSuGrado;
        }

        var ahora = reloj.GetUtcNow();
        var preguntas = (await repository.PreguntasAsync(simulacro.Id, ct)).OrderBy(p => p.Orden).ToList();
        var intento = await repository.IntentoAsync(simulacro.Id, estudiante.Id, ct);
        if (intento is null)
        {
            var estado = simulacro.Estado(ahora);
            if (estado != EstadoSimulacro.Abierto)
            {
                return Error.Conflicto(
                    "Simulacro.NoAbierto",
                    estado == EstadoSimulacro.Programado ? "El simulacro todavía no ha empezado." : "El simulacro ya cerró.");
            }

            if (preguntas.Count == 0)
            {
                return Error.Conflicto("Simulacro.SinPreguntas", "El simulacro todavía no tiene preguntas.");
            }

            intento = IntentoSimulacro.Iniciar(simulacro, estudiante.Id, estudiante.Grado!.Value, estudiante.Grupo, ahora);
            if (!await repository.AgregarIntentoAsync(intento, ct))
            {
                intento = (await repository.IntentoAsync(simulacro.Id, estudiante.Id, ct))!;
            }
        }

        if (!intento.Entregado && intento.TiempoAgotado(ahora))
        {
            intento.Entregar(preguntas, ahora);
            await unitOfWork.SaveChangesAsync(ct);
        }

        if (intento.Entregado)
        {
            return Error.Conflicto("Simulacro.YaEntregado", "Usted ya entregó este simulacro. Consulte su resultado.");
        }

        return new PresentacionResponse(
            simulacro.Id, simulacro.Titulo, simulacro.Instrucciones, simulacro.Tipo, intento.LimiteEn, ahora,
            [.. preguntas.Select(p => new PreguntaPresentacion(p.Id, p.Orden, p.Area, p.Competencia, p.Contexto, p.Enunciado, p.ImagenUrl, p.Opciones))],
            intento.Respuestas);
    }

    public async Task<Result<Unit>> ResponderAsync(MiembroCampus estudiante, Guid simulacroId, ResponderRequest request, CancellationToken ct)
    {
        var intento = await repository.IntentoAsync(simulacroId, estudiante.Id, ct);
        if (intento is null)
        {
            return NoEsSuGrado;
        }

        if (intento.Entregado)
        {
            return Error.Conflicto("Simulacro.YaEntregado", "Este simulacro ya fue entregado.");
        }

        var preguntas = await repository.PreguntasAsync(simulacroId, ct);
        var ahora = reloj.GetUtcNow();
        if (intento.TiempoAgotado(ahora))
        {
            intento.Entregar(preguntas, ahora);
            await unitOfWork.SaveChangesAsync(ct);
            return TiempoAgotado;
        }

        var pregunta = preguntas.FirstOrDefault(p => p.Id == request.PreguntaId);
        if (pregunta is null || request.Opcion is < 0 || request.Opcion >= pregunta.Opciones.Count)
        {
            return Error.Campo("Opcion", "Opción no válida.");
        }

        intento.Responder(pregunta.Id, request.Opcion);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Result<ResultadoIntentoResponse>> EntregarAsync(MiembroCampus estudiante, Guid simulacroId, CancellationToken ct)
    {
        var intento = await repository.IntentoAsync(simulacroId, estudiante.Id, ct);
        var simulacro = await repository.ObtenerAsync(simulacroId, ct);
        if (intento is null || simulacro is null)
        {
            return NoEsSuGrado;
        }

        var preguntas = (await repository.PreguntasAsync(simulacroId, ct)).OrderBy(p => p.Orden).ToList();
        intento.Entregar(preguntas, reloj.GetUtcNow());
        await unitOfWork.SaveChangesAsync(ct);
        return Armar(simulacro, preguntas, intento);
    }

    public async Task<Result<ResultadoIntentoResponse>> ResultadoAsync(MiembroCampus actor, Guid simulacroId, Guid? estudianteId, CancellationToken ct)
    {
        var id = estudianteId ?? actor.Id;
        var visible = await acceso.EstudianteVisibleAsync(actor, id, ct);
        if (!visible.IsSuccess)
        {
            return visible.Error;
        }

        var simulacro = await repository.ObtenerAsync(simulacroId, ct);
        var intento = await repository.IntentoAsync(simulacroId, id, ct);
        if (simulacro is null || intento is null)
        {
            return Error.NoEncontrado("Simulacro.SinIntento", "Todavía no hay un resultado para este simulacro.");
        }

        var preguntas = (await repository.PreguntasAsync(simulacroId, ct)).OrderBy(p => p.Orden).ToList();
        var ahora = reloj.GetUtcNow();
        if (!intento.Entregado)
        {
            if (!intento.TiempoAgotado(ahora))
            {
                return Error.Conflicto("Simulacro.EnCurso", "El simulacro todavía está en curso.");
            }

            intento.Entregar(preguntas, ahora);
            await unitOfWork.SaveChangesAsync(ct);
        }

        return Armar(simulacro, preguntas, intento);
    }

    public async Task<Result<ResultadosSimulacroResponse>> ResultadosDocenteAsync(MiembroCampus docente, Guid simulacroId, CancellationToken ct)
    {
        if (docente.Rol != RolCampus.Docente)
        {
            return CalificacionesService.SoloDocentes;
        }

        var cursos = (await academico.AsignacionesAsync(FechaColombia.AnioLectivo(reloj), docente.Id, null, null, ct))
            .Select(a => (a.Grado, a.Grupo)).Distinct().ToList();
        return await simulacros.ResultadosAsync(simulacroId, cursos, ct);
    }

    private ResultadoIntentoResponse Armar(Simulacro simulacro, IReadOnlyList<PreguntaSimulacro> preguntas, IntentoSimulacro intento)
    {
        var porArea = CalculoSimulacro.PorArea(preguntas, intento.Respuestas);
        var cerrado = simulacro.Estado(reloj.GetUtcNow()) == EstadoSimulacro.Cerrado;
        var revisar = simulacro.MostrarRespuestas && cerrado;
        return new ResultadoIntentoResponse(
            simulacro.Id, simulacro.Titulo, simulacro.Tipo, intento.Aciertos, intento.TotalPreguntas,
            PuntajeSaber.Porcentaje(intento.Aciertos, intento.TotalPreguntas), CalculoSimulacro.Global(simulacro.Tipo, porArea), porArea,
            CalculoSimulacro.PorCompetencia(preguntas, intento.Respuestas), intento.EntregadoEn,
            revisar
                ?
                [
                    .. preguntas.Select(p => new RevisionPregunta(
                        p.Id, p.Orden, p.Area, p.Contexto, p.Enunciado, p.ImagenUrl, p.Opciones,
                        intento.Respuestas.TryGetValue(p.Id, out var r) ? r : null, p.Correcta, p.Explicacion)),
                ]
                : null,
            revisar ? null
            : simulacro.MostrarRespuestas ? $"Las respuestas correctas se publican al cerrar el simulacro ({simulacro.Fin.ToOffset(FechaColombia.Desfase):dd/MM/yyyy h:mm tt})."
            : null);
    }
}
