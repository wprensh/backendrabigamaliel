using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Tests;

/// <summary>Horario semanal: bloques de la carga docente sin cruces de docente ni de curso.</summary>
public sealed class HorarioTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;
    private static readonly TimeOnly Seis30 = new(6, 30);
    private static readonly TimeOnly Siete30 = new(7, 30);
    private static readonly TimeOnly Ocho30 = new(8, 30);

    private readonly RelojFijo _reloj = new(new DateTimeOffset(2026, 11, 9, 15, 0, 0, TimeSpan.Zero));
    private readonly MiembrosEnMemoria _miembros = new();
    private readonly AcademicoEnMemoria _academico = new();
    private readonly MiembroCampus _juan;
    private readonly MiembroCampus _carmen;
    private readonly AsignacionDocente _informatica6A;
    private readonly AsignacionDocente _informatica6B;
    private readonly AsignacionDocente _matematicas6A;
    private readonly AsignacionDocente _religion6;

    public HorarioTests()
    {
        _juan = _miembros.Nuevo(RolCampus.Docente, "Juan Carlos Prens");
        _carmen = _miembros.Nuevo(RolCampus.Docente, "Profe Carmen");
        var informatica = Asignatura.Crear("Tecnología e Informática", "Tecnología e Informática", 1);
        var matematicas = Asignatura.Crear("Matemáticas", "Matemáticas", 2);
        var religion = Asignatura.Crear("Educación Religiosa", "Religión", 3);
        _academico.Asignaturas.AddRange([informatica, matematicas, religion]);
        _informatica6A = AsignacionDocente.Crear(2026, _juan.Id, informatica.Id, 6, "A", 2);
        _informatica6B = AsignacionDocente.Crear(2026, _juan.Id, informatica.Id, 6, "B", 2);
        _matematicas6A = AsignacionDocente.Crear(2026, _carmen.Id, matematicas.Id, 6, "A", 4);
        _religion6 = AsignacionDocente.Crear(2026, _carmen.Id, religion.Id, 6, null, 1);
        _academico.Asignaciones.AddRange([_informatica6A, _informatica6B, _matematicas6A, _religion6]);
    }

    private HorarioService Servicio() => new(
        _academico, _miembros, new AccesoEstudiantes(_miembros, _academico, _reloj), new UnidadDeTrabajo(), _reloj,
        new GuardarBloqueValidator(), new MoverBloqueValidator());

    private Task<Result<BloqueHorarioResponse>> Ubicar(AsignacionDocente a, int dia, TimeOnly inicio, TimeOnly fin) =>
        Servicio().CrearBloqueAsync(new GuardarBloqueRequest(a.Id, dia, inicio, fin), Ct);

    [Fact]
    public async Task Ubica_clases_y_suma_las_horas_programadas()
    {
        Assert.True((await Ubicar(_informatica6A, 1, Seis30, Siete30)).IsSuccess);
        Assert.True((await Ubicar(_informatica6A, 3, Seis30, Siete30)).IsSuccess);
        Assert.True((await Ubicar(_informatica6B, 1, Siete30, Ocho30)).IsSuccess);

        var horario = await Servicio().HorarioAsync(2026, _juan.Id, null, null, Ct);

        Assert.True(horario.IsSuccess);
        Assert.Equal(3, horario.Value.Bloques.Count);
        Assert.Equal(2, horario.Value.Asignaciones.Single(a => a.Asignacion.Id == _informatica6A.Id).HorasProgramadas);
        Assert.Equal("Juan Carlos Prens", horario.Value.Bloques[0].DocenteNombre);
    }

    [Fact]
    public async Task No_permite_que_el_docente_tenga_dos_clases_a_la_vez()
    {
        await Ubicar(_informatica6A, 1, Seis30, Siete30);

        var cruce = await Ubicar(_informatica6B, 1, new TimeOnly(7, 0), Ocho30);

        Assert.False(cruce.IsSuccess);
        Assert.Equal(ErrorType.Conflicto, cruce.Error.Tipo);
        Assert.Contains("Juan Carlos Prens ya tiene clase", cruce.Error.Mensaje);
    }

    [Fact]
    public async Task No_permite_que_el_curso_tenga_dos_clases_a_la_vez_ni_con_clases_de_todo_el_grado()
    {
        await Ubicar(_informatica6A, 2, Seis30, Siete30);

        var mismoCurso = await Ubicar(_matematicas6A, 2, Seis30, Siete30);
        var todoElGrado = await Ubicar(_religion6, 2, Seis30, Siete30);
        var otroDia = await Ubicar(_matematicas6A, 4, Seis30, Siete30);

        Assert.Contains("6° A ya tiene clase", mismoCurso.Error!.Mensaje);
        Assert.False(todoElGrado.IsSuccess);
        Assert.True(otroDia.IsSuccess);
    }

    [Fact]
    public async Task Clases_seguidas_no_se_cruzan_y_mover_ignora_el_propio_bloque()
    {
        var primera = await Ubicar(_informatica6A, 1, Seis30, Siete30);
        Assert.True((await Ubicar(_informatica6B, 1, Siete30, Ocho30)).IsSuccess);

        var alargada = await Servicio().MoverBloqueAsync(primera.Value!.Id, new MoverBloqueRequest(1, new TimeOnly(6, 0), Siete30), Ct);
        var encima = await Servicio().MoverBloqueAsync(primera.Value.Id, new MoverBloqueRequest(1, Siete30, Ocho30), Ct);

        Assert.True(alargada.IsSuccess);
        Assert.False(encima.IsSuccess);
    }

    [Theory]
    [InlineData(0, 7, 8)]
    [InlineData(7, 7, 8)]
    [InlineData(1, 4, 5)]
    [InlineData(1, 8, 7)]
    public async Task Rechaza_franjas_fuera_de_la_jornada(int dia, int desde, int hasta)
    {
        var resultado = await Ubicar(_informatica6A, dia, new TimeOnly(desde, 0), new TimeOnly(hasta, 0));

        Assert.False(resultado.IsSuccess);
        Assert.Equal(ErrorType.Validacion, resultado.Error.Tipo);
    }

    [Fact]
    public async Task En_el_campus_el_estudiante_ve_el_horario_de_su_curso_y_el_docente_el_suyo()
    {
        var ana = _miembros.Nuevo(RolCampus.Estudiante, "Ana Pérez", 6, "A");
        await Ubicar(_informatica6A, 1, Seis30, Siete30);
        await Ubicar(_informatica6B, 1, Siete30, Ocho30);
        await Ubicar(_religion6, 5, Seis30, Siete30);

        var deAna = await Servicio().MiHorarioAsync(ana, null, Ct);
        var deJuan = await Servicio().MiHorarioAsync(_juan, null, Ct);

        Assert.Equal(2, deAna.Value!.Bloques.Count); // Informática 6° A y Religión de todo el grado.
        Assert.Equal(2, deJuan.Value!.Bloques.Count);
    }
}
