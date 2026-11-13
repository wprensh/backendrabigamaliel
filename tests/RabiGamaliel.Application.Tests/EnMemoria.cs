using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Admisiones;
using RabiGamaliel.Application.Asistencia;
using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Application.Comunicaciones;
using RabiGamaliel.Application.Simulacros;
using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Admisiones;
using RabiGamaliel.Domain.Asistencia;
using RabiGamaliel.Domain.Avisos;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Comunicaciones;
using RabiGamaliel.Domain.Simulacros;

namespace RabiGamaliel.Application.Tests;

/// <summary>Repositorios en memoria para probar los servicios sin base de datos.</summary>
internal class MiembrosEnMemoria : IMiembroCampusRepository
{
    public List<MiembroCampus> Lista { get; } = [];

    public List<VinculoAcudiente> Vinculos { get; } = [];

    public Task<MiembroCampus?> ObtenerPorIdAsync(Guid id, CancellationToken ct) => Task.FromResult(Lista.FirstOrDefault(m => m.Id == id));

    public Task<MiembroCampus?> ObtenerPorDocumentoAsync(string documento, CancellationToken ct) =>
        Task.FromResult(Lista.FirstOrDefault(m => m.Documento == documento));

    public Task<List<MiembroCampus>> ObtenerPorDocumentosAsync(IReadOnlyCollection<string> documentos, CancellationToken ct) =>
        Task.FromResult(Lista.Where(m => documentos.Contains(m.Documento)).ToList());

    public Task<PagedResult<MiembroCampus>> ListarAsync(MiembroQuery query, CancellationToken ct) =>
        Task.FromResult(new PagedResult<MiembroCampus>([.. Lista], 1, Lista.Count, Lista.Count));

    public Task<List<MiembroCampus>> ParaGenerarClavesAsync(GenerarClavesRequest filtro, int maximo, CancellationToken ct) =>
        Task.FromResult(Lista.Where(m => m.Activo && (filtro.Ids is null || filtro.Ids.Contains(m.Id))).Take(maximo).ToList());

    public Task<List<MiembroCampus>> BuscarEstudiantesAsync(string? buscar, IReadOnlyCollection<int> grados, int maximo, CancellationToken ct) =>
        Task.FromResult(Lista.Where(m => m.Grado is { } g && grados.Contains(g)).Take(maximo).ToList());

    public Task<List<ConteoPorGrado>> EstudiantesActivosPorGradoAsync(CancellationToken ct) =>
        Task.FromResult(Lista.Where(m => m is { Activo: true, Grado: not null }).GroupBy(m => m.Grado!.Value)
            .Select(g => new ConteoPorGrado(g.Key, g.Count())).ToList());

    public Task<bool> TieneHistorialAsync(Guid id, CancellationToken ct) => Task.FromResult(false);

    public Task<List<MiembroCampus>> ObtenerPorIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        Task.FromResult(Lista.Where(m => ids.Contains(m.Id)).ToList());

    public Task<List<MiembroCampus>> EstudiantesDelCursoAsync(int grado, string? grupo, CancellationToken ct) =>
        Task.FromResult(Lista.Where(m => m is { Activo: true, Rol: RolCampus.Estudiante } && m.Grado == grado && (grupo == null || m.Grupo == grupo))
            .OrderBy(m => m.NombreCompleto).ToList());

    public Task<List<MiembroCampus>> ActivosPorRolAsync(RolCampus rol, CancellationToken ct) =>
        Task.FromResult(Lista.Where(m => m.Activo && m.Rol == rol).ToList());

    public Task<MiembroCampus?> ObtenerPorCodigoCarnetAsync(string codigo, CancellationToken ct) =>
        Task.FromResult(Lista.FirstOrDefault(m => m.CodigoCarnet == codigo));

    public Task<List<VinculoResponse>> VinculosAsync(Guid? acudienteId, Guid? estudianteId, CancellationToken ct) =>
        Task.FromResult(Vinculos
            .Where(v => (acudienteId == null || v.AcudienteId == acudienteId) && (estudianteId == null || v.EstudianteId == estudianteId))
            .Select(v =>
            {
                var a = Lista.Single(m => m.Id == v.AcudienteId);
                var e = Lista.Single(m => m.Id == v.EstudianteId);
                return new VinculoResponse(v.Id, a.Id, a.NombreCompleto, a.Documento, a.Telefono, a.Correo, e.Id, e.NombreCompleto, e.Documento, e.Grado, e.Grupo, v.Parentesco);
            }).ToList());

    public Task<List<VinculoAcudiente>> VinculosDeEstudiantesAsync(IReadOnlyCollection<Guid>? estudianteIds, CancellationToken ct) =>
        Task.FromResult(Vinculos.Where(v => estudianteIds == null || estudianteIds.Contains(v.EstudianteId)).ToList());

    public Task<List<VinculoAcudiente>> VinculosDeAcudienteAsync(Guid acudienteId, CancellationToken ct) =>
        Task.FromResult(Vinculos.Where(v => v.AcudienteId == acudienteId).ToList());

    public Task<VinculoAcudiente?> ObtenerVinculoAsync(Guid id, CancellationToken ct) => Task.FromResult(Vinculos.FirstOrDefault(v => v.Id == id));

    public void AgregarVinculo(VinculoAcudiente vinculo) => Vinculos.Add(vinculo);

    public void EliminarVinculo(VinculoAcudiente vinculo) => Vinculos.Remove(vinculo);

    public void Agregar(MiembroCampus miembro) => Lista.Add(miembro);

    public void Eliminar(MiembroCampus miembro) => Lista.Remove(miembro);

    public MiembroCampus Nuevo(RolCampus rol, string nombre, int? grado = null, string? grupo = null)
    {
        var m = MiembroCampus.Crear(rol, Guid.NewGuid().ToString("N")[..10], nombre, grado, grupo);
        Lista.Add(m);
        return m;
    }
}

internal sealed class AcademicoEnMemoria(AsistenciaEnMemoria? asistencia = null) : IAcademicoRepository
{
    public List<Asignatura> Asignaturas { get; } = [];

    public List<PeriodoAcademico> Periodos { get; } = [];

    public List<AsignacionDocente> Asignaciones { get; } = [];

    public List<Calificacion> Calificaciones { get; } = [];

    public Task<List<Asignatura>> AsignaturasAsync(bool soloActivas, CancellationToken ct) =>
        Task.FromResult(Asignaturas.Where(a => !soloActivas || a.Activa).OrderBy(a => a.Orden).ToList());

    public Task<Asignatura?> AsignaturaAsync(Guid id, CancellationToken ct) => Task.FromResult(Asignaturas.FirstOrDefault(a => a.Id == id));

    public Task<bool> AsignaturaEnUsoAsync(Guid id, CancellationToken ct) => Task.FromResult(Asignaciones.Any(a => a.AsignaturaId == id));

    public void Agregar(Asignatura asignatura) => Asignaturas.Add(asignatura);

    public void Eliminar(Asignatura asignatura) => Asignaturas.Remove(asignatura);

    public Task<List<PeriodoAcademico>> PeriodosAsync(int? anio, CancellationToken ct) =>
        Task.FromResult(Periodos.Where(p => anio == null || p.Anio == anio).OrderBy(p => p.Numero).ToList());

    public Task<PeriodoAcademico?> PeriodoAsync(Guid id, CancellationToken ct) => Task.FromResult(Periodos.FirstOrDefault(p => p.Id == id));

    public Task<bool> ExistePeriodoAsync(int anio, int numero, Guid? excluirId, CancellationToken ct) =>
        Task.FromResult(Periodos.Any(p => p.Anio == anio && p.Numero == numero && p.Id != excluirId));

    public Task<bool> PeriodoConNotasAsync(Guid id, CancellationToken ct) => Task.FromResult(Calificaciones.Any(c => c.PeriodoId == id));

    public void Agregar(PeriodoAcademico periodo) => Periodos.Add(periodo);

    public void Eliminar(PeriodoAcademico periodo) => Periodos.Remove(periodo);

    public Task<List<AsignacionDocente>> AsignacionesAsync(int anio, Guid? docenteId, int? grado, string? grupo, CancellationToken ct) =>
        Task.FromResult(Asignaciones.Where(a => a.Anio == anio && (docenteId == null || a.DocenteId == docenteId)
            && (grado == null || (a.Grado == grado && (grupo == null || a.Grupo == null || a.Grupo == grupo)))).ToList());

    public Task<AsignacionDocente?> AsignacionAsync(Guid id, CancellationToken ct) => Task.FromResult(Asignaciones.FirstOrDefault(a => a.Id == id));

    public Task<bool> ExisteAsignacionAsync(int anio, Guid asignaturaId, int grado, string? grupo, CancellationToken ct) =>
        Task.FromResult(Asignaciones.Any(a => a.Anio == anio && a.AsignaturaId == asignaturaId && a.Grado == grado && a.Grupo == grupo));

    public Task<bool> AsignacionConRegistrosAsync(Guid id, CancellationToken ct) => Task.FromResult(false);

    public void Agregar(AsignacionDocente asignacion) => Asignaciones.Add(asignacion);

    public void Eliminar(AsignacionDocente asignacion) => Asignaciones.Remove(asignacion);

    public List<BloqueHorario> Bloques { get; } = [];

    public Task<List<BloqueHorario>> BloquesAsync(IReadOnlyCollection<Guid> asignacionIds, CancellationToken ct) =>
        Task.FromResult(Bloques.Where(b => asignacionIds.Contains(b.AsignacionId)).ToList());

    public Task<BloqueHorario?> BloqueAsync(Guid id, CancellationToken ct) => Task.FromResult(Bloques.FirstOrDefault(b => b.Id == id));

    public void Agregar(BloqueHorario bloque) => Bloques.Add(bloque);

    public void Eliminar(BloqueHorario bloque) => Bloques.Remove(bloque);

    public Task<List<Calificacion>> CalificacionesAsync(
        IReadOnlyCollection<Guid> estudianteIds, IReadOnlyCollection<Guid> periodoIds, Guid? asignaturaId, CancellationToken ct) =>
        Task.FromResult(Calificaciones.Where(c => estudianteIds.Contains(c.EstudianteId) && periodoIds.Contains(c.PeriodoId)
            && (asignaturaId == null || c.AsignaturaId == asignaturaId)).ToList());

    public void Agregar(Calificacion calificacion) => Calificaciones.Add(calificacion);

    public Task<List<ConteoFallas>> FallasAsync(
        IReadOnlyCollection<Guid> estudianteIds, DateOnly desde, DateOnly hasta, Guid? asignaturaId, CancellationToken ct) =>
        Task.FromResult((asistencia?.Tomas ?? [])
            .Where(t => t.Fecha >= desde && t.Fecha <= hasta && (asignaturaId == null || t.AsignaturaId == asignaturaId))
            .SelectMany(t => t.Novedades.Where(n => n.Estado == EstadoAsistencia.Ausente && estudianteIds.Contains(n.EstudianteId))
                .Select(n => (n.EstudianteId, t.AsignaturaId)))
            .GroupBy(x => x).Select(g => new ConteoFallas(g.Key.EstudianteId, g.Key.AsignaturaId, g.Count())).ToList());

    public Task<List<CursoResponse>> CursosAsync(CancellationToken ct) => Task.FromResult(new List<CursoResponse>());
}

internal sealed class AsistenciaEnMemoria : IAsistenciaRepository
{
    public List<TomaAsistencia> Tomas { get; } = [];

    public Task<TomaAsistencia?> TomaAsync(Guid asignacionId, DateOnly fecha, CancellationToken ct) =>
        Task.FromResult(Tomas.FirstOrDefault(t => t.AsignacionId == asignacionId && t.Fecha == fecha));

    public void Agregar(TomaAsistencia toma) => Tomas.Add(toma);

    public Task<List<NovedadDetalle>> NovedadesDeEstudianteAsync(Guid estudianteId, DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        Task.FromResult(Tomas.Where(t => t.Fecha >= desde && t.Fecha <= hasta)
            .SelectMany(t => t.Novedades.Where(n => n.EstudianteId == estudianteId).Select(n => new NovedadDetalle(t.Fecha, t.AsignaturaId, n.Estado, n.Observacion)))
            .ToList());

    public Task<List<ConteoNovedad>> ConteosAsync(IReadOnlyCollection<Guid> estudianteIds, DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        Task.FromResult(Tomas.SelectMany(t => t.Novedades).Where(n => estudianteIds.Contains(n.EstudianteId))
            .GroupBy(n => (n.EstudianteId, n.Estado)).Select(g => new ConteoNovedad(g.Key.EstudianteId, g.Key.Estado, g.Count())).ToList());

    public Task<int> TomasAsync(int grado, string? grupo, DateOnly desde, DateOnly hasta, CancellationToken ct) => Task.FromResult(Tomas.Count);
}

internal sealed class AvisosEnMemoria : IAvisoRepository
{
    public List<Aviso> Lista { get; } = [];

    public void Agregar(Aviso aviso) => Lista.Add(aviso);

    public Task<List<Aviso>> DeMiembroAsync(Guid miembroId, int maximo, CancellationToken ct) =>
        Task.FromResult(Lista.Where(a => a.DestinatarioId == miembroId).Take(maximo).ToList());

    public Task<int> NoLeidosAsync(Guid miembroId, CancellationToken ct) => Task.FromResult(Lista.Count(a => a.DestinatarioId == miembroId && a.LeidoEn == null));

    public Task<List<Aviso>> NoLeidosParaMarcarAsync(Guid miembroId, IReadOnlyCollection<Guid>? ids, CancellationToken ct) =>
        Task.FromResult(Lista.Where(a => a.DestinatarioId == miembroId && a.LeidoEn == null && (ids == null || ids.Contains(a.Id))).ToList());

    public Task<bool> ExisteDesdeAsync(Guid destinatarioId, Guid estudianteId, TipoAviso tipo, DateTimeOffset desde, CancellationToken ct) =>
        Task.FromResult(Lista.Any(a => a.DestinatarioId == destinatarioId && a.EstudianteId == estudianteId && a.Tipo == tipo));
}

internal sealed class SimulacrosEnMemoria : ISimulacroRepository
{
    public List<Simulacro> Simulacros { get; } = [];

    public List<PreguntaSimulacro> Preguntas { get; } = [];

    public List<IntentoSimulacro> Intentos { get; } = [];

    public Task<List<Simulacro>> ListarAsync(CancellationToken ct) => Task.FromResult(Simulacros.ToList());

    public Task<Simulacro?> ObtenerAsync(Guid id, CancellationToken ct) => Task.FromResult(Simulacros.FirstOrDefault(s => s.Id == id));

    public void Agregar(Simulacro simulacro) => Simulacros.Add(simulacro);

    public void Eliminar(Simulacro simulacro) => Simulacros.Remove(simulacro);

    public Task<List<PreguntaSimulacro>> PreguntasAsync(Guid simulacroId, CancellationToken ct) =>
        Task.FromResult(Preguntas.Where(p => p.SimulacroId == simulacroId).OrderBy(p => p.Orden).ToList());

    public void Agregar(PreguntaSimulacro pregunta) => Preguntas.Add(pregunta);

    public void Eliminar(PreguntaSimulacro pregunta) => Preguntas.Remove(pregunta);

    public Task<Dictionary<Guid, int>> ConteoPreguntasAsync(CancellationToken ct) =>
        Task.FromResult(Preguntas.GroupBy(p => p.SimulacroId).ToDictionary(g => g.Key, g => g.Count()));

    public Task<Dictionary<Guid, int>> ConteoPresentadosAsync(CancellationToken ct) =>
        Task.FromResult(Intentos.Where(i => i.Entregado).GroupBy(i => i.SimulacroId).ToDictionary(g => g.Key, g => g.Count()));

    public Task<bool> TieneIntentosAsync(Guid simulacroId, CancellationToken ct) => Task.FromResult(Intentos.Any(i => i.SimulacroId == simulacroId));

    public Task<List<IntentoSimulacro>> IntentosAsync(Guid simulacroId, CancellationToken ct) =>
        Task.FromResult(Intentos.Where(i => i.SimulacroId == simulacroId).ToList());

    public Task<IntentoSimulacro?> IntentoAsync(Guid simulacroId, Guid estudianteId, CancellationToken ct) =>
        Task.FromResult(Intentos.FirstOrDefault(i => i.SimulacroId == simulacroId && i.EstudianteId == estudianteId));

    public Task<List<IntentoSimulacro>> IntentosDeEstudianteAsync(Guid estudianteId, CancellationToken ct) =>
        Task.FromResult(Intentos.Where(i => i.EstudianteId == estudianteId).ToList());

    public Task<bool> AgregarIntentoAsync(IntentoSimulacro intento, CancellationToken ct)
    {
        if (Intentos.Any(i => i.SimulacroId == intento.SimulacroId && i.EstudianteId == intento.EstudianteId))
        {
            return Task.FromResult(false);
        }

        Intentos.Add(intento);
        return Task.FromResult(true);
    }
}

internal sealed class BancoEnMemoria(SimulacrosEnMemoria simulacros) : IBancoPreguntasRepository
{
    public List<PreguntaBanco> Preguntas { get; } = [];

    public Task<List<PreguntaBanco>> ListarAsync(CancellationToken ct) => Task.FromResult(Preguntas.ToList());

    public Task<List<PreguntaBanco>> ObtenerAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        Task.FromResult(Preguntas.Where(p => ids.Contains(p.Id)).ToList());

    public Task<PreguntaBanco?> ObtenerAsync(Guid id, CancellationToken ct) => Task.FromResult(Preguntas.FirstOrDefault(p => p.Id == id));

    public void Agregar(PreguntaBanco pregunta) => Preguntas.Add(pregunta);

    public void Eliminar(PreguntaBanco pregunta) => Preguntas.Remove(pregunta);

    public Task<Dictionary<Guid, int>> UsosAsync(CancellationToken ct) => Task.FromResult(
        simulacros.Preguntas.Where(p => p.PreguntaBancoId != null).GroupBy(p => p.PreguntaBancoId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(p => p.SimulacroId).Distinct().Count()));
}

internal sealed class AdmisionesEnMemoria : IAdmisionRepository
{
    public List<SolicitudAdmision> Solicitudes { get; } = [];

    public List<DocumentoAdmision> Documentos { get; } = [];

    public Task<SolicitudAdmision?> ObtenerAsync(Guid id, CancellationToken ct) => Task.FromResult(Solicitudes.FirstOrDefault(s => s.Id == id));

    public Task<SolicitudAdmision?> PorRadicadoAsync(string radicado, CancellationToken ct) => Task.FromResult(Solicitudes.FirstOrDefault(s => s.Radicado == radicado));

    public Task<bool> ExisteEnCursoAsync(string documentoAspirante, int anioLectivo, CancellationToken ct) =>
        Task.FromResult(Solicitudes.Any(s => s.AspiranteDocumento == documentoAspirante && s.AnioLectivo == anioLectivo && s.Estado != EstadoAdmision.Rechazada));

    public Task<PagedResult<SolicitudAdmision>> ListarAsync(AdmisionQuery query, CancellationToken ct) =>
        Task.FromResult(new PagedResult<SolicitudAdmision>([.. Solicitudes], 1, 20, Solicitudes.Count));

    public Task<List<DocumentoAdmisionResponse>> DocumentosAsync(Guid solicitudId, CancellationToken ct) =>
        Task.FromResult(Documentos.Where(d => d.SolicitudId == solicitudId)
            .Select(d => new DocumentoAdmisionResponse(d.Id, d.Tipo, d.NombreArchivo, d.TipoContenido, d.Tamano, d.CreadoEn)).ToList());

    public Task<DocumentoAdmision?> DocumentoAsync(Guid solicitudId, Guid documentoId, CancellationToken ct) =>
        Task.FromResult(Documentos.FirstOrDefault(d => d.SolicitudId == solicitudId && d.Id == documentoId));

    public Task<List<ConteoEstadoAdmision>> ConteoPorEstadoAsync(int? anioLectivo, CancellationToken ct) =>
        Task.FromResult(Solicitudes.GroupBy(s => s.Estado).Select(g => new ConteoEstadoAdmision(g.Key, g.Count())).ToList());

    public void Agregar(SolicitudAdmision solicitud) => Solicitudes.Add(solicitud);

    public void Agregar(DocumentoAdmision documento) => Documentos.Add(documento);

    public void Eliminar(SolicitudAdmision solicitud) => Solicitudes.Remove(solicitud);
}

internal sealed class ComunicacionesEnMemoria : IComunicacionesRepository
{
    public List<Circular> Circulares { get; } = [];

    public List<LecturaCircular> Lecturas { get; } = [];

    public List<EventoCalendario> Eventos { get; } = [];

    public Task<List<Circular>> CircularesAsync(bool soloPublicadas, CancellationToken ct) =>
        Task.FromResult(Circulares.Where(c => !soloPublicadas || c.PublicadaEn != null).ToList());

    public Task<Circular?> CircularAsync(Guid id, CancellationToken ct) => Task.FromResult(Circulares.FirstOrDefault(c => c.Id == id));

    public Task<int> SiguienteNumeroAsync(int anio, CancellationToken ct) => Task.FromResult(Circulares.Count(c => c.Anio == anio) + 1);

    public void Agregar(Circular circular) => Circulares.Add(circular);

    public void Eliminar(Circular circular) => Circulares.Remove(circular);

    public Task<List<ConteoLecturas>> ConteoLecturasAsync(CancellationToken ct) =>
        Task.FromResult(Lecturas.GroupBy(l => l.CircularId).Select(g => new ConteoLecturas(g.Key, g.Count(), g.Count(l => l.ConfirmadaEn != null))).ToList());

    public Task<List<LecturaCircular>> LecturasAsync(Guid circularId, CancellationToken ct) => Task.FromResult(Lecturas.Where(l => l.CircularId == circularId).ToList());

    public Task<List<LecturaCircular>> LecturasDeMiembroAsync(Guid miembroId, CancellationToken ct) => Task.FromResult(Lecturas.Where(l => l.MiembroId == miembroId).ToList());

    public void Agregar(LecturaCircular lectura) => Lecturas.Add(lectura);

    public Task<List<EventoCalendario>> EventosAsync(DateOnly desde, DateOnly hasta, bool soloPublicos, CancellationToken ct) =>
        Task.FromResult(Eventos.Where(e => e.Fin >= desde && e.Inicio <= hasta && (!soloPublicos || e.Publico)).ToList());

    public Task<EventoCalendario?> EventoAsync(Guid id, CancellationToken ct) => Task.FromResult(Eventos.FirstOrDefault(e => e.Id == id));

    public void Agregar(EventoCalendario evento) => Eventos.Add(evento);

    public void Eliminar(EventoCalendario evento) => Eventos.Remove(evento);
}

internal sealed class UnidadDeTrabajo : IUnitOfWork
{
    public int Guardados { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Guardados++;
        return Task.FromResult(1);
    }
}

internal sealed class CorreosEnMemoria : IColaCorreos
{
    public List<CorreoSaliente> Enviados { get; } = [];

    public void Encolar(CorreoSaliente correo) => Enviados.Add(correo);
}

internal sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
{
    private DateTimeOffset _ahora = ahora;

    public void Avanzar(TimeSpan tiempo) => _ahora += tiempo;

    public override DateTimeOffset GetUtcNow() => _ahora;
}
