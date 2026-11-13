using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Aula;
using RabiGamaliel.Domain.Avisos;
using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Aula;

public sealed record ArchivoInfo(Guid Id, string NombreArchivo, string TipoContenido, int Tamano, Guid SubidoPorId);

public interface IAulaRepository
{
    Task<List<ActividadAula>> ActividadesAsync(IReadOnlyCollection<Guid> asignacionIds, CancellationToken ct);

    Task<ActividadAula?> ActividadAsync(Guid id, CancellationToken ct);

    void Agregar(ActividadAula actividad);

    void Eliminar(ActividadAula actividad);

    Task<List<EntregaAula>> EntregasAsync(IReadOnlyCollection<Guid> actividadIds, Guid? estudianteId, CancellationToken ct);

    void Agregar(EntregaAula entrega);

    Task<List<ArchivoInfo>> ArchivosAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct);

    Task<ArchivoAula?> ArchivoAsync(Guid id, CancellationToken ct);

    void Agregar(ArchivoAula archivo);

    /// <summary>Actividad o entrega que tiene adjunto ese archivo (para saber quién puede descargarlo).</summary>
    Task<(ActividadAula? Actividad, EntregaAula? Entrega)> UsoDeArchivoAsync(Guid archivoId, CancellationToken ct);
}

// ---------- Contratos ----------

public sealed record ArchivoResponse(Guid Id, string NombreArchivo, string TipoContenido, int Tamano);

public sealed record EntregaResponse(
    Guid? Id, Guid EstudianteId, string Estudiante, string? Texto, string? Enlace, ArchivoResponse? Archivo, DateTimeOffset? EntregadaEn, bool Tarde,
    decimal? Nota, string? Comentario, DateTimeOffset? CalificadaEn);

public sealed record ActividadResponse(
    Guid Id, Guid AsignacionId, string Asignatura, string? Docente, int Grado, string? Grupo, Guid PeriodoId, TipoActividad Tipo, string Titulo,
    string? Descripcion, string? Enlace, ArchivoResponse? Archivo, DateTimeOffset? FechaEntrega, bool Calificable, DateTimeOffset CreadoEn,
    int Entregas, int Calificadas, int Estudiantes, EntregaResponse? MiEntrega);

public sealed record GuardarActividadRequest(
    TipoActividad Tipo, Guid PeriodoId, string Titulo, string? Descripcion, string? Enlace, Guid? ArchivoId, DateTimeOffset? FechaEntrega,
    bool Calificable = true);

public sealed record EntregasActividadResponse(ActividadResponse Actividad, IReadOnlyList<EntregaResponse> Filas);

public sealed record CalificarEntregaRequest(Guid EstudianteId, decimal? Nota, string? Comentario);

public sealed record CalificarEntregasRequest(IReadOnlyList<CalificarEntregaRequest> Calificaciones);

public sealed record EntregarRequest(string? Texto, string? Enlace, Guid? ArchivoId);

public sealed record AulaEstudianteResponse(EstudianteBoletin Estudiante, IReadOnlyList<ActividadResponse> Actividades);

public sealed record PasoPlanillaResponse(int Actualizadas, int SinNotas, IReadOnlyList<string> Tareas);

public interface IAulaService
{
    Task<Result<ArchivoResponse>> SubirArchivoAsync(MiembroCampus actor, ArchivoSubido archivo, CancellationToken ct);

    Task<Result<ArchivoSubido>> DescargarAsync(MiembroCampus actor, Guid archivoId, CancellationToken ct);

    Task<Result<IReadOnlyList<ActividadResponse>>> ActividadesClaseAsync(MiembroCampus docente, Guid asignacionId, CancellationToken ct);

    Task<Result<ActividadResponse>> CrearAsync(MiembroCampus docente, Guid asignacionId, GuardarActividadRequest request, CancellationToken ct);

    Task<Result<ActividadResponse>> ActualizarAsync(MiembroCampus docente, Guid id, GuardarActividadRequest request, CancellationToken ct);

    Task<Result<Unit>> EliminarAsync(MiembroCampus docente, Guid id, CancellationToken ct);

    Task<Result<EntregasActividadResponse>> EntregasAsync(MiembroCampus docente, Guid actividadId, CancellationToken ct);

    Task<Result<EntregasActividadResponse>> CalificarAsync(MiembroCampus docente, Guid actividadId, CalificarEntregasRequest request, CancellationToken ct);

    /// <summary>Lleva a la planilla el promedio de las tareas calificables del periodo.</summary>
    Task<Result<PasoPlanillaResponse>> PasarAPlanillaAsync(MiembroCampus docente, Guid asignacionId, Guid periodoId, CancellationToken ct);

    Task<Result<AulaEstudianteResponse>> MiAulaAsync(MiembroCampus actor, Guid? estudianteId, CancellationToken ct);

    Task<Result<ActividadResponse>> EntregarAsync(MiembroCampus estudiante, Guid actividadId, EntregarRequest request, CancellationToken ct);
}

/// <summary>
/// Aula virtual: el docente publica material y tareas en sus clases, los estudiantes entregan, el docente califica y
/// lleva el promedio a la planilla de notas. Los acudientes ven las tareas y las notas de sus estudiantes.
/// </summary>
public sealed class AulaService(
    IAulaRepository repository,
    IAcademicoRepository academico,
    IMiembroCampusRepository miembros,
    IAccesoEstudiantes acceso,
    INotificador notificador,
    IUnitOfWork unitOfWork,
    TimeProvider reloj) : IAulaService
{
    private static readonly Error ActividadNoEncontrada = Error.NoEncontrado("Actividad.NoEncontrada", "No encontramos esa actividad.");
    private static readonly Error ArchivoNoEncontrado = Error.NoEncontrado("Archivo.NoEncontrado", "No encontramos ese archivo.");
    private static readonly Error ClaseAjena = Error.NoEncontrado("Asignacion.NoEncontrada", "Esa clase no está en su carga académica.");

    public async Task<Result<ArchivoResponse>> SubirArchivoAsync(MiembroCampus actor, ArchivoSubido archivo, CancellationToken ct)
    {
        if (actor.Rol == RolCampus.Acudiente)
        {
            return Error.Conflicto("Archivo.SoloEstudiantesDocentes", "Solo docentes y estudiantes suben archivos al aula.");
        }

        if (archivo.Contenido.Length is 0 or > ArchivoAula.TamanoMaximoBytes)
        {
            return Error.Campo("Archivo", "El archivo debe pesar como máximo 5 MB.");
        }

        if (!Archivos.Permitidos.Contains(archivo.TipoContenido) || !Archivos.FirmaValida(archivo))
        {
            return Error.Campo("Archivo", "Solo se aceptan PDF, imágenes JPG o PNG y documentos de Word, Excel o PowerPoint.");
        }

        var nuevo = ArchivoAula.Crear(actor.Id, archivo.NombreArchivo, archivo.TipoContenido.ToLowerInvariant(), archivo.Contenido);
        repository.Agregar(nuevo);
        await unitOfWork.SaveChangesAsync(ct);
        return new ArchivoResponse(nuevo.Id, nuevo.NombreArchivo, nuevo.TipoContenido, nuevo.Tamano);
    }

    public async Task<Result<ArchivoSubido>> DescargarAsync(MiembroCampus actor, Guid archivoId, CancellationToken ct)
    {
        var archivo = await repository.ArchivoAsync(archivoId, ct);
        if (archivo is null)
        {
            return ArchivoNoEncontrado;
        }

        if (archivo.SubidoPorId != actor.Id)
        {
            var (actividad, entrega) = await repository.UsoDeArchivoAsync(archivoId, ct);
            var permitido = false;
            if (actividad is not null)
            {
                permitido = await PuedeVerActividadAsync(actor, actividad, ct);
            }

            if (!permitido && entrega is not null && await repository.ActividadAsync(entrega.ActividadId, ct) is { } deEntrega)
            {
                permitido = deEntrega.DocenteId == actor.Id
                            || (actor.Rol != RolCampus.Docente && (await acceso.EstudianteVisibleAsync(actor, entrega.EstudianteId, ct)).IsSuccess);
            }

            if (!permitido)
            {
                return ArchivoNoEncontrado;
            }
        }

        return new ArchivoSubido(archivo.NombreArchivo, archivo.TipoContenido, archivo.Contenido);
    }

    // ---------- Docente ----------

    public async Task<Result<IReadOnlyList<ActividadResponse>>> ActividadesClaseAsync(MiembroCampus docente, Guid asignacionId, CancellationToken ct)
    {
        var asignacion = await AsignacionPropiaAsync(docente, asignacionId, ct);
        if (asignacion is null)
        {
            return ClaseAjena;
        }

        var actividades = await repository.ActividadesAsync([asignacion.Id], ct);
        return Resultado.Exito<IReadOnlyList<ActividadResponse>>(await ArmarAsync(actividades, null, ct));
    }

    public async Task<Result<ActividadResponse>> CrearAsync(MiembroCampus docente, Guid asignacionId, GuardarActividadRequest request, CancellationToken ct)
    {
        var asignacion = await AsignacionPropiaAsync(docente, asignacionId, ct);
        if (asignacion is null)
        {
            return ClaseAjena;
        }

        if (await ValidarAsync(docente, asignacion, request, ct) is { } invalido)
        {
            return invalido;
        }

        var actividad = ActividadAula.Crear(asignacion.Id, request.PeriodoId, docente.Id, request.Tipo);
        actividad.Actualizar(request.PeriodoId, request.Titulo, request.Descripcion, request.Enlace, request.ArchivoId, request.FechaEntrega, request.Calificable);
        repository.Agregar(actividad);

        var asignatura = (await academico.AsignaturaAsync(asignacion.AsignaturaId, ct))?.Nombre ?? "su clase";
        var estudiantes = await miembros.EstudiantesDelCursoAsync(asignacion.Grado, asignacion.Grupo, ct);
        var destinatarios = await Familias.DeEstudiantesAsync(miembros, [.. estudiantes.Select(e => e.Id)], ct);
        var limite = actividad.FechaEntrega is { } f ? $" Entrega: {f.ToOffset(FechaColombia.Desfase):dd/MM/yyyy h:mm tt}." : string.Empty;
        notificador.Notificar(
            destinatarios, TipoAviso.Tarea,
            actividad.Tipo == TipoActividad.Tarea ? $"Nueva tarea de {asignatura}" : $"Nuevo material de {asignatura}",
            $"{actividad.Titulo}.{limite}", "/campus/aula", null, porCorreo: false);

        await unitOfWork.SaveChangesAsync(ct);
        return (await ArmarAsync([actividad], null, ct))[0];
    }

    public async Task<Result<ActividadResponse>> ActualizarAsync(MiembroCampus docente, Guid id, GuardarActividadRequest request, CancellationToken ct)
    {
        var actividad = await repository.ActividadAsync(id, ct);
        if (actividad is null || actividad.DocenteId != docente.Id || await AsignacionPropiaAsync(docente, actividad.AsignacionId, ct) is not { } asignacion)
        {
            return ActividadNoEncontrada;
        }

        if (request.Tipo != actividad.Tipo)
        {
            return Error.Campo("Tipo", "No se puede cambiar el tipo de una actividad ya publicada.");
        }

        if (await ValidarAsync(docente, asignacion, request, ct) is { } invalido)
        {
            return invalido;
        }

        actividad.Actualizar(request.PeriodoId, request.Titulo, request.Descripcion, request.Enlace, request.ArchivoId, request.FechaEntrega, request.Calificable);
        await unitOfWork.SaveChangesAsync(ct);
        return (await ArmarAsync([actividad], null, ct))[0];
    }

    public async Task<Result<Unit>> EliminarAsync(MiembroCampus docente, Guid id, CancellationToken ct)
    {
        var actividad = await repository.ActividadAsync(id, ct);
        if (actividad is null || actividad.DocenteId != docente.Id)
        {
            return ActividadNoEncontrada;
        }

        if ((await repository.EntregasAsync([actividad.Id], null, ct)).Count > 0)
        {
            return Error.Conflicto("Actividad.ConEntregas", "La tarea ya tiene entregas o notas; edítela en lugar de eliminarla.");
        }

        repository.Eliminar(actividad);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Result<EntregasActividadResponse>> EntregasAsync(MiembroCampus docente, Guid actividadId, CancellationToken ct)
    {
        var actividad = await repository.ActividadAsync(actividadId, ct);
        if (actividad is null || actividad.DocenteId != docente.Id || await AsignacionPropiaAsync(docente, actividad.AsignacionId, ct) is not { } asignacion)
        {
            return ActividadNoEncontrada;
        }

        return await ArmarEntregasAsync(actividad, asignacion, ct);
    }

    public async Task<Result<EntregasActividadResponse>> CalificarAsync(
        MiembroCampus docente, Guid actividadId, CalificarEntregasRequest request, CancellationToken ct)
    {
        var actividad = await repository.ActividadAsync(actividadId, ct);
        if (actividad is null || actividad.DocenteId != docente.Id || await AsignacionPropiaAsync(docente, actividad.AsignacionId, ct) is not { } asignacion)
        {
            return ActividadNoEncontrada;
        }

        if (actividad.Tipo != TipoActividad.Tarea)
        {
            return Error.Conflicto("Actividad.NoEsTarea", "El material no se califica.");
        }

        if (request.Calificaciones.Any(c => c.Nota is { } n && !EscalaValorativa.EsValida(n)))
        {
            return Error.Campo("Nota", $"Las notas van de {EscalaValorativa.Minima:0.0} a {EscalaValorativa.Maxima:0.0}.");
        }

        if (request.Calificaciones.Any(c => c.Comentario?.Length > EntregaAula.ComentarioMaxLength))
        {
            return Error.Campo("Comentario", $"El comentario admite máximo {EntregaAula.ComentarioMaxLength} caracteres.");
        }

        var estudiantes = (await miembros.EstudiantesDelCursoAsync(asignacion.Grado, asignacion.Grupo, ct)).ToDictionary(e => e.Id);
        if (request.Calificaciones.Any(c => !estudiantes.ContainsKey(c.EstudianteId)))
        {
            return Error.Campo("Calificaciones", "Hay estudiantes que no pertenecen a este curso.");
        }

        var entregas = (await repository.EntregasAsync([actividad.Id], null, ct)).ToDictionary(e => e.EstudianteId);
        var ahora = reloj.GetUtcNow();
        var calificados = new List<Guid>();
        foreach (var c in request.Calificaciones)
        {
            if (!entregas.TryGetValue(c.EstudianteId, out var entrega))
            {
                if (c.Nota is null && string.IsNullOrWhiteSpace(c.Comentario))
                {
                    continue;
                }

                entrega = EntregaAula.Crear(actividad.Id, c.EstudianteId);
                repository.Agregar(entrega);
            }

            var antes = (entrega.Nota, entrega.Comentario);
            entrega.Calificar(c.Nota, c.Comentario, ahora);
            if (entrega.Nota is not null && (entrega.Nota, entrega.Comentario) != antes)
            {
                calificados.Add(c.EstudianteId);
            }
        }

        foreach (var estudianteId in calificados)
        {
            var familia = await Familias.DeEstudiantesAsync(miembros, [estudianteId], ct);
            var nota = entregas.TryGetValue(estudianteId, out var e) ? e.Nota : request.Calificaciones.First(c => c.EstudianteId == estudianteId).Nota;
            notificador.Notificar(
                familia, TipoAviso.Tarea, "Tarea calificada",
                $"{estudiantes[estudianteId].NombreCompleto}: «{actividad.Titulo}» obtuvo {nota:0.0}.", "/campus/aula", estudianteId, porCorreo: false);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return await ArmarEntregasAsync(actividad, asignacion, ct);
    }

    public async Task<Result<PasoPlanillaResponse>> PasarAPlanillaAsync(MiembroCampus docente, Guid asignacionId, Guid periodoId, CancellationToken ct)
    {
        var asignacion = await AsignacionPropiaAsync(docente, asignacionId, ct);
        if (asignacion is null)
        {
            return ClaseAjena;
        }

        var periodo = await academico.PeriodoAsync(periodoId, ct);
        if (periodo is null || periodo.Anio != asignacion.Anio)
        {
            return Error.NoEncontrado("Periodo.NoEncontrado", "Ese periodo no corresponde al año de la clase.");
        }

        if (!periodo.NotasAbiertas)
        {
            return Error.Conflicto("Periodo.NotasCerradas", "El registro de notas de este periodo está cerrado. Pida a coordinación que lo abra.");
        }

        var tareas = (await repository.ActividadesAsync([asignacion.Id], ct))
            .Where(a => a.Tipo == TipoActividad.Tarea && a.Calificable && a.PeriodoId == periodo.Id)
            .ToList();
        if (tareas.Count == 0)
        {
            return Error.Conflicto("Aula.SinTareas", "No hay tareas calificables en este periodo.");
        }

        var estudiantes = await miembros.EstudiantesDelCursoAsync(asignacion.Grado, asignacion.Grupo, ct);
        var ids = estudiantes.Select(e => e.Id).ToList();
        var notas = (await repository.EntregasAsync([.. tareas.Select(t => t.Id)], null, ct))
            .Where(e => e.Nota is not null)
            .ToLookup(e => e.EstudianteId, e => e.Nota);
        var calificaciones = (await academico.CalificacionesAsync(ids, [periodo.Id], asignacion.AsignaturaId, ct)).ToDictionary(c => c.EstudianteId);

        var actualizadas = 0;
        foreach (var estudiante in estudiantes)
        {
            var promedio = CalculoBoletin.Promedio(notas[estudiante.Id]);
            if (promedio is null)
            {
                continue;
            }

            if (!calificaciones.TryGetValue(estudiante.Id, out var calificacion))
            {
                calificacion = Calificacion.Crear(estudiante.Id, asignacion.AsignaturaId, periodo.Id);
                academico.Agregar(calificacion);
            }

            calificacion.Registrar(Math.Round(promedio.Value, 1, MidpointRounding.AwayFromZero), calificacion.Fallas, calificacion.Observacion, docente.NombreCompleto);
            actualizadas++;
        }

        await unitOfWork.SaveChangesAsync(ct);
        return new PasoPlanillaResponse(actualizadas, estudiantes.Count - actualizadas, [.. tareas.OrderBy(t => t.CreadoEn).Select(t => t.Titulo)]);
    }

    // ---------- Estudiante y acudiente ----------

    public async Task<Result<AulaEstudianteResponse>> MiAulaAsync(MiembroCampus actor, Guid? estudianteId, CancellationToken ct)
    {
        MiembroCampus estudiante;
        if (estudianteId is { } id)
        {
            var visible = await acceso.EstudianteVisibleAsync(actor, id, ct);
            if (!visible.IsSuccess)
            {
                return visible.Error;
            }

            estudiante = visible.Value;
        }
        else
        {
            var propios = await acceso.EstudiantesPropiosAsync(actor, ct);
            if (propios.Count == 0)
            {
                return Error.NoEncontrado("Aula.SinEstudiante", "No hay estudiantes vinculados a su cuenta.");
            }

            estudiante = propios[0];
        }

        if (estudiante.Grado is not { } grado)
        {
            return new AulaEstudianteResponse(Estudiante(estudiante), []);
        }

        var asignaciones = (await academico.AsignacionesAsync(FechaColombia.AnioLectivo(reloj), null, grado, estudiante.Grupo, ct))
            .Where(a => a.Incluye(grado, estudiante.Grupo))
            .ToList();
        var actividades = await repository.ActividadesAsync([.. asignaciones.Select(a => a.Id)], ct);
        return new AulaEstudianteResponse(Estudiante(estudiante), await ArmarAsync(actividades, estudiante.Id, ct));
    }

    public async Task<Result<ActividadResponse>> EntregarAsync(MiembroCampus estudiante, Guid actividadId, EntregarRequest request, CancellationToken ct)
    {
        if (estudiante.Rol != RolCampus.Estudiante)
        {
            return Error.Conflicto("Aula.SoloEstudiantes", "Solo el estudiante entrega sus tareas.");
        }

        var actividad = await repository.ActividadAsync(actividadId, ct);
        if (actividad is null || actividad.Tipo != TipoActividad.Tarea || !await PuedeVerActividadAsync(estudiante, actividad, ct))
        {
            return ActividadNoEncontrada;
        }

        if (string.IsNullOrWhiteSpace(request.Texto) && string.IsNullOrWhiteSpace(request.Enlace) && request.ArchivoId is null)
        {
            return Error.Campo("Texto", "Escriba la respuesta, pegue un enlace o adjunte un archivo.");
        }

        if (request.Texto?.Length > EntregaAula.TextoMaxLength)
        {
            return Error.Campo("Texto", $"La respuesta admite máximo {EntregaAula.TextoMaxLength} caracteres.");
        }

        if (!Archivos.EnlaceValido(request.Enlace) || request.Enlace?.Length > ActividadAula.EnlaceMaxLength)
        {
            return Error.Campo("Enlace", "El enlace debe empezar por https://");
        }

        if (request.ArchivoId is { } archivoId && (await repository.ArchivosAsync([archivoId], ct)).FirstOrDefault()?.SubidoPorId != estudiante.Id)
        {
            return Error.Campo("ArchivoId", "Suba de nuevo el archivo.");
        }

        var entrega = (await repository.EntregasAsync([actividad.Id], estudiante.Id, ct)).FirstOrDefault();
        if (entrega?.CalificadaEn is not null)
        {
            return Error.Conflicto("Entrega.Calificada", "La tarea ya fue calificada; no se puede cambiar la entrega.");
        }

        if (entrega is null)
        {
            entrega = EntregaAula.Crear(actividad.Id, estudiante.Id);
            repository.Agregar(entrega);
        }

        entrega.Entregar(request.Texto, request.Enlace, request.ArchivoId, reloj.GetUtcNow());
        await unitOfWork.SaveChangesAsync(ct);
        return (await ArmarAsync([actividad], estudiante.Id, ct))[0];
    }

    // ---------- Apoyo ----------

    private static EstudianteBoletin Estudiante(MiembroCampus e) => new(e.Id, e.NombreCompleto, e.Documento, e.Grado, e.Grupo);

    private async Task<AsignacionDocente?> AsignacionPropiaAsync(MiembroCampus docente, Guid asignacionId, CancellationToken ct)
    {
        if (docente.Rol != RolCampus.Docente)
        {
            return null;
        }

        var asignacion = await academico.AsignacionAsync(asignacionId, ct);
        return asignacion is not null && asignacion.DocenteId == docente.Id ? asignacion : null;
    }

    private async Task<bool> PuedeVerActividadAsync(MiembroCampus actor, ActividadAula actividad, CancellationToken ct)
    {
        if (actor.Rol == RolCampus.Docente)
        {
            return actividad.DocenteId == actor.Id;
        }

        var asignacion = await academico.AsignacionAsync(actividad.AsignacionId, ct);
        if (asignacion is null)
        {
            return false;
        }

        var propios = await acceso.EstudiantesPropiosAsync(actor, ct);
        return propios.Any(e => asignacion.Incluye(e.Grado, e.Grupo));
    }

    private async Task<Error?> ValidarAsync(MiembroCampus docente, AsignacionDocente asignacion, GuardarActividadRequest r, CancellationToken ct)
    {
        if (!Enum.IsDefined(r.Tipo))
        {
            return Error.Campo("Tipo", "Elija si es material o tarea.");
        }

        if (string.IsNullOrWhiteSpace(r.Titulo) || r.Titulo.Trim().Length > ActividadAula.TituloMaxLength)
        {
            return Error.Campo("Titulo", $"Escriba un título de máximo {ActividadAula.TituloMaxLength} caracteres.");
        }

        if (r.Descripcion?.Length > ActividadAula.DescripcionMaxLength)
        {
            return Error.Campo("Descripcion", $"Las instrucciones admiten máximo {ActividadAula.DescripcionMaxLength} caracteres.");
        }

        if (!Archivos.EnlaceValido(r.Enlace) || r.Enlace?.Length > ActividadAula.EnlaceMaxLength)
        {
            return Error.Campo("Enlace", "El enlace debe empezar por https://");
        }

        if (r.Tipo == TipoActividad.Tarea && r.FechaEntrega is null)
        {
            return Error.Campo("FechaEntrega", "Indique la fecha de entrega de la tarea.");
        }

        var periodo = await academico.PeriodoAsync(r.PeriodoId, ct);
        if (periodo is null || periodo.Anio != asignacion.Anio)
        {
            return Error.Campo("PeriodoId", "Elija un periodo del año de la clase.");
        }

        if (r.ArchivoId is { } archivoId && (await repository.ArchivosAsync([archivoId], ct)).FirstOrDefault()?.SubidoPorId != docente.Id)
        {
            return Error.Campo("ArchivoId", "Suba de nuevo el archivo.");
        }

        return null;
    }

    private async Task<Result<EntregasActividadResponse>> ArmarEntregasAsync(ActividadAula actividad, AsignacionDocente asignacion, CancellationToken ct)
    {
        var estudiantes = await miembros.EstudiantesDelCursoAsync(asignacion.Grado, asignacion.Grupo, ct);
        var entregas = (await repository.EntregasAsync([actividad.Id], null, ct)).ToDictionary(e => e.EstudianteId);
        var archivos = (await repository.ArchivosAsync([.. entregas.Values.Where(e => e.ArchivoId is not null).Select(e => e.ArchivoId!.Value)], ct))
            .ToDictionary(a => a.Id);
        var filas = estudiantes
            .Select(e => entregas.TryGetValue(e.Id, out var entrega)
                ? Entrega(entrega, e.NombreCompleto, actividad, archivos)
                : new EntregaResponse(null, e.Id, e.NombreCompleto, null, null, null, null, false, null, null, null))
            .ToList();
        return new EntregasActividadResponse((await ArmarAsync([actividad], null, ct))[0], filas);
    }

    private static EntregaResponse Entrega(EntregaAula e, string nombre, ActividadAula actividad, Dictionary<Guid, ArchivoInfo> archivos) => new(
        e.Id, e.EstudianteId, nombre, e.Texto, e.Enlace, Archivo(e.ArchivoId, archivos), e.EntregadaEn, e.Tarde(actividad.FechaEntrega), e.Nota,
        e.Comentario, e.CalificadaEn);

    private static ArchivoResponse? Archivo(Guid? id, Dictionary<Guid, ArchivoInfo> archivos) =>
        id is { } valor && archivos.TryGetValue(valor, out var a) ? new ArchivoResponse(a.Id, a.NombreArchivo, a.TipoContenido, a.Tamano) : null;

    private async Task<List<ActividadResponse>> ArmarAsync(List<ActividadAula> actividades, Guid? estudianteId, CancellationToken ct)
    {
        if (actividades.Count == 0)
        {
            return [];
        }

        var asignaciones = new Dictionary<Guid, AsignacionDocente>();
        foreach (var id in actividades.Select(a => a.AsignacionId).Distinct())
        {
            if (await academico.AsignacionAsync(id, ct) is { } asignacion)
            {
                asignaciones[id] = asignacion;
            }
        }

        var asignaturas = (await academico.AsignaturasAsync(soloActivas: false, ct)).ToDictionary(a => a.Id, a => a.Nombre);
        var docentes = (await miembros.ObtenerPorIdsAsync([.. actividades.Select(a => a.DocenteId).Distinct()], ct)).ToDictionary(d => d.Id, d => d.NombreCompleto);
        var entregas = await repository.EntregasAsync([.. actividades.Select(a => a.Id)], estudianteId, ct);
        var porActividad = entregas.ToLookup(e => e.ActividadId);
        var archivoIds = actividades.Select(a => a.ArchivoId).Concat(entregas.Select(e => e.ArchivoId)).OfType<Guid>().Distinct().ToList();
        var archivos = (await repository.ArchivosAsync(archivoIds, ct)).ToDictionary(a => a.Id);
        var tamanoCurso = new Dictionary<Guid, int>();
        if (estudianteId is null)
        {
            foreach (var a in asignaciones.Values)
            {
                tamanoCurso[a.Id] = (await miembros.EstudiantesDelCursoAsync(a.Grado, a.Grupo, ct)).Count;
            }
        }

        return
        [
            .. actividades
                .Where(a => asignaciones.ContainsKey(a.AsignacionId))
                .OrderByDescending(a => a.CreadoEn)
                .Select(a =>
                {
                    var asignacion = asignaciones[a.AsignacionId];
                    var propias = porActividad[a.Id].ToList();
                    var mia = estudianteId is { } id ? propias.FirstOrDefault(e => e.EstudianteId == id) : null;
                    return new ActividadResponse(
                        a.Id, a.AsignacionId, asignaturas.GetValueOrDefault(asignacion.AsignaturaId, "(asignatura)"), docentes.GetValueOrDefault(a.DocenteId),
                        asignacion.Grado, asignacion.Grupo, a.PeriodoId, a.Tipo, a.Titulo, a.Descripcion, a.Enlace, Archivo(a.ArchivoId, archivos),
                        a.FechaEntrega, a.Calificable, a.CreadoEn, propias.Count(e => e.EntregadaEn is not null), propias.Count(e => e.Nota is not null),
                        tamanoCurso.GetValueOrDefault(a.AsignacionId), mia is null ? null : Entrega(mia, string.Empty, a, archivos));
                }),
        ];
    }
}
