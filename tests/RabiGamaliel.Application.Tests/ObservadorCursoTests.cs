using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Application.Convivencia;
using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Convivencia;

namespace RabiGamaliel.Application.Tests;

/// <summary>Historial del observador por curso en el campus del docente.</summary>
public sealed class ObservadorCursoTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly RelojFijo _reloj = new(new DateTimeOffset(2026, 11, 12, 15, 0, 0, TimeSpan.Zero));
    private readonly MiembrosEnMemoria _miembros = new();
    private readonly AcademicoEnMemoria _academico = new();
    private readonly AnotacionesEnMemoria _anotaciones = new();
    private readonly MiembroCampus _docente;
    private readonly MiembroCampus _otroDocente;
    private readonly MiembroCampus _ana;
    private readonly MiembroCampus _luis;
    private readonly MiembroCampus _sofia7B;
    private readonly AsignacionDocente _tecnologia9;
    private readonly AsignacionDocente _tecnologiaTodo7;

    public ObservadorCursoTests()
    {
        _docente = _miembros.Nuevo(RolCampus.Docente, "Profe Carmen");
        _otroDocente = _miembros.Nuevo(RolCampus.Docente, "Profe Pedro");
        _ana = _miembros.Nuevo(RolCampus.Estudiante, "Ana Pérez", 9, "03");
        _luis = _miembros.Nuevo(RolCampus.Estudiante, "Luis Gómez", 9, "03");
        _sofia7B = _miembros.Nuevo(RolCampus.Estudiante, "Sofía Ruiz", 7, "B");
        var tecnologia = Asignatura.Crear("Tecnología e Informática", "Tecnología e Informática", 1);
        _academico.Asignaturas.Add(tecnologia);
        _tecnologia9 = AsignacionDocente.Crear(2026, _docente.Id, tecnologia.Id, 9, "03", 2);
        _tecnologiaTodo7 = AsignacionDocente.Crear(2026, _docente.Id, tecnologia.Id, 7, null, 2);
        _academico.Asignaciones.Add(_tecnologia9);
        _academico.Asignaciones.Add(_tecnologiaTodo7);
    }

    private ConvivenciaService Servicio() => new(
        _anotaciones, _miembros, _academico, new AccesoEstudiantes(_miembros, _academico, _reloj),
        new Notificador(new AvisosEnMemoria(), new CorreosEnMemoria()), new UnidadDeTrabajo(), _reloj,
        new CrearAnotacionValidator(), new SeguimientoValidator());

    private static CrearAnotacionRequest Anotacion(MiembroCampus estudiante, TipoAnotacion tipo, int dia) =>
        new(estudiante.Id, tipo, new DateOnly(2026, 11, dia), "Hechos concretos observados en clase.", null);

    [Fact]
    public async Task El_historial_trae_las_anotaciones_del_curso_de_la_clase_mas_recientes_primero()
    {
        var servicio = Servicio();
        Assert.True((await servicio.CrearDesdeCampusAsync(_docente, Anotacion(_ana, TipoAnotacion.Positiva, 3), Ct)).IsSuccess);
        Assert.True((await servicio.CrearDesdeCampusAsync(_docente, Anotacion(_luis, TipoAnotacion.SituacionTipoI, 10), Ct)).IsSuccess);
        Assert.True((await servicio.CrearDesdePanelAsync("Coordinación", Anotacion(_ana, TipoAnotacion.SituacionTipoII, 11), Ct)).IsSuccess);
        Assert.True((await servicio.CrearDesdeCampusAsync(_docente, Anotacion(_sofia7B, TipoAnotacion.Positiva, 11), Ct)).IsSuccess);

        var r = await servicio.HistorialCursoAsync(_docente, _tecnologia9.Id, new HistorialCursoQuery(), Ct);

        Assert.True(r.IsSuccess);
        Assert.Equal(3, r.Value.Total);
        Assert.Equal(["Ana Pérez", "Luis Gómez", "Ana Pérez"], r.Value.Items.Select(a => a.EstudianteNombre));
        Assert.Equal("Coordinación", r.Value.Items[0].AutorNombre);
    }

    [Fact]
    public async Task El_historial_filtra_por_tipo()
    {
        var servicio = Servicio();
        await servicio.CrearDesdeCampusAsync(_docente, Anotacion(_ana, TipoAnotacion.Positiva, 3), Ct);
        await servicio.CrearDesdeCampusAsync(_docente, Anotacion(_luis, TipoAnotacion.SituacionTipoI, 10), Ct);

        var r = await servicio.HistorialCursoAsync(_docente, _tecnologia9.Id, new HistorialCursoQuery(Tipo: TipoAnotacion.Positiva), Ct);

        Assert.True(r.IsSuccess);
        Assert.Equal("Ana Pérez", Assert.Single(r.Value.Items).EstudianteNombre);
    }

    [Fact]
    public async Task Una_clase_de_todo_el_grado_incluye_todos_sus_grupos()
    {
        var servicio = Servicio();
        await servicio.CrearDesdeCampusAsync(_docente, Anotacion(_sofia7B, TipoAnotacion.Positiva, 3), Ct);

        var r = await servicio.HistorialCursoAsync(_docente, _tecnologiaTodo7.Id, new HistorialCursoQuery(), Ct);

        Assert.True(r.IsSuccess);
        Assert.Equal("Sofía Ruiz", Assert.Single(r.Value.Items).EstudianteNombre);
    }

    [Fact]
    public async Task Otro_docente_no_ve_el_historial_de_una_clase_ajena()
    {
        var r = await Servicio().HistorialCursoAsync(_otroDocente, _tecnologia9.Id, new HistorialCursoQuery(), Ct);

        Assert.False(r.IsSuccess);
    }

    [Fact]
    public async Task Un_estudiante_no_puede_consultar_el_historial()
    {
        var r = await Servicio().HistorialCursoAsync(_ana, _tecnologia9.Id, new HistorialCursoQuery(), Ct);

        Assert.False(r.IsSuccess);
    }
}

internal sealed class AnotacionesEnMemoria : IConvivenciaRepository
{
    public List<Anotacion> Lista { get; } = [];

    public Task<Anotacion?> ObtenerAsync(Guid id, CancellationToken ct) => Task.FromResult(Lista.FirstOrDefault(a => a.Id == id));

    public Task<List<Anotacion>> DeEstudianteAsync(Guid estudianteId, CancellationToken ct) =>
        Task.FromResult(Lista.Where(a => a.EstudianteId == estudianteId).ToList());

    public Task<PagedResult<Anotacion>> ListarAsync(AnotacionQuery query, CancellationToken ct)
    {
        var consulta = Lista
            .Where(a => query.Tipo is null || a.Tipo == query.Tipo)
            .Where(a => query.Grado is null || a.Grado == query.Grado)
            .Where(a => query.Grupo is null || a.Grupo == query.Grupo)
            .OrderByDescending(a => a.Fecha).ThenByDescending(a => a.CreadoEn)
            .ToList();
        var items = consulta.Skip((query.Pagina - 1) * query.TamanoPagina).Take(query.TamanoPagina).ToList();
        return Task.FromResult(new PagedResult<Anotacion>(items, query.Pagina, query.TamanoPagina, consulta.Count));
    }

    public void Agregar(Anotacion anotacion) => Lista.Add(anotacion);

    public void Agregar(SeguimientoAnotacion seguimiento)
    {
    }
}
