using RabiGamaliel.Application.Simulacros;
using RabiGamaliel.Domain.Simulacros;

namespace RabiGamaliel.Application.Tests;

/// <summary>Banco de preguntas y armado de simulacros con preguntas del banco.</summary>
public sealed class BancoPreguntasTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private static readonly AreaEvaluada[] AreasSaber11 =
        [AreaEvaluada.LecturaCritica, AreaEvaluada.Matematicas, AreaEvaluada.SocialesCiudadanas, AreaEvaluada.CienciasNaturales, AreaEvaluada.Ingles];

    private readonly RelojFijo _reloj = new(new DateTimeOffset(2026, 11, 9, 15, 0, 0, TimeSpan.Zero));
    private readonly UnidadDeTrabajo _uow = new();
    private readonly SimulacrosEnMemoria _simulacros = new();
    private readonly BancoEnMemoria _banco;
    private readonly SimulacroService _admin;
    private readonly BancoPreguntasService _servicio;

    public BancoPreguntasTests()
    {
        _banco = new BancoEnMemoria(_simulacros);
        _admin = new SimulacroService(_simulacros, new MiembrosEnMemoria(), _uow, _reloj, new GuardarSimulacroValidator(), new GuardarPreguntasValidator());
        _servicio = new BancoPreguntasService(
            _banco, _simulacros, _admin, _uow, new GuardarPreguntaBancoValidator(), new GuardarLoteBancoValidator(), new AgregarDesdeBancoValidator());
    }

    private static GuardarPreguntaBancoRequest Pregunta(AreaEvaluada area, int[]? grados = null, string enunciado = "¿Cuál es la respuesta?") =>
        new(area, "Competencia", null, enunciado, null, ["A", "B", "C", "D"], 1, "Porque sí.", grados ?? [10, 11], DificultadPregunta.Media);

    private async Task<Guid> SimulacroSaber11Async()
    {
        var ahora = _reloj.GetUtcNow();
        var creado = await _admin.CrearAsync(
            new GuardarSimulacroRequest("Prueba Saber 11", null, TipoPrueba.Saber11, [11], ahora.AddDays(1), ahora.AddDays(2), 120, true), Ct);
        return creado.Value!.Id;
    }

    private async Task LlenarBancoAsync(int porArea)
    {
        var lote = AreasSaber11.SelectMany(a => Enumerable.Range(1, porArea).Select(i => Pregunta(a, enunciado: $"{a} {i}"))).ToList();
        Assert.True((await _servicio.CrearLoteAsync(new GuardarLoteBancoRequest(lote), Ct)).IsSuccess);
    }

    [Fact]
    public async Task Al_azar_toma_la_cantidad_pedida_repartida_por_igual_entre_las_areas_de_la_prueba()
    {
        await LlenarBancoAsync(porArea: 8);
        var id = await SimulacroSaber11Async();

        var resultado = await _servicio.AgregarASimulacroAsync(id, new AgregarDesdeBancoRequest(null, 20, null, null), Ct);

        Assert.True(resultado.IsSuccess);
        var preguntas = resultado.Value!.Simulacro.Preguntas;
        Assert.Equal(20, preguntas.Count);
        Assert.All(AreasSaber11, a => Assert.Equal(4, preguntas.Count(p => p.Area == a)));
        Assert.All(preguntas, p => Assert.NotNull(p.PreguntaBancoId));
        Assert.Equal(20, preguntas.Select(p => p.PreguntaBancoId).Distinct().Count());
        Assert.Equal(Enumerable.Range(1, 20), preguntas.Select(p => p.Orden));
        // Agrupadas por área en el orden del ICFES.
        Assert.Equal(AreaEvaluada.LecturaCritica, preguntas[0].Area);
        Assert.Equal(AreaEvaluada.Ingles, preguntas[^1].Area);
    }

    [Fact]
    public async Task No_repite_preguntas_que_el_simulacro_ya_tiene_y_completa_con_otras_areas()
    {
        await LlenarBancoAsync(porArea: 2); // 10 en total
        var id = await SimulacroSaber11Async();
        await _servicio.AgregarASimulacroAsync(id, new AgregarDesdeBancoRequest(null, 6, null, null), Ct);

        var segunda = await _servicio.AgregarASimulacroAsync(id, new AgregarDesdeBancoRequest(null, 20, null, null), Ct);

        Assert.Equal(4, segunda.Value!.Agregadas); // solo quedaban 4 sin usar
        Assert.Equal(10, segunda.Value.Simulacro.Preguntas.Select(p => p.PreguntaBancoId).Distinct().Count());

        var tercera = await _servicio.AgregarASimulacroAsync(id, new AgregarDesdeBancoRequest(null, 5, null, null), Ct);
        Assert.False(tercera.IsSuccess);
    }

    [Fact]
    public async Task Solo_usa_preguntas_del_grado_del_simulacro()
    {
        await _servicio.CrearAsync(Pregunta(AreaEvaluada.Matematicas, [5]), Ct);
        var paraOnce = await _servicio.CrearAsync(Pregunta(AreaEvaluada.Matematicas, [11]), Ct);
        var sinGrado = await _servicio.CrearAsync(Pregunta(AreaEvaluada.Matematicas, []), Ct);
        var id = await SimulacroSaber11Async();

        var resultado = await _servicio.AgregarASimulacroAsync(id, new AgregarDesdeBancoRequest(null, 10, null, null), Ct);

        Assert.Equal(
            new[] { paraOnce.Value!.Id, sinGrado.Value!.Id }.Order(),
            resultado.Value!.Simulacro.Preguntas.Select(p => p.PreguntaBancoId!.Value).Order());
    }

    [Fact]
    public async Task Corregir_el_banco_no_cambia_el_simulacro_ya_armado_y_cuenta_los_usos()
    {
        var creada = await _servicio.CrearAsync(Pregunta(AreaEvaluada.Ingles, enunciado: "Original"), Ct);
        var id = await SimulacroSaber11Async();
        await _servicio.AgregarASimulacroAsync(id, new AgregarDesdeBancoRequest([creada.Value!.Id], null, null, null), Ct);

        await _servicio.ActualizarAsync(creada.Value.Id, Pregunta(AreaEvaluada.Ingles, enunciado: "Corregida"), Ct);

        var detalle = await _admin.ObtenerAsync(id, Ct);
        Assert.Equal("Original", detalle.Value!.Preguntas.Single().Enunciado);
        var banco = await _servicio.ListarAsync(new FiltroBanco(AreaEvaluada.Ingles, 11, null, "corregida"), Ct);
        Assert.Equal(1, banco.Single().Usos);
    }

    [Fact]
    public async Task Rechaza_preguntas_sin_respuesta_correcta_valida()
    {
        var mala = Pregunta(AreaEvaluada.Matematicas) with { Correcta = 7 };
        var resultado = await _servicio.CrearAsync(mala, Ct);
        Assert.False(resultado.IsSuccess);
        Assert.Empty(_banco.Preguntas);
    }
}
