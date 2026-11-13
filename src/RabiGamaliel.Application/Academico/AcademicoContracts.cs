using RabiGamaliel.Domain.Academico;

namespace RabiGamaliel.Application.Academico;

public sealed record AsignaturaResponse(Guid Id, string Nombre, string Area, int Orden, bool Activa)
{
    public static AsignaturaResponse Desde(Asignatura a) => new(a.Id, a.Nombre, a.Area, a.Orden, a.Activa);
}

public sealed record GuardarAsignaturaRequest(string Nombre, string? Area, int Orden, bool Activa = true);

public sealed record PeriodoResponse(
    Guid Id, int Anio, int Numero, string Nombre, DateOnly Inicio, DateOnly Fin, int Porcentaje, bool NotasAbiertas, bool Publicado)
{
    public static PeriodoResponse Desde(PeriodoAcademico p) =>
        new(p.Id, p.Anio, p.Numero, p.Nombre, p.Inicio, p.Fin, p.Porcentaje, p.NotasAbiertas, p.Publicado);
}

public sealed record GuardarPeriodoRequest(
    int Anio, int Numero, string? Nombre, DateOnly Inicio, DateOnly Fin, int Porcentaje, bool NotasAbiertas = true, bool Publicado = false);

public sealed record AsignacionResponse(
    Guid Id, int Anio, Guid DocenteId, string DocenteNombre, Guid AsignaturaId, string Asignatura, string Area, int Grado, string? Grupo,
    int? IntensidadHoraria);

public sealed record CrearAsignacionRequest(int Anio, Guid DocenteId, Guid AsignaturaId, int Grado, string? Grupo, int? IntensidadHoraria);

public sealed record ActualizarAsignacionRequest(Guid DocenteId, int? IntensidadHoraria);

/// <summary>Curso con estudiantes activos (grado y grupo).</summary>
public sealed record CursoResponse(int Grado, string? Grupo, int Estudiantes);

// ---------- Consolidado (sábana de notas) ----------

public sealed record ColumnaAsignatura(Guid Id, string Nombre, string Area);

public sealed record FilaConsolidado(
    Guid EstudianteId, string NombreCompleto, string Documento, IReadOnlyList<decimal?> Notas, decimal? Promedio, int? Puesto,
    int AsignaturasPerdidas);

public sealed record ConsolidadoResponse(
    PeriodoResponse Periodo, int Grado, string? Grupo, IReadOnlyList<ColumnaAsignatura> Asignaturas, IReadOnlyList<FilaConsolidado> Filas,
    decimal? PromedioCurso, int AvanceCarguePorcentaje);

// ---------- Planilla del docente ----------

public sealed record CursoDocente(
    Guid AsignacionId, Guid AsignaturaId, string Asignatura, string Area, int Grado, string? Grupo, int? IntensidadHoraria, int Estudiantes);

public sealed record MisCursosResponse(int Anio, IReadOnlyList<PeriodoResponse> Periodos, IReadOnlyList<CursoDocente> Cursos);

public sealed record FilaPlanillaNotas(
    Guid EstudianteId, string NombreCompleto, string Documento, decimal? Nota, int? Fallas, int FallasRegistradas, string? Observacion);

public sealed record PlanillaNotasResponse(
    Guid AsignacionId, string Asignatura, int Grado, string? Grupo, PeriodoResponse Periodo, bool Editable, IReadOnlyList<FilaPlanillaNotas> Filas);

public sealed record NotaEstudianteRequest(Guid EstudianteId, decimal? Nota, int? Fallas, string? Observacion);

public sealed record GuardarNotasRequest(IReadOnlyList<NotaEstudianteRequest> Notas);

// ---------- Boletín ----------

public sealed record EstudianteBoletin(Guid Id, string NombreCompleto, string Documento, int? Grado, string? Grupo);

public sealed record PeriodoColumna(Guid Id, int Numero, string Nombre, int Porcentaje);

public sealed record AsignaturaBoletin(
    Guid AsignaturaId, string Nombre, string? Docente, int? IntensidadHoraria, IReadOnlyList<decimal?> NotasPeriodos, decimal? Nota,
    Desempeno? Desempeno, int Fallas, string? Observacion, decimal? Acumulada);

public sealed record AreaBoletin(string Area, IReadOnlyList<AsignaturaBoletin> Asignaturas);

public sealed record BoletinResponse(
    EstudianteBoletin Estudiante,
    PeriodoResponse Periodo,
    IReadOnlyList<PeriodoColumna> Periodos,
    IReadOnlyList<AreaBoletin> Areas,
    decimal? Promedio,
    Desempeno? DesempenoGeneral,
    int? Puesto,
    int TotalCurso,
    int FallasTotales,
    int AsignaturasPerdidas,
    DateTimeOffset GeneradoEn);

/// <summary>Lo que el estudiante o el acudiente puede consultar: sus estudiantes y los periodos publicados.</summary>
public sealed record BoletinesDisponiblesResponse(IReadOnlyList<EstudianteBoletin> Estudiantes, IReadOnlyList<PeriodoResponse> Periodos);

public sealed record ConteoFallas(Guid EstudianteId, Guid AsignaturaId, int Fallas);
