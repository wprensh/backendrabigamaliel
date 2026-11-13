using FluentValidation;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Simulacros;

namespace RabiGamaliel.Application.Simulacros;

public interface IBancoPreguntasService
{
    Task<IReadOnlyList<PreguntaBancoResponse>> ListarAsync(FiltroBanco filtro, CancellationToken ct);

    Task<Result<PreguntaBancoResponse>> CrearAsync(GuardarPreguntaBancoRequest request, CancellationToken ct);

    Task<Result<IReadOnlyList<PreguntaBancoResponse>>> CrearLoteAsync(GuardarLoteBancoRequest request, CancellationToken ct);

    Task<Result<PreguntaBancoResponse>> ActualizarAsync(Guid id, GuardarPreguntaBancoRequest request, CancellationToken ct);

    Task<Result<Unit>> EliminarAsync(Guid id, CancellationToken ct);

    Task<Result<AgregadasDesdeBancoResponse>> AgregarASimulacroAsync(Guid simulacroId, AgregarDesdeBancoRequest request, CancellationToken ct);
}

/// <summary>Banco de preguntas tipo Saber del colegio y armado de simulacros con preguntas del banco.</summary>
public sealed class BancoPreguntasService(
    IBancoPreguntasRepository banco,
    ISimulacroRepository simulacros,
    ISimulacroService simulacroService,
    IUnitOfWork unitOfWork,
    IValidator<GuardarPreguntaBancoRequest> validator,
    IValidator<GuardarLoteBancoRequest> loteValidator,
    IValidator<AgregarDesdeBancoRequest> agregarValidator) : IBancoPreguntasService
{
    private static Error NoEncontrada(Guid id) => Error.NoEncontrado("Banco.NoEncontrada", $"No existe la pregunta '{id}' en el banco.");

    public async Task<IReadOnlyList<PreguntaBancoResponse>> ListarAsync(FiltroBanco filtro, CancellationToken ct)
    {
        var texto = filtro.Texto?.Trim();
        var usos = await banco.UsosAsync(ct);
        return
        [
            .. (await banco.ListarAsync(ct))
                .Where(p => filtro.Area is null || p.Area == filtro.Area)
                .Where(p => filtro.Grado is null || p.SirveParaGrado(filtro.Grado))
                .Where(p => filtro.Dificultad is null || p.Dificultad == filtro.Dificultad)
                .Where(p => string.IsNullOrEmpty(texto)
                    || p.Enunciado.Contains(texto, StringComparison.CurrentCultureIgnoreCase)
                    || (p.Contexto?.Contains(texto, StringComparison.CurrentCultureIgnoreCase) ?? false)
                    || (p.Competencia?.Contains(texto, StringComparison.CurrentCultureIgnoreCase) ?? false))
                .OrderBy(p => p.Area).ThenBy(p => p.CreadoEn)
                .Select(p => PreguntaBancoResponse.Desde(p, usos.GetValueOrDefault(p.Id))),
        ];
    }

    public async Task<Result<PreguntaBancoResponse>> CrearAsync(GuardarPreguntaBancoRequest request, CancellationToken ct)
    {
        if (await validator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var pregunta = Nueva(request);
        banco.Agregar(pregunta);
        await unitOfWork.SaveChangesAsync(ct);
        return PreguntaBancoResponse.Desde(pregunta, 0);
    }

    public async Task<Result<IReadOnlyList<PreguntaBancoResponse>>> CrearLoteAsync(GuardarLoteBancoRequest request, CancellationToken ct)
    {
        if (await loteValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var nuevas = request.Preguntas.Select(Nueva).ToList();
        nuevas.ForEach(banco.Agregar);
        await unitOfWork.SaveChangesAsync(ct);
        return nuevas.Select(p => PreguntaBancoResponse.Desde(p, 0)).ToList();
    }

    public async Task<Result<PreguntaBancoResponse>> ActualizarAsync(Guid id, GuardarPreguntaBancoRequest request, CancellationToken ct)
    {
        if (await validator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var pregunta = await banco.ObtenerAsync(id, ct);
        if (pregunta is null)
        {
            return NoEncontrada(id);
        }

        // Los simulacros ya armados conservan su copia: corregir el banco no cambia pruebas presentadas.
        pregunta.Actualizar(
            request.Area, request.Competencia, request.Contexto, request.Enunciado, request.ImagenUrl, request.Opciones, request.Correcta,
            request.Explicacion, request.Grados ?? [], request.Dificultad);
        await unitOfWork.SaveChangesAsync(ct);
        var usos = await banco.UsosAsync(ct);
        return PreguntaBancoResponse.Desde(pregunta, usos.GetValueOrDefault(id));
    }

    public async Task<Result<Unit>> EliminarAsync(Guid id, CancellationToken ct)
    {
        var pregunta = await banco.ObtenerAsync(id, ct);
        if (pregunta is null)
        {
            return NoEncontrada(id);
        }

        banco.Eliminar(pregunta);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Result<AgregadasDesdeBancoResponse>> AgregarASimulacroAsync(
        Guid simulacroId, AgregarDesdeBancoRequest request, CancellationToken ct)
    {
        if (await agregarValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var simulacro = await simulacros.ObtenerAsync(simulacroId, ct);
        if (simulacro is null)
        {
            return SimulacroService.NoEncontrado(simulacroId);
        }

        if (await simulacros.TieneIntentosAsync(simulacroId, ct))
        {
            return Error.Conflicto("Simulacro.ConIntentos", "Ya hay estudiantes que lo presentaron: no se pueden agregar preguntas.");
        }

        var actuales = await simulacros.PreguntasAsync(simulacroId, ct);
        var yaIncluidas = actuales.Where(p => p.PreguntaBancoId is not null).Select(p => p.PreguntaBancoId!.Value).ToHashSet();

        List<PreguntaBanco> elegidas;
        int solicitadas;
        if (request.PreguntaIds is { Count: > 0 } ids)
        {
            solicitadas = ids.Count;
            var encontradas = (await banco.ObtenerAsync([.. ids.Distinct()], ct)).ToDictionary(p => p.Id);
            elegidas = [.. ids.Distinct().Where(id => !yaIncluidas.Contains(id) && encontradas.ContainsKey(id)).Select(id => encontradas[id])];
        }
        else
        {
            solicitadas = request.Cantidad!.Value;
            var areas = request.Areas is { Count: > 0 } a ? a.Distinct().ToList() : [.. AreasPrueba.De(simulacro.Tipo)];
            var candidatas = (await banco.ListarAsync(ct))
                .Where(p => !yaIncluidas.Contains(p.Id) && areas.Contains(p.Area))
                .Where(p => request.Dificultad is null || p.Dificultad == request.Dificultad)
                .Where(p => p.Grados.Count == 0 || p.Grados.Intersect(simulacro.Grados).Any())
                .ToList();
            elegidas = AlAzar(candidatas, areas, solicitadas, Random.Shared);
        }

        if (elegidas.Count == 0)
        {
            return Error.Conflicto(
                "Banco.SinPreguntas",
                "El banco no tiene preguntas nuevas para este simulacro con esos filtros (las que ya están en el simulacro no se repiten).");
        }

        var orden = actuales.Count == 0 ? 0 : actuales.Max(p => p.Orden);
        foreach (var pregunta in elegidas)
        {
            simulacros.Agregar(pregunta.CopiarA(simulacroId, ++orden));
        }

        await unitOfWork.SaveChangesAsync(ct);
        var detalle = await simulacroService.ObtenerAsync(simulacroId, ct);
        return detalle.IsSuccess ? new AgregadasDesdeBancoResponse(elegidas.Count, solicitadas, detalle.Value!) : detalle.Error!;
    }

    /// <summary>
    /// Toma <paramref name="cantidad"/> preguntas repartidas por igual entre las áreas (el sobrante, a las primeras);
    /// si a un área le faltan, completa con las de las demás. Quedan agrupadas por área, en el orden de la prueba.
    /// </summary>
    public static List<PreguntaBanco> AlAzar(IReadOnlyList<PreguntaBanco> candidatas, IReadOnlyList<AreaEvaluada> areas, int cantidad, Random azar)
    {
        var porArea = areas.ToDictionary(a => a, a => candidatas.Where(p => p.Area == a).OrderBy(_ => azar.Next()).ToList());
        var elegidas = new List<PreguntaBanco>();
        for (var i = 0; i < areas.Count; i++)
        {
            var cupo = cantidad / areas.Count + (i < cantidad % areas.Count ? 1 : 0);
            elegidas.AddRange(porArea[areas[i]].Take(cupo));
        }

        var faltan = cantidad - elegidas.Count;
        if (faltan > 0)
        {
            var tomadas = elegidas.ToHashSet();
            elegidas.AddRange(porArea.Values.SelectMany(l => l).Where(p => !tomadas.Contains(p)).OrderBy(_ => azar.Next()).Take(faltan));
        }

        var posicion = areas.Select((a, i) => (a, i)).ToDictionary(x => x.a, x => x.i);
        return [.. elegidas.OrderBy(p => posicion[p.Area])];
    }

    private static PreguntaBanco Nueva(GuardarPreguntaBancoRequest r) => PreguntaBanco.Crear(
        r.Area, r.Competencia, r.Contexto, r.Enunciado, r.ImagenUrl, r.Opciones, r.Correcta, r.Explicacion, r.Grados ?? [], r.Dificultad);
}
