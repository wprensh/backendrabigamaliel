using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Admisiones;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Application.Comunicaciones;
using RabiGamaliel.Application.Convivencia;
using RabiGamaliel.Application.Simulacros;
using RabiGamaliel.Domain.Admisiones;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Convivencia;
using RabiGamaliel.Domain.Simulacros;

namespace RabiGamaliel.Application.Indicadores;

public sealed record ComunidadIndicadores(int Estudiantes, int Docentes, int Acudientes, int EstudiantesConAcudiente, IReadOnlyList<ConteoPorGrado> PorGrado);

public sealed record PorcentajeGrado(int Grado, double Porcentaje);

public sealed record EstudianteFallas(Guid EstudianteId, string NombreCompleto, string Curso, int Ausencias);

public sealed record AsistenciaIndicadores(
    int ClasesHoy, int AusenciasHoy, int TardesHoy, double? PorcentajeHoy, double? Porcentaje30Dias, IReadOnlyList<PorcentajeGrado> PorGrado,
    IReadOnlyList<EstudianteFallas> MasAusencias);

public sealed record PromedioGrado(int Grado, decimal Promedio);

public sealed record PromedioAreaAcademica(string Area, decimal Promedio);

public sealed record AcademicoIndicadores(
    PeriodoResponse? Periodo, decimal? PromedioGeneral, IReadOnlyList<PromedioGrado> PorGrado, IReadOnlyList<PromedioAreaAcademica> PorArea,
    int EstudiantesConPerdidas, double? PorcentajeAprobacion, int NotasRegistradas);

public sealed record CasoConvivencia(Guid Id, Guid EstudianteId, string Estudiante, string Curso, TipoAnotacion Tipo, DateOnly Fecha, EstadoCaso Estado);

public sealed record ConvivenciaIndicadores(int CasosAbiertos, IReadOnlyList<ConteoTipo> AbiertosPorTipo, int AnotacionesUltimos30Dias, IReadOnlyList<CasoConvivencia> CasosGraves);

public sealed record SimulacroIndicador(Guid Id, string Titulo, TipoPrueba Tipo, int Presentados, int Habilitados, double PromedioPorcentaje, int? PromedioGlobal, IReadOnlyList<PromedioArea> PorArea);

public sealed record AdmisionesIndicadores(int AnioLectivo, IReadOnlyList<ConteoEstadoAdmision> PorEstado, int PorRevisar);

public sealed record CircularIndicador(Guid Id, string Codigo, string Titulo, int Destinatarios, int Leidas, int Confirmadas, bool RequiereConfirmacion);

public sealed record TableroResponse(
    DateOnly Fecha, ComunidadIndicadores Comunidad, AsistenciaIndicadores Asistencia, AcademicoIndicadores Academico,
    ConvivenciaIndicadores Convivencia, SimulacroIndicador? UltimoSimulacro, AdmisionesIndicadores Admisiones, CircularIndicador? UltimaCircular);

public sealed record ResumenToma(int Grado, DateOnly Fecha, int TotalEstudiantes, int Ausentes, int Tardes);

public sealed record ConteoEstudiante(Guid EstudianteId, int Total);

public sealed record NotaRegistrada(Guid EstudianteId, Guid AsignaturaId, decimal Nota);

public interface IIndicadoresRepository
{
    Task<List<ResumenToma>> ResumenTomasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);

    Task<List<ConteoEstudiante>> MasAusenciasAsync(DateOnly desde, DateOnly hasta, int maximo, CancellationToken ct);

    Task<List<NotaRegistrada>> NotasPeriodoAsync(Guid periodoId, CancellationToken ct);

    Task<List<Anotacion>> SituacionesAbiertasAsync(CancellationToken ct);

    Task<int> AnotacionesDesdeAsync(DateOnly desde, CancellationToken ct);

    Task<int> EstudiantesConAcudienteAsync(CancellationToken ct);

    /// <summary>Simulacros entregados desde una fecha (para la alerta temprana).</summary>
    Task<List<SimulacroEntregado>> SimulacrosEntregadosAsync(DateTimeOffset desde, CancellationToken ct);
}

public interface IIndicadoresService
{
    Task<TableroResponse> TableroAsync(CancellationToken ct);
}

/// <summary>Tablero del rector: matrícula, asistencia, rendimiento, convivencia, simulacros, admisiones y circulares en una vista.</summary>
public sealed class IndicadoresService(
    IIndicadoresRepository repository,
    IMiembroCampusRepository miembros,
    IAcademicoRepository academico,
    ISimulacroRepository simulacros,
    ISimulacroService simulacroService,
    IAdmisionRepository admisiones,
    IComunicacionesService comunicaciones,
    TimeProvider reloj) : IIndicadoresService
{
    public async Task<TableroResponse> TableroAsync(CancellationToken ct)
    {
        var hoy = FechaColombia.Hoy(reloj);
        var estudiantes = await miembros.ActivosPorRolAsync(RolCampus.Estudiante, ct);
        var porId = estudiantes.ToDictionary(e => e.Id);

        var comunidad = new ComunidadIndicadores(
            estudiantes.Count,
            (await miembros.ActivosPorRolAsync(RolCampus.Docente, ct)).Count,
            (await miembros.ActivosPorRolAsync(RolCampus.Acudiente, ct)).Count,
            await repository.EstudiantesConAcudienteAsync(ct),
            [.. estudiantes.GroupBy(e => e.Grado!.Value).OrderBy(g => g.Key).Select(g => new ConteoPorGrado(g.Key, g.Count()))]);

        return new TableroResponse(
            hoy, comunidad, await AsistenciaAsync(hoy, porId, ct), await AcademicoAsync(hoy, porId, ct), await ConvivenciaAsync(hoy, ct),
            await SimulacroAsync(ct), await AdmisionesAsync(hoy, ct), await CircularAsync(ct));
    }

    private async Task<AsistenciaIndicadores> AsistenciaAsync(DateOnly hoy, Dictionary<Guid, MiembroCampus> estudiantes, CancellationToken ct)
    {
        var tomas = await repository.ResumenTomasAsync(hoy.AddDays(-30), hoy, ct);
        var deHoy = tomas.Where(t => t.Fecha == hoy).ToList();
        var anio = new DateOnly(hoy.Year, 1, 1);
        var masAusencias = await repository.MasAusenciasAsync(anio, hoy, 8, ct);
        return new AsistenciaIndicadores(
            deHoy.Count, deHoy.Sum(t => t.Ausentes), deHoy.Sum(t => t.Tardes), Porcentaje(deHoy), Porcentaje(tomas),
            [.. tomas.GroupBy(t => t.Grado).OrderBy(g => g.Key).Select(g => new PorcentajeGrado(g.Key, Porcentaje([.. g]) ?? 100))],
            [
                .. masAusencias.Where(m => estudiantes.ContainsKey(m.EstudianteId)).Select(m =>
                {
                    var e = estudiantes[m.EstudianteId];
                    return new EstudianteFallas(e.Id, e.NombreCompleto, $"{GradosEscolares.Nombre(e.Grado!.Value)} {e.Grupo}".Trim(), m.Total);
                }),
            ]);
    }

    private static double? Porcentaje(IReadOnlyCollection<ResumenToma> tomas)
    {
        var total = tomas.Sum(t => t.TotalEstudiantes);
        return total == 0 ? null : Math.Round(100 - (tomas.Sum(t => t.Ausentes) * 100.0 / total), 1);
    }

    private async Task<AcademicoIndicadores> AcademicoAsync(DateOnly hoy, Dictionary<Guid, MiembroCampus> estudiantes, CancellationToken ct)
    {
        var periodos = (await academico.PeriodosAsync(hoy.Year, ct)).OrderByDescending(p => p.Numero).ToList();
        foreach (var periodo in periodos)
        {
            var notas = (await repository.NotasPeriodoAsync(periodo.Id, ct)).Where(n => estudiantes.ContainsKey(n.EstudianteId)).ToList();
            if (notas.Count == 0)
            {
                continue;
            }

            var asignaturas = (await academico.AsignaturasAsync(soloActivas: false, ct)).ToDictionary(a => a.Id);
            var porEstudiante = notas.GroupBy(n => n.EstudianteId).ToList();
            return new AcademicoIndicadores(
                PeriodoResponse.Desde(periodo),
                Math.Round(notas.Average(n => n.Nota), 2),
                [.. notas.GroupBy(n => estudiantes[n.EstudianteId].Grado!.Value).OrderBy(g => g.Key).Select(g => new PromedioGrado(g.Key, Math.Round(g.Average(n => n.Nota), 2)))],
                [
                    .. notas.Where(n => asignaturas.ContainsKey(n.AsignaturaId)).GroupBy(n => asignaturas[n.AsignaturaId].Area)
                        .Select(g => new PromedioAreaAcademica(g.Key, Math.Round(g.Average(n => n.Nota), 2))).OrderBy(a => a.Promedio),
                ],
                porEstudiante.Count(g => g.Any(n => CalculoBoletin.Perdida(n.Nota))),
                Math.Round(porEstudiante.Count(g => g.All(n => !CalculoBoletin.Perdida(n.Nota))) * 100.0 / porEstudiante.Count, 1),
                notas.Count);
        }

        return new AcademicoIndicadores(null, null, [], [], 0, null, 0);
    }

    private async Task<ConvivenciaIndicadores> ConvivenciaAsync(DateOnly hoy, CancellationToken ct)
    {
        var abiertas = await repository.SituacionesAbiertasAsync(ct);
        var graves = abiertas.Where(a => a.Tipo is TipoAnotacion.SituacionTipoII or TipoAnotacion.SituacionTipoIII)
            .OrderByDescending(a => a.Fecha).Take(8).ToList();
        var nombres = (await miembros.ObtenerPorIdsAsync([.. graves.Select(g => g.EstudianteId)], ct)).ToDictionary(m => m.Id, m => m.NombreCompleto);
        return new ConvivenciaIndicadores(
            abiertas.Count,
            [.. abiertas.GroupBy(a => a.Tipo).OrderBy(g => g.Key).Select(g => new ConteoTipo(g.Key, g.Count()))],
            await repository.AnotacionesDesdeAsync(hoy.AddDays(-30), ct),
            [
                .. graves.Select(a => new CasoConvivencia(
                    a.Id, a.EstudianteId, nombres.GetValueOrDefault(a.EstudianteId, "(estudiante)"),
                    $"{(a.Grado is { } g ? GradosEscolares.Nombre(g) : string.Empty)} {a.Grupo}".Trim(), a.Tipo, a.Fecha, a.Estado)),
            ]);
    }

    private async Task<SimulacroIndicador?> SimulacroAsync(CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        var presentados = await simulacros.ConteoPresentadosAsync(ct);
        var ultimo = (await simulacros.ListarAsync(ct))
            .Where(s => s.Inicio <= ahora && presentados.GetValueOrDefault(s.Id) > 0)
            .OrderByDescending(s => s.Fin)
            .FirstOrDefault();
        if (ultimo is null)
        {
            return null;
        }

        var resultados = await simulacroService.ResultadosAsync(ultimo.Id, null, ct);
        if (!resultados.IsSuccess)
        {
            return null;
        }

        var r = resultados.Value;
        return new SimulacroIndicador(ultimo.Id, ultimo.Titulo, ultimo.Tipo, r.Presentados, r.Habilitados, r.PromedioPorcentaje, r.PromedioGlobal, r.PorArea);
    }

    private async Task<AdmisionesIndicadores> AdmisionesAsync(DateOnly hoy, CancellationToken ct)
    {
        // A partir de agosto las solicitudes son, sobre todo, para el año siguiente.
        var anio = hoy.Month >= 8 ? hoy.Year + 1 : hoy.Year;
        var conteos = await admisiones.ConteoPorEstadoAsync(anio, ct);
        return new AdmisionesIndicadores(
            anio, conteos,
            conteos.Where(c => c.Estado is EstadoAdmision.Recibida or EstadoAdmision.EnRevision or EstadoAdmision.DocumentosPendientes).Sum(c => c.Total));
    }

    private async Task<CircularIndicador?> CircularAsync(CancellationToken ct)
    {
        var ultima = (await comunicaciones.CircularesAsync(ct)).Where(c => c.PublicadaEn is not null).MaxBy(c => c.PublicadaEn);
        return ultima is null
            ? null
            : new CircularIndicador(ultima.Id, ultima.Codigo, ultima.Titulo, ultima.Destinatarios, ultima.Leidas, ultima.Confirmadas, ultima.RequiereConfirmacion);
    }
}
