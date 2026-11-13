using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Aula;
using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Application.Evaluacion;
using RabiGamaliel.Application.Indicadores;
using RabiGamaliel.Application.Mensajeria;
using RabiGamaliel.Application.Reportes;
using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Aula;
using RabiGamaliel.Domain.Avisos;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Evaluacion;
using RabiGamaliel.Domain.Mensajeria;
using RabiGamaliel.Domain.Planeacion;

namespace RabiGamaliel.Application.Tests;

/// <summary>Alerta temprana, aula virtual, planeador, recuperaciones, reportes, mensajería y notificaciones push.</summary>
public sealed class MejorasTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    private readonly RelojFijo _reloj = new(new DateTimeOffset(2026, 11, 12, 15, 0, 0, TimeSpan.Zero));
    private readonly MiembrosEnMemoria _miembros = new();
    private readonly AcademicoEnMemoria _academico = new();
    private readonly AvisosEnMemoria _avisos = new();
    private readonly PushEnMemoria _push = new();
    private readonly MiembroCampus _docente;
    private readonly MiembroCampus _otroDocente;
    private readonly MiembroCampus _ana;
    private readonly MiembroCampus _luis;
    private readonly MiembroCampus _madreAna;
    private readonly AsignacionDocente _matematicas6A;
    private readonly PeriodoAcademico _periodo;

    public MejorasTests()
    {
        _docente = _miembros.Nuevo(RolCampus.Docente, "Profe Carmen");
        _otroDocente = _miembros.Nuevo(RolCampus.Docente, "Profe Pedro");
        _ana = _miembros.Nuevo(RolCampus.Estudiante, "Ana Pérez", 6, "A");
        _luis = _miembros.Nuevo(RolCampus.Estudiante, "Luis Gómez", 6, "A");
        _madreAna = _miembros.Nuevo(RolCampus.Acudiente, "Marta Pérez");
        _miembros.Vinculos.Add(VinculoAcudiente.Crear(_madreAna.Id, _ana.Id, "Madre"));
        var matematicas = Asignatura.Crear("Matemáticas", "Matemáticas", 1);
        _academico.Asignaturas.Add(matematicas);
        _matematicas6A = AsignacionDocente.Crear(2026, _docente.Id, matematicas.Id, 6, "A", 4);
        _academico.Asignaciones.Add(_matematicas6A);
        _periodo = PeriodoAcademico.Crear(2026, 4, null, new DateOnly(2026, 8, 31), new DateOnly(2026, 11, 27), 40);
        _academico.Periodos.Add(_periodo);
    }

    private Notificador Notificador() => new(_avisos, new CorreosEnMemoria(), _push);

    private AccesoEstudiantes Acceso() => new(_miembros, _academico, _reloj);

    // ---------- Alerta temprana ----------

    [Fact]
    public void Sin_senales_no_hay_alerta()
    {
        var r = CalculoRiesgo.Evaluar(new DatosRiesgo(0, 2, 0, null, 0, 4.1m, 0, 0, 0, 72));

        Assert.Equal(NivelRiesgo.Ninguno, r.Nivel);
        Assert.Empty(r.Motivos);
    }

    [Fact]
    public void Tres_asignaturas_perdidas_y_fallas_es_riesgo_alto()
    {
        var r = CalculoRiesgo.Evaluar(new DatosRiesgo(6, 30, 2, "Cuarto periodo", 3, 2.7m, 0, 1, 0, 35));

        Assert.Equal(NivelRiesgo.Alto, r.Nivel);
        Assert.Equal(100, r.Puntaje);
        Assert.Contains(r.Motivos, m => m.Contains("puede reprobar el grado", StringComparison.Ordinal));
        Assert.Contains(r.Motivos, m => m.StartsWith("6 inasistencias", StringComparison.Ordinal));
    }

    [Fact]
    public void Una_asignatura_perdida_es_riesgo_bajo_y_con_situacion_tipo_ii_medio()
    {
        Assert.Equal(NivelRiesgo.Bajo, CalculoRiesgo.Evaluar(new DatosRiesgo(0, 0, 0, null, 1, 3.4m, 0, 0, 0, null)).Nivel);
        Assert.Equal(NivelRiesgo.Medio, CalculoRiesgo.Evaluar(new DatosRiesgo(0, 0, 0, null, 1, 3.4m, 0, 1, 0, null)).Nivel);
    }

    // ---------- Recuperaciones ----------

    [Theory]
    [InlineData(2.4, 4.5, 3.0)]
    [InlineData(2.4, 2.8, 2.8)]
    [InlineData(2.4, 2.0, 2.4)]
    public void La_recuperacion_aprobada_deja_la_minima_aprobatoria(double original, double recuperacion, double definitiva) =>
        Assert.Equal((decimal)definitiva, Recuperacion.Definitiva((decimal)original, (decimal)recuperacion));

    [Fact]
    public async Task Docente_asigna_plan_y_al_recuperar_la_nota_del_periodo_queda_en_3()
    {
        var nota = Calificacion.Crear(_ana.Id, _matematicas6A.AsignaturaId, _periodo.Id);
        nota.Registrar(2.4m, 3, "Debe repasar fracciones", "Profe Carmen");
        _academico.Calificaciones.Add(nota);
        var repo = new EvaluacionEnMemoria();
        var servicio = new EvaluacionService(repo, _academico, _miembros, Acceso(), Notificador(), new UnidadDeTrabajo());

        var clase = await servicio.RecuperacionesClaseAsync(_docente, _matematicas6A.Id, _periodo.Id, Ct);
        Assert.True(clase.IsSuccess);
        Assert.Equal(_ana.Id, Assert.Single(clase.Value.Filas).EstudianteId);

        var asignado = await servicio.AsignarPlanesAsync(
            _docente, _matematicas6A.Id, _periodo.Id, new AsignarPlanRequest([_ana.Id], "Taller de fracciones y sustentación", new DateOnly(2026, 11, 20)), Ct);
        Assert.True(asignado.IsSuccess);
        Assert.Contains(_avisos.Lista, a => a.DestinatarioId == _madreAna.Id && a.Tipo == TipoAviso.Recuperacion);

        var noPerdio = await servicio.AsignarPlanesAsync(_docente, _matematicas6A.Id, _periodo.Id, new AsignarPlanRequest([_luis.Id], "Plan", null), Ct);
        Assert.False(noPerdio.IsSuccess);

        var calificada = await servicio.CalificarAsync(_docente, repo.Recuperaciones[0].Id, new CalificarRecuperacionRequest(4.2m, "Sustentó bien"), Ct);
        Assert.True(calificada.IsSuccess);
        Assert.Equal(EstadoRecuperacion.Aprobada, calificada.Value.Estado);
        Assert.Equal(2.4m, calificada.Value.NotaOriginal);
        Assert.Equal(3.0m, nota.Nota);
        Assert.Equal(3, nota.Fallas);

        var ajeno = await servicio.CalificarAsync(_otroDocente, repo.Recuperaciones[0].Id, new CalificarRecuperacionRequest(5m, null), Ct);
        Assert.False(ajeno.IsSuccess);
    }

    // ---------- Aula virtual ----------

    [Fact]
    public async Task Tarea_entregada_y_calificada_pasa_su_promedio_a_la_planilla()
    {
        var aula = new AulaEnMemoria();
        var servicio = new AulaService(aula, _academico, _miembros, Acceso(), Notificador(), new UnidadDeTrabajo(), _reloj);

        var tarea1 = await servicio.CrearAsync(
            _docente, _matematicas6A.Id,
            new GuardarActividadRequest(TipoActividad.Tarea, _periodo.Id, "Taller de fracciones", "Resuelva los ejercicios 1 a 10", null, null, _reloj.GetUtcNow().AddDays(2)), Ct);
        var tarea2 = await servicio.CrearAsync(
            _docente, _matematicas6A.Id,
            new GuardarActividadRequest(TipoActividad.Tarea, _periodo.Id, "Quiz de decimales", null, null, null, _reloj.GetUtcNow().AddDays(5)), Ct);
        Assert.True(tarea1.IsSuccess && tarea2.IsSuccess);
        Assert.Contains(_avisos.Lista, a => a.DestinatarioId == _madreAna.Id && a.Tipo == TipoAviso.Tarea);
        Assert.Contains(_push.Enviados, p => p.MiembroIds.Contains(_ana.Id));

        var sinNada = await servicio.EntregarAsync(_ana, tarea1.Value.Id, new EntregarRequest(null, null, null), Ct);
        Assert.False(sinNada.IsSuccess);
        var entrega = await servicio.EntregarAsync(_ana, tarea1.Value.Id, new EntregarRequest("Mis respuestas", "https://docs.example.com/ana", null), Ct);
        Assert.True(entrega.IsSuccess);
        Assert.NotNull(entrega.Value.MiEntrega);

        var acudiente = await servicio.EntregarAsync(_madreAna, tarea1.Value.Id, new EntregarRequest("Hola", null, null), Ct);
        Assert.False(acudiente.IsSuccess);

        Assert.True((await servicio.CalificarAsync(_docente, tarea1.Value.Id, new CalificarEntregasRequest([new(_ana.Id, 4.0m, "Bien"), new(_luis.Id, 1.0m, "No entregó")]), Ct)).IsSuccess);
        Assert.True((await servicio.CalificarAsync(_docente, tarea2.Value.Id, new CalificarEntregasRequest([new(_ana.Id, 4.5m, null)]), Ct)).IsSuccess);
        Assert.False((await servicio.EntregarAsync(_ana, tarea1.Value.Id, new EntregarRequest("Otra versión", null, null), Ct)).IsSuccess);

        var paso = await servicio.PasarAPlanillaAsync(_docente, _matematicas6A.Id, _periodo.Id, Ct);
        Assert.True(paso.IsSuccess);
        Assert.Equal(2, paso.Value.Actualizadas);
        Assert.Equal(4.3m, _academico.Calificaciones.Single(c => c.EstudianteId == _ana.Id).Nota);
        Assert.Equal(1.0m, _academico.Calificaciones.Single(c => c.EstudianteId == _luis.Id).Nota);

        var vista = await servicio.MiAulaAsync(_madreAna, null, Ct);
        Assert.True(vista.IsSuccess);
        Assert.Equal(2, vista.Value.Actividades.Count);
        Assert.Equal(4.0m, vista.Value.Actividades.Single(a => a.Id == tarea1.Value.Id).MiEntrega?.Nota);

        var ajena = await servicio.ActividadesClaseAsync(_otroDocente, _matematicas6A.Id, Ct);
        Assert.False(ajena.IsSuccess);
    }

    [Fact]
    public async Task Solo_se_aceptan_archivos_con_firma_valida()
    {
        var servicio = new AulaService(new AulaEnMemoria(), _academico, _miembros, Acceso(), Notificador(), new UnidadDeTrabajo(), _reloj);

        var falso = await servicio.SubirArchivoAsync(_ana, new("tarea.pdf", "application/pdf", [1, 2, 3, 4, 5]), Ct);
        var pdf = await servicio.SubirArchivoAsync(_ana, new("tarea.pdf", "application/pdf", "%PDF-1.7 contenido"u8.ToArray()), Ct);

        Assert.False(falso.IsSuccess);
        Assert.True(pdf.IsSuccess);
        Assert.True((await servicio.DescargarAsync(_ana, pdf.Value.Id, Ct)).IsSuccess);
        Assert.False((await servicio.DescargarAsync(_luis, pdf.Value.Id, Ct)).IsSuccess);
    }

    // ---------- Planeador ----------

    [Fact]
    public void La_planeacion_se_ubica_en_el_lunes_y_solo_se_revisa_enviada()
    {
        var p = PlaneacionClase.Crear(_matematicas6A.Id, _docente.Id);
        p.Actualizar(_periodo.Id, new DateOnly(2026, 11, 12), "Fracciones equivalentes", "DBA 2", "Inicio, desarrollo y cierre", null, null);

        Assert.Equal(new DateOnly(2026, 11, 9), p.Semana);
        Assert.Throws<InvalidOperationException>(() => p.Revisar(true, null, "Coordinación", _reloj.GetUtcNow()));

        p.Enviar(_reloj.GetUtcNow());
        Assert.False(p.Editable);
        p.Revisar(false, "Agregue la evaluación", "Coordinación", _reloj.GetUtcNow());
        Assert.Equal(EstadoPlaneacion.Devuelta, p.Estado);
        Assert.True(p.Editable);
    }

    // ---------- Reportes ----------

    [Theory]
    [InlineData("Ana Pérez", "Pérez", null, "Ana", null)]
    [InlineData("Ana Pérez Gómez", "Pérez", "Gómez", "Ana", null)]
    [InlineData("Ana María Pérez Gómez", "Pérez", "Gómez", "Ana", "María")]
    public void Divide_el_nombre_completo_para_el_formato_simat(string nombre, string ap1, string? ap2, string n1, string? n2) =>
        Assert.Equal((ap1, ap2, n1, n2), ReportesService.DividirNombreCompleto(nombre));

    // ---------- Mensajería ----------

    [Fact]
    public async Task Acudiente_escribe_a_un_docente_de_su_estudiante_y_el_docente_lo_ve_sin_leer()
    {
        var repo = new MensajeriaEnMemoria();
        var servicio = new MensajeriaService(repo, _academico, _miembros, Acceso(), Notificador(), new UnidadDeTrabajo(), _reloj);

        var contactos = await servicio.ContactosAsync(_madreAna, Ct);
        Assert.Equal(_docente.Id, Assert.Single(contactos).Id);

        var noPermitido = await servicio.IniciarAsync(_madreAna, new IniciarConversacionRequest(_otroDocente.Id, _ana.Id, "Hola", "Buenas tardes"), Ct);
        Assert.False(noPermitido.IsSuccess);

        var conversacion = await servicio.IniciarAsync(
            _madreAna, new IniciarConversacionRequest(_docente.Id, _ana.Id, "Excusa del martes", "Ana estuvo enferma, adjunto la incapacidad."), Ct);
        Assert.True(conversacion.IsSuccess);
        Assert.Contains(_avisos.Lista, a => a.DestinatarioId == _docente.Id && a.Tipo == TipoAviso.Mensaje);

        var bandeja = await servicio.BandejaAsync(_docente, Ct);
        Assert.Equal(1, bandeja.NoLeidas);

        _reloj.Avanzar(TimeSpan.FromMinutes(5));
        var respuesta = await servicio.ResponderAsync(_docente, conversacion.Value.Conversacion.Id, new EnviarMensajeRequest("Recibido, gracias."), Ct);
        Assert.True(respuesta.IsSuccess);
        Assert.Equal(0, (await servicio.BandejaAsync(_docente, Ct)).NoLeidas);
        Assert.Equal(1, (await servicio.BandejaAsync(_madreAna, Ct)).NoLeidas);

        Assert.False((await servicio.LeerAsync(_luis, conversacion.Value.Conversacion.Id, Ct)).IsSuccess);
    }
}

internal sealed class PushEnMemoria : IColaPush
{
    public List<PushSaliente> Enviados { get; } = [];

    public bool Habilitado => true;

    public string? ClavePublica => "clave";

    public void Encolar(PushSaliente push) => Enviados.Add(push);
}

internal sealed class AulaEnMemoria : IAulaRepository
{
    public List<ActividadAula> Actividades { get; } = [];

    public List<EntregaAula> Entregas { get; } = [];

    public List<ArchivoAula> Archivos { get; } = [];

    public Task<List<ActividadAula>> ActividadesAsync(IReadOnlyCollection<Guid> asignacionIds, CancellationToken ct) =>
        Task.FromResult(Actividades.Where(a => asignacionIds.Contains(a.AsignacionId)).ToList());

    public Task<ActividadAula?> ActividadAsync(Guid id, CancellationToken ct) => Task.FromResult(Actividades.FirstOrDefault(a => a.Id == id));

    public void Agregar(ActividadAula actividad) => Actividades.Add(actividad);

    public void Eliminar(ActividadAula actividad) => Actividades.Remove(actividad);

    public Task<List<EntregaAula>> EntregasAsync(IReadOnlyCollection<Guid> actividadIds, Guid? estudianteId, CancellationToken ct) =>
        Task.FromResult(Entregas.Where(e => actividadIds.Contains(e.ActividadId) && (estudianteId == null || e.EstudianteId == estudianteId)).ToList());

    public void Agregar(EntregaAula entrega) => Entregas.Add(entrega);

    public Task<List<ArchivoInfo>> ArchivosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        Task.FromResult(Archivos.Where(a => ids.Contains(a.Id)).Select(a => new ArchivoInfo(a.Id, a.NombreArchivo, a.TipoContenido, a.Tamano, a.SubidoPorId)).ToList());

    public Task<ArchivoAula?> ArchivoAsync(Guid id, CancellationToken ct) => Task.FromResult(Archivos.FirstOrDefault(a => a.Id == id));

    public void Agregar(ArchivoAula archivo) => Archivos.Add(archivo);

    public Task<(ActividadAula? Actividad, EntregaAula? Entrega)> UsoDeArchivoAsync(Guid archivoId, CancellationToken ct) =>
        Task.FromResult((Actividades.FirstOrDefault(a => a.ArchivoId == archivoId), Entregas.FirstOrDefault(e => e.ArchivoId == archivoId)));
}

internal sealed class EvaluacionEnMemoria : IEvaluacionRepository
{
    public List<Recuperacion> Recuperaciones { get; } = [];

    public List<ActaComision> Actas { get; } = [];

    public List<DecisionEstudiante> Decisiones { get; } = [];

    public Task<List<Recuperacion>> RecuperacionesAsync(
        IReadOnlyCollection<Guid>? estudianteIds, IReadOnlyCollection<Guid>? periodoIds, Guid? asignaturaId, CancellationToken ct) =>
        Task.FromResult(Recuperaciones.Where(r => (estudianteIds == null || estudianteIds.Contains(r.EstudianteId))
            && (periodoIds == null || periodoIds.Contains(r.PeriodoId)) && (asignaturaId == null || r.AsignaturaId == asignaturaId)).ToList());

    public Task<Recuperacion?> RecuperacionAsync(Guid id, CancellationToken ct) => Task.FromResult(Recuperaciones.FirstOrDefault(r => r.Id == id));

    public void Agregar(Recuperacion recuperacion) => Recuperaciones.Add(recuperacion);

    public Task<List<ActaComision>> ActasAsync(int anio, CancellationToken ct) => Task.FromResult(Actas.Where(a => a.Anio == anio).ToList());

    public Task<ActaComision?> ActaAsync(Guid id, CancellationToken ct) => Task.FromResult(Actas.FirstOrDefault(a => a.Id == id));

    public Task<int> SiguienteNumeroActaAsync(int anio, CancellationToken ct) => Task.FromResult(Actas.Count(a => a.Anio == anio) + 1);

    public void Agregar(ActaComision acta) => Actas.Add(acta);

    public void Eliminar(ActaComision acta) => Actas.Remove(acta);

    public Task<List<DecisionEstudiante>> DecisionesAsync(IReadOnlyCollection<Guid> actaIds, CancellationToken ct) =>
        Task.FromResult(Decisiones.Where(d => actaIds.Contains(d.ActaId)).ToList());

    public void Agregar(DecisionEstudiante decision) => Decisiones.Add(decision);

    public void Eliminar(DecisionEstudiante decision) => Decisiones.Remove(decision);
}

internal sealed class MensajeriaEnMemoria : IMensajeriaRepository
{
    public List<Conversacion> Conversaciones { get; } = [];

    public List<Mensaje> Mensajes { get; } = [];

    public Task<List<Conversacion>> ConversacionesAsync(Guid miembroId, CancellationToken ct) =>
        Task.FromResult(Conversaciones.Where(c => c.Participa(miembroId)).ToList());

    public Task<Conversacion?> ConversacionAsync(Guid id, CancellationToken ct) => Task.FromResult(Conversaciones.FirstOrDefault(c => c.Id == id));

    public Task<List<Mensaje>> MensajesAsync(Guid conversacionId, CancellationToken ct) =>
        Task.FromResult(Mensajes.Where(m => m.ConversacionId == conversacionId).ToList());

    public Task<Dictionary<Guid, string>> UltimosTextosAsync(IReadOnlyCollection<Guid> conversacionIds, CancellationToken ct) =>
        Task.FromResult(Mensajes.Where(m => conversacionIds.Contains(m.ConversacionId)).GroupBy(m => m.ConversacionId).ToDictionary(g => g.Key, g => g.Last().Texto));

    public void Agregar(Conversacion conversacion) => Conversaciones.Add(conversacion);

    public void Agregar(Mensaje mensaje) => Mensajes.Add(mensaje);
}
