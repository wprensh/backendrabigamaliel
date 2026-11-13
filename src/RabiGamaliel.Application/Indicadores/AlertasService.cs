using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Convivencia;

namespace RabiGamaliel.Application.Indicadores;

public enum NivelRiesgo
{
    Ninguno = 0,
    Bajo,
    Medio,
    Alto,
}

/// <summary>Señales de un estudiante que usa la alerta temprana.</summary>
public sealed record DatosRiesgo(
    int Ausencias30Dias,
    int AusenciasAnio,
    int PerdidasPeriodo,
    string? Periodo,
    int PerdidasAcumuladas,
    decimal? Promedio,
    int SituacionesTipoI,
    int SituacionesTipoII,
    int SituacionesTipoIII,
    double? PorcentajeSimulacro);

public sealed record EvaluacionRiesgo(int Puntaje, NivelRiesgo Nivel, IReadOnlyList<string> Motivos);

/// <summary>
/// Puntaje de riesgo de deserción y de pérdida del año (0 a 100). Suma inasistencias recientes, asignaturas perdidas,
/// promedio bajo, situaciones de convivencia abiertas y un simulacro bajo. Los umbrales se ajustan aquí.
/// </summary>
public static class CalculoRiesgo
{
    public const int UmbralBajo = 10;
    public const int UmbralMedio = 25;
    public const int UmbralAlto = 50;

    /// <summary>Asignaturas perdidas en el acumulado con las que el SIEE suele reprobar el grado.</summary>
    public const int PerdidasQueReprueban = 3;

    public static EvaluacionRiesgo Evaluar(DatosRiesgo d)
    {
        var puntaje = 0;
        var motivos = new List<string>();

        if (d.Ausencias30Dias >= 3)
        {
            puntaje += d.Ausencias30Dias >= 10 ? 30 : d.Ausencias30Dias >= 5 ? 20 : 10;
            motivos.Add($"{d.Ausencias30Dias} inasistencias en los últimos 30 días");
        }

        if (d.AusenciasAnio >= 25)
        {
            puntaje += 10;
            motivos.Add($"{d.AusenciasAnio} inasistencias en el año");
        }

        if (d.PerdidasAcumuladas >= PerdidasQueReprueban)
        {
            puntaje += 35;
            motivos.Add($"Va perdiendo {d.PerdidasAcumuladas} asignaturas en el acumulado del año: puede reprobar el grado");
        }
        else if (d.PerdidasAcumuladas > 0)
        {
            puntaje += 15 * d.PerdidasAcumuladas;
            motivos.Add(d.PerdidasAcumuladas == 1 ? "Va perdiendo 1 asignatura en el acumulado del año" : $"Va perdiendo {d.PerdidasAcumuladas} asignaturas en el acumulado del año");
        }

        if (d.PerdidasPeriodo > 0)
        {
            puntaje += Math.Min(15, 5 * d.PerdidasPeriodo);
            motivos.Add($"Perdió {d.PerdidasPeriodo} asignatura(s) en {d.Periodo ?? "el último periodo"}");
        }

        if (d.Promedio is { } promedio && promedio < EscalaValorativa.Aprobatoria)
        {
            puntaje += 10;
            motivos.Add($"Promedio de {promedio:0.0}");
        }

        var convivencia = Math.Min(20, (d.SituacionesTipoIII * 20) + (d.SituacionesTipoII * 10) + (d.SituacionesTipoI >= 2 ? 5 : 0));
        if (convivencia > 0)
        {
            puntaje += convivencia;
            var partes = new List<string>();
            if (d.SituacionesTipoIII > 0)
            {
                partes.Add($"{d.SituacionesTipoIII} tipo III");
            }

            if (d.SituacionesTipoII > 0)
            {
                partes.Add($"{d.SituacionesTipoII} tipo II");
            }

            if (d.SituacionesTipoI > 0)
            {
                partes.Add($"{d.SituacionesTipoI} tipo I");
            }

            motivos.Add($"Situaciones de convivencia abiertas: {string.Join(", ", partes)}");
        }

        if (d.PorcentajeSimulacro is { } simulacro && simulacro < 55)
        {
            puntaje += simulacro < 40 ? 10 : 5;
            motivos.Add($"{simulacro:0}% en el último simulacro");
        }

        puntaje = Math.Min(100, puntaje);
        var nivel = puntaje >= UmbralAlto ? NivelRiesgo.Alto
            : puntaje >= UmbralMedio ? NivelRiesgo.Medio
            : puntaje >= UmbralBajo ? NivelRiesgo.Bajo
            : NivelRiesgo.Ninguno;
        return new EvaluacionRiesgo(puntaje, nivel, motivos);
    }
}

public sealed record AlertaEstudiante(
    Guid EstudianteId,
    string NombreCompleto,
    string Documento,
    int? Grado,
    string? Grupo,
    int Puntaje,
    NivelRiesgo Nivel,
    IReadOnlyList<string> Motivos,
    int Ausencias30Dias,
    int AusenciasAnio,
    int PerdidasAcumuladas,
    decimal? Promedio,
    int SituacionesAbiertas,
    double? PorcentajeSimulacro,
    string? Acudiente,
    string? TelefonoAcudiente,
    string? CorreoAcudiente);

public sealed record AlertasResponse(DateOnly Fecha, string? Periodo, int Evaluados, int Altos, int Medios, int Bajos, IReadOnlyList<AlertaEstudiante> Items);

public sealed record SimulacroEntregado(Guid EstudianteId, int Aciertos, int TotalPreguntas, DateTimeOffset EntregadoEn);

public interface IAlertasService
{
    Task<AlertasResponse> CalcularAsync(int? grado, string? grupo, CancellationToken ct);
}

/// <summary>Alerta temprana: estudiantes en riesgo de desertar o de perder el año, con los motivos y el contacto del acudiente.</summary>
public sealed class AlertasService(
    IIndicadoresRepository repository,
    IMiembroCampusRepository miembros,
    IAcademicoRepository academico,
    TimeProvider reloj) : IAlertasService
{
    public async Task<AlertasResponse> CalcularAsync(int? grado, string? grupo, CancellationToken ct)
    {
        var hoy = FechaColombia.Hoy(reloj);
        var grupoNormalizado = string.IsNullOrWhiteSpace(grupo) ? null : grupo.Trim().ToUpperInvariant();
        var estudiantes = (await miembros.ActivosPorRolAsync(RolCampus.Estudiante, ct))
            .Where(e => e.Grado is not null && (grado is null || e.Grado == grado) && (grupoNormalizado is null || e.Grupo == grupoNormalizado))
            .ToList();
        var ids = estudiantes.Select(e => e.Id).ToList();

        var ausencias30 = (await repository.MasAusenciasAsync(hoy.AddDays(-30), hoy, int.MaxValue, ct)).ToDictionary(a => a.EstudianteId, a => a.Total);
        var ausenciasAnio = (await repository.MasAusenciasAsync(new DateOnly(hoy.Year, 1, 1), hoy, int.MaxValue, ct)).ToDictionary(a => a.EstudianteId, a => a.Total);

        var periodos = (await academico.PeriodosAsync(hoy.Year, ct)).OrderBy(p => p.Numero).ToList();
        var notas = ids.Count == 0 || periodos.Count == 0 ? [] : await academico.CalificacionesAsync(ids, [.. periodos.Select(p => p.Id)], null, ct);
        var conNota = notas.Where(n => n.Nota is not null).ToList();
        var ultimo = periodos.LastOrDefault(p => conNota.Any(n => n.PeriodoId == p.Id));
        var notasPorEstudiante = conNota.ToLookup(n => n.EstudianteId);

        var situaciones = (await repository.SituacionesAbiertasAsync(ct)).ToLookup(a => a.EstudianteId);
        var simulacros = (await repository.SimulacrosEntregadosAsync(new DateTimeOffset(hoy.Year, 1, 1, 0, 0, 0, TimeSpan.Zero), ct))
            .GroupBy(s => s.EstudianteId)
            .ToDictionary(g => g.Key, g => g.MaxBy(s => s.EntregadoEn)!);

        var items = new List<AlertaEstudiante>();
        foreach (var e in estudiantes)
        {
            var propias = notasPorEstudiante[e.Id].ToList();
            var acumuladas = propias.GroupBy(n => n.AsignaturaId)
                .Select(g => CalculoBoletin.Acumulada([.. periodos.Select(p => (g.FirstOrDefault(n => n.PeriodoId == p.Id)?.Nota, p.Porcentaje))]))
                .ToList();
            var abiertas = situaciones[e.Id].ToList();
            double? simulacro = simulacros.TryGetValue(e.Id, out var s) && s.TotalPreguntas > 0
                ? Math.Round(s.Aciertos * 100.0 / s.TotalPreguntas, 1)
                : null;

            var datos = new DatosRiesgo(
                ausencias30.GetValueOrDefault(e.Id),
                ausenciasAnio.GetValueOrDefault(e.Id),
                ultimo is null ? 0 : propias.Count(n => n.PeriodoId == ultimo.Id && CalculoBoletin.Perdida(n.Nota)),
                ultimo?.Nombre,
                acumuladas.Count(CalculoBoletin.Perdida),
                CalculoBoletin.Promedio(acumuladas),
                abiertas.Count(a => a.Tipo == TipoAnotacion.SituacionTipoI),
                abiertas.Count(a => a.Tipo == TipoAnotacion.SituacionTipoII),
                abiertas.Count(a => a.Tipo == TipoAnotacion.SituacionTipoIII),
                simulacro);
            var riesgo = CalculoRiesgo.Evaluar(datos);
            if (riesgo.Nivel == NivelRiesgo.Ninguno)
            {
                continue;
            }

            items.Add(new AlertaEstudiante(
                e.Id, e.NombreCompleto, e.Documento, e.Grado, e.Grupo, riesgo.Puntaje, riesgo.Nivel, riesgo.Motivos, datos.Ausencias30Dias,
                datos.AusenciasAnio, datos.PerdidasAcumuladas, datos.Promedio, abiertas.Count, simulacro, null, null, null));
        }

        // Contacto del primer acudiente de cada estudiante en riesgo.
        var vinculos = items.Count == 0 ? [] : await miembros.VinculosDeEstudiantesAsync([.. items.Select(i => i.EstudianteId)], ct);
        var acudientes = (await miembros.ObtenerPorIdsAsync([.. vinculos.Select(v => v.AcudienteId).Distinct()], ct)).ToDictionary(a => a.Id);
        var primerAcudiente = vinculos.GroupBy(v => v.EstudianteId)
            .ToDictionary(g => g.Key, g => g.Select(v => acudientes.GetValueOrDefault(v.AcudienteId)).OrderByDescending(a => a?.Telefono is not null).FirstOrDefault());
        var conContacto = items
            .Select(i => primerAcudiente.GetValueOrDefault(i.EstudianteId) is { } a
                ? i with { Acudiente = a.NombreCompleto, TelefonoAcudiente = a.Telefono, CorreoAcudiente = a.Correo }
                : i)
            .OrderByDescending(i => i.Puntaje).ThenBy(i => i.Grado).ThenBy(i => i.NombreCompleto)
            .ToList();

        return new AlertasResponse(
            hoy, ultimo?.Nombre, estudiantes.Count, conContacto.Count(i => i.Nivel == NivelRiesgo.Alto),
            conContacto.Count(i => i.Nivel == NivelRiesgo.Medio), conContacto.Count(i => i.Nivel == NivelRiesgo.Bajo), conContacto);
    }
}
