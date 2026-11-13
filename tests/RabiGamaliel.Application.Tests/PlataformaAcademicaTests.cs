using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Admisiones;
using RabiGamaliel.Application.Asistencia;
using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Application.Comunicaciones;
using RabiGamaliel.Application.Simulacros;
using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Admisiones;
using RabiGamaliel.Domain.Asistencia;
using RabiGamaliel.Domain.Avisos;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Certificaciones;
using RabiGamaliel.Domain.Comunicaciones;
using RabiGamaliel.Domain.Simulacros;

namespace RabiGamaliel.Application.Tests;

/// <summary>Notas y boletines, asistencia con avisos, simulacros, pre-matrícula y circulares.</summary>
public sealed class PlataformaAcademicaTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    // Lunes 9 de noviembre de 2026, 10:00 a. m. en Colombia.
    private readonly RelojFijo _reloj = new(new DateTimeOffset(2026, 11, 9, 15, 0, 0, TimeSpan.Zero));
    private readonly MiembrosEnMemoria _miembros = new();
    private readonly AsistenciaEnMemoria _asistencia = new();
    private readonly AcademicoEnMemoria _academico;
    private readonly AvisosEnMemoria _avisos = new();
    private readonly CorreosEnMemoria _correos = new();
    private readonly UnidadDeTrabajo _uow = new();
    private readonly AccesoEstudiantes _acceso;
    private readonly Notificador _notificador;

    private readonly MiembroCampus _docente;
    private readonly MiembroCampus _ana;
    private readonly MiembroCampus _beto;
    private readonly MiembroCampus _mamaAna;
    private readonly Asignatura _matematicas;
    private readonly Asignatura _lenguaje;
    private readonly AsignacionDocente _claseMatematicas;
    private readonly PeriodoAcademico _periodo1;
    private readonly PeriodoAcademico _periodo2;

    public PlataformaAcademicaTests()
    {
        _academico = new AcademicoEnMemoria(_asistencia);
        _acceso = new AccesoEstudiantes(_miembros, _academico, _reloj);
        _notificador = new Notificador(_avisos, _correos);

        _docente = _miembros.Nuevo(RolCampus.Docente, "Profe Carmen");
        _ana = _miembros.Nuevo(RolCampus.Estudiante, "Ana Pérez", 7, "A");
        _beto = _miembros.Nuevo(RolCampus.Estudiante, "Beto Ruiz", 7, "A");
        _mamaAna = _miembros.Nuevo(RolCampus.Acudiente, "Rosa Pérez");
        _mamaAna.ActualizarContacto("rosa@correo.co", "300 123 4567");
        _miembros.AgregarVinculo(VinculoAcudiente.Crear(_mamaAna.Id, _ana.Id, "Madre"));

        _matematicas = Asignatura.Crear("Matemáticas", "Matemáticas", 1);
        _lenguaje = Asignatura.Crear("Lengua Castellana", "Humanidades", 2);
        _academico.Asignaturas.AddRange([_matematicas, _lenguaje]);
        _claseMatematicas = AsignacionDocente.Crear(2026, _docente.Id, _matematicas.Id, 7, "A", 5);
        _academico.Asignaciones.Add(_claseMatematicas);
        _academico.Asignaciones.Add(AsignacionDocente.Crear(2026, _docente.Id, _lenguaje.Id, 7, "A", 4));
        _periodo1 = PeriodoAcademico.Crear(2026, 1, null, new DateOnly(2026, 2, 1), new DateOnly(2026, 4, 30), 50);
        _periodo2 = PeriodoAcademico.Crear(2026, 2, null, new DateOnly(2026, 5, 1), new DateOnly(2026, 11, 30), 50);
        _academico.Periodos.AddRange([_periodo1, _periodo2]);
    }

    private CalificacionesService Calificaciones() => new(_academico, _miembros, _uow, _reloj, new GuardarNotasValidator());

    private BoletinService Boletines() => new(_academico, _miembros, _acceso, _notificador, _reloj);

    private AsistenciaService Asistencia() => new(_asistencia, _academico, _miembros, _acceso, _avisos, _notificador, _uow, _reloj);

    // ---------- Escala y cálculos ----------

    [Theory]
    [InlineData(1.0, Desempeno.Bajo)]
    [InlineData(2.9, Desempeno.Bajo)]
    [InlineData(3.0, Desempeno.Basico)]
    [InlineData(3.9, Desempeno.Basico)]
    [InlineData(4.0, Desempeno.Alto)]
    [InlineData(4.5, Desempeno.Alto)]
    [InlineData(4.6, Desempeno.Superior)]
    [InlineData(5.0, Desempeno.Superior)]
    public void La_escala_sigue_los_desempenos_del_decreto_1290(double nota, Desempeno esperado) =>
        Assert.Equal(esperado, EscalaValorativa.Desempeno((decimal)nota));

    [Fact]
    public void La_nota_acumulada_pondera_por_el_porcentaje_de_cada_periodo() =>
        Assert.Equal(3.8m, CalculoBoletin.Acumulada([(3.0m, 20), (4.0m, 80), (null, 0)]));

    [Fact]
    public void Los_empates_comparten_puesto()
    {
        Guid a = Guid.NewGuid(), b = Guid.NewGuid(), c = Guid.NewGuid(), d = Guid.NewGuid();
        var puestos = CalculoBoletin.Puestos(new Dictionary<Guid, decimal?> { [a] = 4.5m, [b] = 4.0m, [c] = 4.0m, [d] = 3.2m });
        Assert.Equal([1, 2, 2, 4], [puestos[a], puestos[b], puestos[c], puestos[d]]);
    }

    [Fact]
    public void El_puntaje_global_saber_11_pondera_ingles_con_peso_uno()
    {
        var global = PuntajeSaber.GlobalSaber11(new Dictionary<AreaEvaluada, double>
        {
            [AreaEvaluada.LecturaCritica] = 60, [AreaEvaluada.Matematicas] = 60, [AreaEvaluada.SocialesCiudadanas] = 60,
            [AreaEvaluada.CienciasNaturales] = 60, [AreaEvaluada.Ingles] = 100,
        });
        Assert.Equal(315, global); // ((60*3*4) + 100) / 13 * 5
    }

    // ---------- Notas y boletín ----------

    [Fact]
    public async Task El_docente_registra_notas_y_el_acudiente_ve_el_boletin_publicado()
    {
        var guardado = await Calificaciones().GuardarAsync(_docente, _claseMatematicas.Id, _periodo2.Id, new GuardarNotasRequest(
            [new NotaEstudianteRequest(_ana.Id, 4.7m, null, "Excelente razonamiento."), new NotaEstudianteRequest(_beto.Id, 2.5m, 3, null)]), Ct);
        Assert.True(guardado.IsSuccess);

        var sinPublicar = await Boletines().BoletinAsync(_mamaAna, _ana.Id, _periodo2.Id, Ct);
        Assert.False(sinPublicar.IsSuccess);

        _periodo2.Actualizar(null, _periodo2.Inicio, _periodo2.Fin, 50, notasAbiertas: false, publicado: true);
        var boletin = await Boletines().BoletinAsync(_mamaAna, _ana.Id, _periodo2.Id, Ct);

        Assert.True(boletin.IsSuccess);
        var mate = boletin.Value!.Areas.SelectMany(a => a.Asignaturas).Single(a => a.Nombre == "Matemáticas");
        Assert.Equal(4.7m, mate.Nota);
        Assert.Equal(Desempeno.Superior, mate.Desempeno);
        Assert.Equal("Profe Carmen", mate.Docente);
        Assert.Equal(1, boletin.Value.Puesto);
        Assert.Equal(2, boletin.Value.TotalCurso);
    }

    [Fact]
    public async Task Un_acudiente_no_ve_el_boletin_de_otro_estudiante()
    {
        _periodo1.Actualizar(null, _periodo1.Inicio, _periodo1.Fin, 50, false, publicado: true);
        var resultado = await Boletines().BoletinAsync(_mamaAna, _beto.Id, _periodo1.Id, Ct);
        Assert.False(resultado.IsSuccess);
    }

    [Fact]
    public async Task No_se_registran_notas_con_el_periodo_cerrado_ni_en_clases_ajenas()
    {
        _periodo1.Actualizar(null, _periodo1.Inicio, _periodo1.Fin, 50, notasAbiertas: false, publicado: false);
        var cerrado = await Calificaciones().GuardarAsync(_docente, _claseMatematicas.Id, _periodo1.Id,
            new GuardarNotasRequest([new NotaEstudianteRequest(_ana.Id, 4.0m, null, null)]), Ct);
        Assert.Equal("Periodo.NotasCerradas", cerrado.Error!.Codigo);

        var otroDocente = _miembros.Nuevo(RolCampus.Docente, "Otro docente");
        var ajena = await Calificaciones().GuardarAsync(otroDocente, _claseMatematicas.Id, _periodo2.Id,
            new GuardarNotasRequest([new NotaEstudianteRequest(_ana.Id, 4.0m, null, null)]), Ct);
        Assert.False(ajena.IsSuccess);
    }

    // ---------- Asistencia ----------

    [Fact]
    public async Task Una_inasistencia_avisa_al_acudiente_una_sola_vez_por_dia_y_entrega_su_whatsapp()
    {
        var hoy = new DateOnly(2026, 11, 9);
        var registro = await Asistencia().RegistrarAsync(_docente, _claseMatematicas.Id,
            new RegistrarAsistenciaRequest(hoy, [new NovedadRequest(_ana.Id, EstadoAsistencia.Ausente, null)]), Ct);

        Assert.True(registro.IsSuccess);
        Assert.Equal(1, registro.Value!.AvisosEnviados);
        var aviso = Assert.Single(_avisos.Lista);
        Assert.Equal(_mamaAna.Id, aviso.DestinatarioId);
        Assert.Equal(TipoAviso.Asistencia, aviso.Tipo);
        Assert.Single(_correos.Enviados);
        Assert.Equal("3001234567", Assert.Single(Assert.Single(registro.Value.Contactos).Acudientes).Telefono);

        // La misma falla en otra clase del mismo día no repite el aviso.
        var lenguaje = _academico.Asignaciones.Single(a => a.AsignaturaId == _lenguaje.Id);
        var otra = await Asistencia().RegistrarAsync(_docente, lenguaje.Id,
            new RegistrarAsistenciaRequest(hoy, [new NovedadRequest(_ana.Id, EstadoAsistencia.Ausente, null)]), Ct);
        Assert.Equal(0, otra.Value!.AvisosEnviados);

        var resumen = await Asistencia().ResumenAsync(_mamaAna, _ana.Id, Ct);
        Assert.Equal(2, resumen.Value!.Ausencias);
    }

    [Fact]
    public async Task La_asistencia_no_se_registra_en_fechas_futuras_ni_muy_atrasadas()
    {
        var futura = await Asistencia().RegistrarAsync(_docente, _claseMatematicas.Id,
            new RegistrarAsistenciaRequest(new DateOnly(2026, 11, 10), []), Ct);
        var vieja = await Asistencia().RegistrarAsync(_docente, _claseMatematicas.Id,
            new RegistrarAsistenciaRequest(new DateOnly(2026, 10, 1), []), Ct);
        Assert.False(futura.IsSuccess);
        Assert.False(vieja.IsSuccess);
    }

    [Fact]
    public async Task Las_fallas_del_boletin_salen_de_la_asistencia_registrada()
    {
        await Asistencia().RegistrarAsync(_docente, _claseMatematicas.Id,
            new RegistrarAsistenciaRequest(new DateOnly(2026, 11, 9), [new NovedadRequest(_ana.Id, EstadoAsistencia.Ausente, null)]), Ct);
        var boletin = await Boletines().BoletinAsync(_docente, _ana.Id, _periodo2.Id, Ct);
        Assert.Equal(1, boletin.Value!.Areas.SelectMany(a => a.Asignaturas).Single(a => a.Nombre == "Matemáticas").Fallas);
    }

    // ---------- Simulacros ----------

    private (SimulacroService Admin, PresentacionService Campus, SimulacrosEnMemoria Repo) Simulacros()
    {
        var repo = new SimulacrosEnMemoria();
        var admin = new SimulacroService(repo, _miembros, _uow, _reloj, new GuardarSimulacroValidator(), new GuardarPreguntasValidator());
        return (admin, new PresentacionService(repo, admin, _academico, _acceso, _uow, _reloj), repo);
    }

    private static PreguntaRequest Pregunta(AreaEvaluada area, int correcta) =>
        new(null, area, "Competencia", null, "¿Cuál es la respuesta?", null, ["A", "B", "C", "D"], correcta, "Porque sí.");

    [Fact]
    public async Task Un_simulacro_se_presenta_con_guardado_automatico_y_se_califica_por_area()
    {
        var (admin, campus, repo) = Simulacros();
        var ahora = _reloj.GetUtcNow();
        var creado = await admin.CrearAsync(new GuardarSimulacroRequest(
            "Simulacro Saber 7°", null, TipoPrueba.Interna, [7], ahora.AddHours(-1), ahora.AddHours(3), 60, MostrarRespuestas: true), Ct);
        var detalle = await admin.GuardarPreguntasAsync(creado.Value!.Id, new GuardarPreguntasRequest(
            [Pregunta(AreaEvaluada.Matematicas, 0), Pregunta(AreaEvaluada.Matematicas, 1), Pregunta(AreaEvaluada.LecturaCritica, 2)]), Ct);
        var preguntas = detalle.Value!.Preguntas;

        var presentacion = await campus.IniciarAsync(_ana, creado.Value.Id, Ct);
        Assert.True(presentacion.IsSuccess);
        Assert.Equal(3, presentacion.Value!.Preguntas.Count);

        await campus.ResponderAsync(_ana, creado.Value.Id, new ResponderRequest(preguntas[0].Id, 0), Ct); // bien
        await campus.ResponderAsync(_ana, creado.Value.Id, new ResponderRequest(preguntas[1].Id, 3), Ct); // mal
        await campus.ResponderAsync(_ana, creado.Value.Id, new ResponderRequest(preguntas[2].Id, 2), Ct); // bien

        var resultado = await campus.EntregarAsync(_ana, creado.Value.Id, Ct);
        Assert.Equal(2, resultado.Value!.Aciertos);
        Assert.Equal(50, resultado.Value.PorArea.Single(a => a.Area == AreaEvaluada.Matematicas).Porcentaje);
        Assert.Equal(100, resultado.Value.PorArea.Single(a => a.Area == AreaEvaluada.LecturaCritica).Porcentaje);
        Assert.Null(resultado.Value.Revision); // Las respuestas se muestran al cerrar.

        // Beto se queda sin tiempo: queda entregado con lo que respondió.
        await campus.IniciarAsync(_beto, creado.Value.Id, Ct);
        await campus.ResponderAsync(_beto, creado.Value.Id, new ResponderRequest(preguntas[0].Id, 0), Ct);
        _reloj.Avanzar(TimeSpan.FromMinutes(61));
        var tarde = await campus.ResponderAsync(_beto, creado.Value.Id, new ResponderRequest(preguntas[1].Id, 1), Ct);
        Assert.Equal("Simulacro.TiempoAgotado", tarde.Error!.Codigo);

        var resultados = await admin.ResultadosAsync(creado.Value.Id, null, Ct);
        Assert.Equal(2, resultados.Value!.Presentados);
        Assert.Equal(1, resultados.Value.Estudiantes.Single(e => e.EstudianteId == _beto.Id).Aciertos);
        Assert.Equal(100, resultados.Value.PorPregunta[0].PorcentajeAcierto);
        Assert.Single(repo.Intentos, i => i.EstudianteId == _ana.Id);
    }

    [Fact]
    public async Task Con_estudiantes_presentando_no_se_cambia_la_respuesta_correcta()
    {
        var (admin, campus, _) = Simulacros();
        var ahora = _reloj.GetUtcNow();
        var creado = await admin.CrearAsync(new GuardarSimulacroRequest(
            "Simulacro", null, TipoPrueba.Saber11, [7], ahora.AddHours(-1), ahora.AddHours(3), 60, false), Ct);
        var detalle = await admin.GuardarPreguntasAsync(creado.Value!.Id, new GuardarPreguntasRequest([Pregunta(AreaEvaluada.Ingles, 0)]), Ct);
        await campus.IniciarAsync(_ana, creado.Value.Id, Ct);

        var p = detalle.Value!.Preguntas[0];
        var cambio = await admin.GuardarPreguntasAsync(creado.Value.Id, new GuardarPreguntasRequest(
            [new PreguntaRequest(p.Id, p.Area, p.Competencia, null, p.Enunciado, null, p.Opciones, 1, null)]), Ct);
        Assert.Equal("Simulacro.ConIntentos", cambio.Error!.Codigo);

        var correccion = await admin.GuardarPreguntasAsync(creado.Value.Id, new GuardarPreguntasRequest(
            [new PreguntaRequest(p.Id, p.Area, p.Competencia, null, "Enunciado corregido", null, p.Opciones, 0, null)]), Ct);
        Assert.True(correccion.IsSuccess);
    }

    // ---------- Pre-matrícula ----------

    private static RadicarAdmisionRequest Solicitud() => new(
        2027, 6, TipoDocumento.TarjetaIdentidad, "1.043.222.111", "Luisa", "Gómez Díaz", new DateOnly(2015, 3, 10), "Colegio X", "Barrio Olaya",
        "Mutual Ser", "Marta Díaz", "52.111.222", "Madre", "300 555 1234", "marta@correo.co", null, AceptaTratamientoDatos: true);

    [Fact]
    public async Task La_familia_radica_consulta_con_su_documento_y_la_secretaria_matricula()
    {
        var repo = new AdmisionesEnMemoria();
        var servicio = new AdmisionesService(repo, _miembros, _correos, _uow, _reloj, new RadicarAdmisionValidator(_reloj));

        var radicada = await servicio.RadicarAsync(Solicitud(), Ct);
        Assert.True(radicada.IsSuccess);
        Assert.StartsWith("ADM-", radicada.Value!.Radicado, StringComparison.Ordinal);
        Assert.Single(_correos.Enviados);

        Assert.False((await servicio.ConsultarAsync(radicada.Value.Radicado, "999999", Ct)).IsSuccess);
        var estado = await servicio.ConsultarAsync(radicada.Value.Radicado.ToLowerInvariant(), "1043222111", Ct);
        Assert.Equal(EstadoAdmision.Recibida, estado.Value!.Estado);

        var repetida = await servicio.RadicarAsync(Solicitud(), Ct);
        Assert.Equal("Admision.EnCurso", repetida.Error!.Codigo);

        var pdf = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31 };
        var subido = await servicio.SubirDocumentoAsync(
            radicada.Value.Radicado, "1043222111", TipoDocumentoAdmision.DocumentoIdentidad, new ArchivoAdmision("ti.pdf", "application/pdf", pdf), Ct);
        Assert.True(subido.IsSuccess);
        var falso = await servicio.SubirDocumentoAsync(
            radicada.Value.Radicado, "1043222111", TipoDocumentoAdmision.Otro, new ArchivoAdmision("virus.pdf", "application/pdf", [0x4D, 0x5A, 0, 0, 0]), Ct);
        Assert.False(falso.IsSuccess);

        var matricula = await servicio.MatricularAsync(repo.Solicitudes[0].Id, new MatricularRequest("b"), Ct);
        Assert.True(matricula.IsSuccess);
        var estudiante = _miembros.Lista.Single(m => m.Id == matricula.Value!.EstudianteId);
        Assert.Equal((6, "B"), (estudiante.Grado!.Value, estudiante.Grupo!));
        Assert.Contains(_miembros.Vinculos, v => v.EstudianteId == estudiante.Id && v.AcudienteId == matricula.Value!.AcudienteId);
        Assert.Equal(EstadoAdmision.Matriculada, repo.Solicitudes[0].Estado);
    }

    [Fact]
    public async Task Sin_autorizacion_de_datos_no_se_radica()
    {
        var servicio = new AdmisionesService(new AdmisionesEnMemoria(), _miembros, _correos, _uow, _reloj, new RadicarAdmisionValidator(_reloj));
        var resultado = await servicio.RadicarAsync(Solicitud() with { AceptaTratamientoDatos = false }, Ct);
        Assert.False(resultado.IsSuccess);
    }

    // ---------- Circulares ----------

    [Fact]
    public async Task Una_circular_de_septimo_llega_a_los_acudientes_de_septimo_y_cuenta_las_confirmaciones()
    {
        var repo = new ComunicacionesEnMemoria();
        var servicio = new ComunicacionesService(repo, _miembros, _correos, _uow, _reloj, new GuardarCircularValidator(), new GuardarEventoValidator());
        var otroAcudiente = _miembros.Nuevo(RolCampus.Acudiente, "Acudiente de once");
        var deOnce = _miembros.Nuevo(RolCampus.Estudiante, "Carlos", 11, "A");
        _miembros.AgregarVinculo(VinculoAcudiente.Crear(otroAcudiente.Id, deOnce.Id, null));

        var circular = await servicio.CrearCircularAsync(
            new GuardarCircularRequest("Salida pedagógica", "El viernes vamos al Castillo de San Felipe.", PublicoCircular.Acudientes, [7], true, null), Ct);
        Assert.Equal("Circular 001 de 2026", circular.Value!.Codigo);
        Assert.Equal(1, circular.Value.Destinatarios);

        await servicio.PublicarAsync(circular.Value.Id, Ct);
        Assert.Single(_correos.Enviados, c => c.Para == "rosa@correo.co");
        Assert.Single(await servicio.MisCircularesAsync(_mamaAna, Ct));
        Assert.Empty(await servicio.MisCircularesAsync(otroAcudiente, Ct));

        Assert.Single((await servicio.PendientesAsync(circular.Value.Id, Ct)).Value!);
        await servicio.LeerAsync(_mamaAna, circular.Value.Id, confirmar: true, Ct);
        Assert.Empty((await servicio.PendientesAsync(circular.Value.Id, Ct)).Value!);
        Assert.Equal(1, (await servicio.CircularAsync(circular.Value.Id, Ct)).Value!.Confirmadas);
    }
}
