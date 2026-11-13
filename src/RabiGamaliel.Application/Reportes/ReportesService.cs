using System.Globalization;
using System.Text;
using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Certificaciones;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Admisiones;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Certificaciones;
using RabiGamaliel.Domain.Evaluacion;

namespace RabiGamaliel.Application.Reportes;

public interface IReportesRepository
{
    /// <summary>Solicitudes de admisión con estudiante creado en el campus (traen fecha de nacimiento, EPS, dirección...).</summary>
    Task<List<SolicitudAdmision>> AdmisionesMatriculadasAsync(CancellationToken ct);

    /// <summary>Decisiones de las actas de promoción final (sin periodo) del año.</summary>
    Task<List<DecisionEstudiante>> DecisionesFinalesAsync(int anio, CancellationToken ct);
}

/// <summary>Fila del formato tipo SIMAT para conciliar la matrícula.</summary>
public sealed record FilaSimat(
    Guid EstudianteId, string TipoDocumento, string Documento, string Apellido1, string? Apellido2, string Nombre1, string? Nombre2,
    DateOnly? FechaNacimiento, int Grado, string? Grupo, string Jornada, string? Direccion, string? Eps, string? Acudiente, string? DocumentoAcudiente,
    string? TelefonoAcudiente, string? ColegioProcedencia, bool DatosCompletos);

public sealed record AsignaturaLibro(
    string Asignatura, string Area, int? IntensidadHoraria, IReadOnlyList<decimal?> NotasPeriodos, decimal? Final, Desempeno? Desempeno, int Fallas);

public sealed record EstudianteLibro(
    Guid EstudianteId, string NombreCompleto, string Documento, IReadOnlyList<AsignaturaLibro> Asignaturas, decimal? Promedio, int Perdidas, int Fallas,
    int? Puesto, DecisionComision? Decision, string Resultado);

public sealed record LibroFinalResponse(
    int Anio, int Grado, string? Grupo, IReadOnlyList<PeriodoColumna> Periodos, bool AnioCerrado, IReadOnlyList<EstudianteLibro> Estudiantes,
    DateTimeOffset GeneradoEn);

public sealed record CertificadoEstudiosRequest(Guid EstudianteId, int Anio, string FirmanteNombre, string FirmanteCargo, TipoDocumento? TipoDocumento);

public interface IReportesService
{
    Task<IReadOnlyList<FilaSimat>> SimatAsync(int? grado, CancellationToken ct);

    Task<Result<LibroFinalResponse>> LibroFinalAsync(int anio, int grado, string? grupo, CancellationToken ct);

    /// <summary>Expide un certificado de estudios con las notas finales del año (queda con código de verificación).</summary>
    Task<Result<CertificadoResponse>> CertificadoEstudiosAsync(CertificadoEstudiosRequest request, CancellationToken ct);
}

/// <summary>Reportes oficiales: formato tipo SIMAT, libro final de calificaciones y certificado de estudios con notas.</summary>
public sealed class ReportesService(
    IReportesRepository repository,
    IAcademicoRepository academico,
    IMiembroCampusRepository miembros,
    IBoletinService boletines,
    ICertificadoService certificados,
    TimeProvider reloj) : IReportesService
{
    /// <summary>Las clases son de 1:00 a 6:30 p. m.</summary>
    public const string Jornada = "TARDE";

    private static readonly CultureInfo Colombia = CultureInfo.GetCultureInfo("es-CO");

    public async Task<IReadOnlyList<FilaSimat>> SimatAsync(int? grado, CancellationToken ct)
    {
        var estudiantes = (await miembros.ActivosPorRolAsync(RolCampus.Estudiante, ct))
            .Where(e => e.Grado is not null && (grado is null || e.Grado == grado))
            .OrderBy(e => e.Grado).ThenBy(e => e.Grupo).ThenBy(e => e.NombreCompleto)
            .ToList();
        var admisiones = await repository.AdmisionesMatriculadasAsync(ct);
        var porEstudiante = admisiones.Where(a => a.EstudianteId is not null).GroupBy(a => a.EstudianteId!.Value)
            .ToDictionary(g => g.Key, g => g.MaxBy(a => a.AnioLectivo)!);
        var porDocumento = admisiones.GroupBy(a => a.AspiranteDocumento).ToDictionary(g => g.Key, g => g.MaxBy(a => a.AnioLectivo)!);

        var vinculos = (await miembros.VinculosDeEstudiantesAsync([.. estudiantes.Select(e => e.Id)], ct)).ToLookup(v => v.EstudianteId);
        var acudientes = (await miembros.ObtenerPorIdsAsync([.. vinculos.SelectMany(g => g).Select(v => v.AcudienteId).Distinct()], ct)).ToDictionary(a => a.Id);

        return
        [
            .. estudiantes.Select(e =>
            {
                var admision = porEstudiante.GetValueOrDefault(e.Id) ?? porDocumento.GetValueOrDefault(e.Documento);
                var acudiente = vinculos[e.Id].Select(v => acudientes.GetValueOrDefault(v.AcudienteId)).FirstOrDefault(a => a is not null);
                var (apellido1, apellido2, nombre1, nombre2) = admision is null
                    ? DividirNombreCompleto(e.NombreCompleto)
                    : (Primera(admision.AspiranteApellidos), Resto(admision.AspiranteApellidos), Primera(admision.AspiranteNombres), Resto(admision.AspiranteNombres));
                return new FilaSimat(
                    e.Id, admision is null ? string.Empty : SiglaDocumento(admision.AspiranteTipoDocumento), e.Documento, apellido1, apellido2, nombre1, nombre2,
                    admision?.AspiranteFechaNacimiento, e.Grado!.Value, e.Grupo, Jornada, admision?.Direccion, admision?.Eps,
                    acudiente?.NombreCompleto ?? admision?.AcudienteNombre, acudiente?.Documento ?? admision?.AcudienteDocumento,
                    acudiente?.Telefono ?? admision?.AcudienteTelefono, admision?.ColegioProcedencia, admision is not null);
            }),
        ];
    }

    public async Task<Result<LibroFinalResponse>> LibroFinalAsync(int anio, int grado, string? grupo, CancellationToken ct)
    {
        var grupoNormalizado = string.IsNullOrWhiteSpace(grupo) ? null : grupo.Trim().ToUpperInvariant();
        var estudiantes = await miembros.EstudiantesDelCursoAsync(grado, grupoNormalizado, ct);
        return await LibroAsync(anio, grado, grupoNormalizado, estudiantes, ct);
    }

    public async Task<Result<CertificadoResponse>> CertificadoEstudiosAsync(CertificadoEstudiosRequest request, CancellationToken ct)
    {
        var r = request;
        var estudiante = await miembros.ObtenerPorIdAsync(r.EstudianteId, ct);
        if (estudiante is not { Rol: RolCampus.Estudiante, Grado: { } grado })
        {
            return Error.NoEncontrado("Estudiante.NoEncontrado", "No encontramos a ese estudiante.");
        }

        if (string.IsNullOrWhiteSpace(r.FirmanteNombre) || string.IsNullOrWhiteSpace(r.FirmanteCargo))
        {
            return Error.Campo("FirmanteNombre", "Indique quién firma el certificado y su cargo.");
        }

        var libro = await LibroAsync(r.Anio, grado, estudiante.Grupo, [estudiante], ct);
        if (!libro.IsSuccess)
        {
            return libro.Error;
        }

        var fila = libro.Value.Estudiantes.Count > 0 ? libro.Value.Estudiantes[0] : null;
        if (fila is null || fila.Asignaturas.Count == 0)
        {
            return Error.Conflicto("Certificado.SinNotas", $"{estudiante.NombreCompleto} no tiene calificaciones registradas en {r.Anio}.");
        }

        var tipoDocumento = r.TipoDocumento
                            ?? (await repository.AdmisionesMatriculadasAsync(ct)).FirstOrDefault(a => a.EstudianteId == estudiante.Id)?.AspiranteTipoDocumento
                            ?? TipoDocumento.TarjetaIdentidad;
        var nombreGrado = GradosEscolares.Nombre(grado);
        var contenido = new StringBuilder();
        var situacion = !libro.Value.AnioCerrado ? "cursa actualmente"
            : fila.Resultado == "Reprobó" ? "cursó y no aprobó"
            : fila.Resultado == "Aprobó" ? "cursó y aprobó"
            : "cursó";
        contenido.Append(CultureInfo.InvariantCulture, $"Que {estudiante.NombreCompleto.ToUpperInvariant()}, identificado(a) con {NombreDocumento(tipoDocumento)} n.º {estudiante.Documento}, ")
            .Append(CultureInfo.InvariantCulture, $"{situacion} en esta institución el grado {nombreGrado} de {(grado <= 5 ? "educación básica primaria" : grado <= 9 ? "educación básica secundaria" : "educación media académica")}")
            .Append(grado == GradosEscolares.Transicion ? " (preescolar)" : string.Empty)
            .Append(CultureInfo.InvariantCulture, $", en el año lectivo {r.Anio}, jornada de la tarde, con las siguientes valoraciones")
            .Append(libro.Value.AnioCerrado ? " finales" : " acumuladas a la fecha")
            .Append(" (escala de 1.0 a 5.0, Decreto 1290 de 2009):\n\n");
        foreach (var a in fila.Asignaturas)
        {
            contenido.Append(CultureInfo.InvariantCulture, $"• {a.Asignatura}")
                .Append(a.IntensidadHoraria is { } ih ? string.Create(CultureInfo.InvariantCulture, $" ({ih} h/sem)") : string.Empty)
                .Append(a.Final is { } nota ? string.Create(Colombia, $": {nota:0.0} – desempeño {DesempenoTexto(a.Desempeno)}") : ": sin valoración")
                .Append('\n');
        }

        if (fila.Promedio is { } promedio)
        {
            contenido.Append(Colombia, $"\nPromedio general: {promedio:0.00}.");
        }

        if (fila.Decision is { } decision)
        {
            contenido.Append(CultureInfo.InvariantCulture, $" Decisión de la comisión de evaluación y promoción: {DecisionTexto(decision)}.");
        }

        var texto = contenido.ToString().Trim();
        if (texto.Length > Certificado.ContenidoMaxLength)
        {
            texto = texto[..Certificado.ContenidoMaxLength];
        }

        return await certificados.ExpedirAsync(
            new ExpedirCertificadoRequest(
                TipoCertificado.Estudios, "CERTIFICADO DE ESTUDIOS", estudiante.NombreCompleto, tipoDocumento, estudiante.Documento,
                Familias.Curso(grado, estudiante.Grupo), r.Anio, texto, FechaColombia.Hoy(reloj), r.FirmanteNombre.Trim(), r.FirmanteCargo.Trim()),
            ct);
    }

    // ---------- Apoyo ----------

    private async Task<Result<LibroFinalResponse>> LibroAsync(int anio, int grado, string? grupo, IReadOnlyList<MiembroCampus> estudiantes, CancellationToken ct)
    {
        var periodos = (await academico.PeriodosAsync(anio, ct)).OrderBy(p => p.Numero).ToList();
        if (periodos.Count == 0)
        {
            return Error.Conflicto("Periodo.SinPeriodos", $"No hay periodos configurados en {anio}.");
        }

        var ultimo = periodos[^1];
        var calculados = await boletines.CalcularAsync([.. estudiantes.Where(e => e.Grado is not null)], ultimo, incluirNoPublicados: true, ct);
        var ids = estudiantes.Select(e => e.Id).ToList();
        var fallas = ids.Count == 0 ? [] : (await academico.FallasAsync(ids, periodos[0].Inicio, ultimo.Fin, null, ct))
            .GroupBy(f => (f.EstudianteId, f.AsignaturaId)).ToDictionary(g => g.Key, g => g.Sum(f => f.Fallas));
        var decisiones = (await repository.DecisionesFinalesAsync(anio, ct)).GroupBy(d => d.EstudianteId).ToDictionary(g => g.Key, g => g.Last().Decision);
        var hoy = FechaColombia.Hoy(reloj);
        var cerrado = hoy > ultimo.Fin;

        // El puesto del libro es por el promedio de las notas finales del año.
        var filas = calculados.Select(b =>
        {
            var asignaturas = b.Areas.SelectMany(a => a.Asignaturas.Select(x => new AsignaturaLibro(
                x.Nombre, a.Area, x.IntensidadHoraria, x.NotasPeriodos, x.Acumulada, CalculoBoletin.Desempeno(x.Acumulada),
                fallas.GetValueOrDefault((b.Estudiante.Id, x.AsignaturaId))))).ToList();
            return (Boletin: b, Asignaturas: asignaturas, Promedio: CalculoBoletin.Promedio(asignaturas.Select(a => a.Final)));
        }).ToList();
        var puestos = CalculoBoletin.Puestos(filas.ToDictionary(f => f.Boletin.Estudiante.Id, f => f.Promedio));

        return new LibroFinalResponse(
            anio, grado, grupo, [.. periodos.Select(p => new PeriodoColumna(p.Id, p.Numero, p.Nombre, p.Porcentaje))], cerrado,
            [
                .. filas.Select(f =>
                {
                    var perdidas = f.Asignaturas.Count(a => CalculoBoletin.Perdida(a.Final));
                    var decision = decisiones.TryGetValue(f.Boletin.Estudiante.Id, out var d) ? d : (DecisionComision?)null;
                    var resultado = decision switch
                    {
                        DecisionComision.NoPromovido => "Reprobó",
                        DecisionComision.Promovido or DecisionComision.PromocionAnticipada => "Aprobó",
                        _ when !cerrado => "En curso",
                        _ when perdidas >= 3 => "Reprobó",
                        _ when perdidas > 0 => "Pendiente",
                        _ => "Aprobó",
                    };
                    return new EstudianteLibro(
                        f.Boletin.Estudiante.Id, f.Boletin.Estudiante.NombreCompleto, f.Boletin.Estudiante.Documento, f.Asignaturas, f.Promedio, perdidas,
                        f.Asignaturas.Sum(a => a.Fallas), puestos.TryGetValue(f.Boletin.Estudiante.Id, out var p) ? p : null, decision, resultado);
                }),
            ],
            reloj.GetUtcNow());
    }

    public static (string Apellido1, string? Apellido2, string Nombre1, string? Nombre2) DividirNombreCompleto(string nombreCompleto)
    {
        var partes = nombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return partes.Length switch
        {
            0 => (string.Empty, null, string.Empty, null),
            1 => (partes[0], null, partes[0], null),
            2 => (partes[1], null, partes[0], null),
            3 => (partes[1], partes[2], partes[0], null),
            _ => (partes[^2], partes[^1], partes[0], string.Join(' ', partes[1..^2])),
        };
    }

    private static string Primera(string texto) => texto.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? string.Empty;

    private static string? Resto(string texto)
    {
        var partes = texto.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return partes.Length > 1 ? partes[1] : null;
    }

    public static string SiglaDocumento(TipoDocumento tipo) => tipo switch
    {
        TipoDocumento.RegistroCivil => "RC",
        TipoDocumento.TarjetaIdentidad => "TI",
        TipoDocumento.CedulaCiudadania => "CC",
        TipoDocumento.CedulaExtranjeria => "CE",
        TipoDocumento.PermisoProteccionTemporal => "PPT",
        TipoDocumento.Pasaporte => "PA",
        TipoDocumento.Nuip => "NUIP",
        _ => string.Empty,
    };

    private static string NombreDocumento(TipoDocumento tipo) => tipo switch
    {
        TipoDocumento.RegistroCivil => "registro civil",
        TipoDocumento.TarjetaIdentidad => "tarjeta de identidad",
        TipoDocumento.CedulaCiudadania => "cédula de ciudadanía",
        TipoDocumento.CedulaExtranjeria => "cédula de extranjería",
        TipoDocumento.PermisoProteccionTemporal => "permiso por protección temporal",
        TipoDocumento.Pasaporte => "pasaporte",
        TipoDocumento.Nuip => "NUIP",
        _ => "documento",
    };

    private static string DesempenoTexto(Desempeno? d) => d switch
    {
        Desempeno.Superior => "superior",
        Desempeno.Alto => "alto",
        Desempeno.Basico => "básico",
        Desempeno.Bajo => "bajo",
        _ => "sin valoración",
    };

    private static string DecisionTexto(DecisionComision d) => d switch
    {
        DecisionComision.Promovido => "promovido(a)",
        DecisionComision.NoPromovido => "no promovido(a)",
        DecisionComision.PromocionAnticipada => "promoción anticipada",
        DecisionComision.PlanMejoramiento => "plan de mejoramiento",
        DecisionComision.CompromisoAcademico => "compromiso académico",
        DecisionComision.RemitirOrientacion => "remisión a orientación escolar",
        _ => d.ToString(),
    };
}
