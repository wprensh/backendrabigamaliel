using RabiGamaliel.Application.Indicadores;
using RabiGamaliel.Domain.Asistencia;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Convivencia;
using Microsoft.EntityFrameworkCore;

namespace RabiGamaliel.Infrastructure.Persistence.Repositories;

internal sealed class IndicadoresRepository(ApplicationDbContext db) : IIndicadoresRepository
{
    public Task<List<ResumenToma>> ResumenTomasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        db.TomasAsistencia.Where(t => t.Fecha >= desde && t.Fecha <= hasta)
            .Select(t => new ResumenToma(
                t.Grado, t.Fecha, t.TotalEstudiantes,
                t.Novedades.Count(n => n.Estado == EstadoAsistencia.Ausente),
                t.Novedades.Count(n => n.Estado == EstadoAsistencia.Tarde)))
            .ToListAsync(ct);

    public Task<List<ConteoEstudiante>> MasAusenciasAsync(DateOnly desde, DateOnly hasta, int maximo, CancellationToken ct) =>
        (from n in db.NovedadesAsistencia
         join t in db.TomasAsistencia on n.TomaId equals t.Id
         where n.Estado == EstadoAsistencia.Ausente && t.Fecha >= desde && t.Fecha <= hasta
         group n by n.EstudianteId into g
         orderby g.Count() descending
         select new ConteoEstudiante(g.Key, g.Count()))
        .Take(maximo)
        .ToListAsync(ct);

    public Task<List<NotaRegistrada>> NotasPeriodoAsync(Guid periodoId, CancellationToken ct) =>
        db.Calificaciones.Where(c => c.PeriodoId == periodoId && c.Nota != null)
            .Select(c => new NotaRegistrada(c.EstudianteId, c.AsignaturaId, c.Nota!.Value))
            .ToListAsync(ct);

    public Task<List<Anotacion>> SituacionesAbiertasAsync(CancellationToken ct) =>
        db.Anotaciones.AsNoTracking()
            .Where(a => a.Estado != EstadoCaso.Cerrado
                        && (a.Tipo == TipoAnotacion.SituacionTipoI || a.Tipo == TipoAnotacion.SituacionTipoII || a.Tipo == TipoAnotacion.SituacionTipoIII))
            .ToListAsync(ct);

    public Task<int> AnotacionesDesdeAsync(DateOnly desde, CancellationToken ct) => db.Anotaciones.CountAsync(a => a.Fecha >= desde, ct);

    public Task<int> EstudiantesConAcudienteAsync(CancellationToken ct) =>
        db.MiembrosCampus.CountAsync(
            m => m.Activo && m.Rol == RolCampus.Estudiante && db.VinculosAcudiente.Any(v => v.EstudianteId == m.Id), ct);

    public Task<List<SimulacroEntregado>> SimulacrosEntregadosAsync(DateTimeOffset desde, CancellationToken ct) =>
        db.IntentosSimulacro.Where(i => i.EntregadoEn != null && i.EntregadoEn >= desde)
            .Select(i => new SimulacroEntregado(i.EstudianteId, i.Aciertos, i.TotalPreguntas, i.EntregadoEn!.Value))
            .ToListAsync(ct);
}
