using RabiGamaliel.Application.Aula;
using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Application.Evaluacion;
using RabiGamaliel.Application.Mensajeria;
using RabiGamaliel.Application.Planeacion;
using RabiGamaliel.Application.Reportes;
using RabiGamaliel.Domain.Admisiones;
using RabiGamaliel.Domain.Aula;
using RabiGamaliel.Domain.Avisos;
using RabiGamaliel.Domain.Evaluacion;
using RabiGamaliel.Domain.Mensajeria;
using RabiGamaliel.Domain.Planeacion;
using Microsoft.EntityFrameworkCore;

namespace RabiGamaliel.Infrastructure.Persistence.Repositories;

internal sealed class PushRepository(ApplicationDbContext db) : IPushRepository
{
    public Task<SuscripcionPush?> PorEndpointAsync(string endpoint, CancellationToken ct) =>
        db.SuscripcionesPush.FirstOrDefaultAsync(s => s.Endpoint == endpoint, ct);

    public Task<int> ConteoDeMiembroAsync(Guid miembroId, CancellationToken ct) => db.SuscripcionesPush.CountAsync(s => s.MiembroId == miembroId, ct);

    public void Agregar(SuscripcionPush suscripcion) => db.SuscripcionesPush.Add(suscripcion);

    public void Eliminar(SuscripcionPush suscripcion) => db.SuscripcionesPush.Remove(suscripcion);
}

internal sealed class AulaRepository(ApplicationDbContext db) : IAulaRepository
{
    public Task<List<ActividadAula>> ActividadesAsync(IReadOnlyCollection<Guid> asignacionIds, CancellationToken ct) =>
        db.ActividadesAula.Where(a => asignacionIds.Contains(a.AsignacionId)).ToListAsync(ct);

    public Task<ActividadAula?> ActividadAsync(Guid id, CancellationToken ct) => db.ActividadesAula.FirstOrDefaultAsync(a => a.Id == id, ct);

    public void Agregar(ActividadAula actividad) => db.ActividadesAula.Add(actividad);

    public void Eliminar(ActividadAula actividad) => db.ActividadesAula.Remove(actividad);

    public Task<List<EntregaAula>> EntregasAsync(IReadOnlyCollection<Guid> actividadIds, Guid? estudianteId, CancellationToken ct) =>
        db.EntregasAula.Where(e => actividadIds.Contains(e.ActividadId) && (estudianteId == null || e.EstudianteId == estudianteId)).ToListAsync(ct);

    public void Agregar(EntregaAula entrega) => db.EntregasAula.Add(entrega);

    public Task<List<ArchivoInfo>> ArchivosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct) =>
        db.ArchivosAula.Where(a => ids.Contains(a.Id))
            .Select(a => new ArchivoInfo(a.Id, a.NombreArchivo, a.TipoContenido, a.Tamano, a.SubidoPorId))
            .ToListAsync(ct);

    public Task<ArchivoAula?> ArchivoAsync(Guid id, CancellationToken ct) => db.ArchivosAula.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public void Agregar(ArchivoAula archivo) => db.ArchivosAula.Add(archivo);

    public async Task<(ActividadAula? Actividad, EntregaAula? Entrega)> UsoDeArchivoAsync(Guid archivoId, CancellationToken ct) =>
        (await db.ActividadesAula.AsNoTracking().FirstOrDefaultAsync(a => a.ArchivoId == archivoId, ct),
         await db.EntregasAula.AsNoTracking().FirstOrDefaultAsync(e => e.ArchivoId == archivoId, ct));
}

internal sealed class PlaneacionRepository(ApplicationDbContext db) : IPlaneacionRepository
{
    public Task<List<PlanEstudios>> PlanesAsync(Guid? asignaturaId, int? grado, CancellationToken ct) =>
        db.PlanesEstudio.Where(p => (asignaturaId == null || p.AsignaturaId == asignaturaId) && (grado == null || p.Grado == grado)).ToListAsync(ct);

    public Task<PlanEstudios?> PlanAsync(Guid asignaturaId, int grado, int periodo, CancellationToken ct) =>
        db.PlanesEstudio.FirstOrDefaultAsync(p => p.AsignaturaId == asignaturaId && p.Grado == grado && p.Periodo == periodo, ct);

    public Task<PlanEstudios?> PlanAsync(Guid id, CancellationToken ct) => db.PlanesEstudio.FirstOrDefaultAsync(p => p.Id == id, ct);

    public void Agregar(PlanEstudios plan) => db.PlanesEstudio.Add(plan);

    public void Eliminar(PlanEstudios plan) => db.PlanesEstudio.Remove(plan);

    public Task<List<PlaneacionClase>> PlaneacionesAsync(
        Guid? docenteId, IReadOnlyCollection<Guid>? asignacionIds, EstadoPlaneacion? estado, CancellationToken ct) =>
        db.PlaneacionesClase
            .Where(p => (docenteId == null || p.DocenteId == docenteId)
                        && (asignacionIds == null || asignacionIds.Contains(p.AsignacionId))
                        && (estado == null || p.Estado == estado))
            .OrderByDescending(p => p.Semana)
            .Take(500)
            .ToListAsync(ct);

    public Task<PlaneacionClase?> PlaneacionAsync(Guid id, CancellationToken ct) => db.PlaneacionesClase.FirstOrDefaultAsync(p => p.Id == id, ct);

    public void Agregar(PlaneacionClase planeacion) => db.PlaneacionesClase.Add(planeacion);

    public void Eliminar(PlaneacionClase planeacion) => db.PlaneacionesClase.Remove(planeacion);
}

internal sealed class EvaluacionRepository(ApplicationDbContext db) : IEvaluacionRepository
{
    public Task<List<Recuperacion>> RecuperacionesAsync(
        IReadOnlyCollection<Guid>? estudianteIds, IReadOnlyCollection<Guid>? periodoIds, Guid? asignaturaId, CancellationToken ct) =>
        db.Recuperaciones
            .Where(r => (estudianteIds == null || estudianteIds.Contains(r.EstudianteId))
                        && (periodoIds == null || periodoIds.Contains(r.PeriodoId))
                        && (asignaturaId == null || r.AsignaturaId == asignaturaId))
            .ToListAsync(ct);

    public Task<Recuperacion?> RecuperacionAsync(Guid id, CancellationToken ct) => db.Recuperaciones.FirstOrDefaultAsync(r => r.Id == id, ct);

    public void Agregar(Recuperacion recuperacion) => db.Recuperaciones.Add(recuperacion);

    public Task<List<ActaComision>> ActasAsync(int anio, CancellationToken ct) => db.ActasComision.Where(a => a.Anio == anio).ToListAsync(ct);

    public Task<ActaComision?> ActaAsync(Guid id, CancellationToken ct) => db.ActasComision.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<int> SiguienteNumeroActaAsync(int anio, CancellationToken ct) =>
        (await db.ActasComision.Where(a => a.Anio == anio).MaxAsync(a => (int?)a.Numero, ct) ?? 0) + 1;

    public void Agregar(ActaComision acta) => db.ActasComision.Add(acta);

    public void Eliminar(ActaComision acta) => db.ActasComision.Remove(acta);

    public Task<List<DecisionEstudiante>> DecisionesAsync(IReadOnlyCollection<Guid> actaIds, CancellationToken ct) =>
        db.DecisionesComision.Where(d => actaIds.Contains(d.ActaId)).ToListAsync(ct);

    public void Agregar(DecisionEstudiante decision) => db.DecisionesComision.Add(decision);

    public void Eliminar(DecisionEstudiante decision) => db.DecisionesComision.Remove(decision);
}

internal sealed class ReportesRepository(ApplicationDbContext db) : IReportesRepository
{
    public Task<List<SolicitudAdmision>> AdmisionesMatriculadasAsync(CancellationToken ct) =>
        db.SolicitudesAdmision.AsNoTracking()
            .Where(s => s.EstudianteId != null || s.Estado == EstadoAdmision.Matriculada)
            .ToListAsync(ct);

    public Task<List<DecisionEstudiante>> DecisionesFinalesAsync(int anio, CancellationToken ct) =>
        (from d in db.DecisionesComision.AsNoTracking()
         join a in db.ActasComision on d.ActaId equals a.Id
         where a.Anio == anio && a.PeriodoId == null
         orderby a.Numero
         select d)
        .ToListAsync(ct);
}

internal sealed class MensajeriaRepository(ApplicationDbContext db) : IMensajeriaRepository
{
    public Task<List<Conversacion>> ConversacionesAsync(Guid miembroId, CancellationToken ct) =>
        db.Conversaciones.Where(c => c.DocenteId == miembroId || c.FamiliaId == miembroId)
            .OrderByDescending(c => c.UltimoMensajeEn).Take(200).ToListAsync(ct);

    public Task<Conversacion?> ConversacionAsync(Guid id, CancellationToken ct) => db.Conversaciones.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<Mensaje>> MensajesAsync(Guid conversacionId, CancellationToken ct) =>
        db.Mensajes.AsNoTracking().Where(m => m.ConversacionId == conversacionId).OrderBy(m => m.CreadoEn).ToListAsync(ct);

    public async Task<Dictionary<Guid, string>> UltimosTextosAsync(IReadOnlyCollection<Guid> conversacionIds, CancellationToken ct) =>
        (await db.Mensajes.AsNoTracking()
            .Where(m => conversacionIds.Contains(m.ConversacionId))
            .GroupBy(m => m.ConversacionId)
            .Select(g => g.OrderByDescending(m => m.CreadoEn).Select(m => new { m.ConversacionId, m.Texto }).First())
            .ToListAsync(ct))
        .ToDictionary(x => x.ConversacionId, x => x.Texto.Length > 120 ? x.Texto[..120] : x.Texto);

    public void Agregar(Conversacion conversacion) => db.Conversaciones.Add(conversacion);

    public void Agregar(Mensaje mensaje) => db.Mensajes.Add(mensaje);
}
