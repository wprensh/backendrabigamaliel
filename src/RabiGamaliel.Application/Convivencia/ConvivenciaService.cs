using FluentValidation;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Avisos;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Convivencia;

namespace RabiGamaliel.Application.Convivencia;

public sealed record SeguimientoResponse(Guid Id, string Texto, string AutorNombre, DateTimeOffset CreadoEn);

public sealed record AnotacionResponse(
    Guid Id, Guid EstudianteId, string EstudianteNombre, int? Grado, string? Grupo, TipoAnotacion Tipo, DateOnly Fecha, string Descripcion,
    string? Compromisos, string AutorNombre, EstadoCaso Estado, DateTimeOffset? EnteradoEn, string? EnteradoPor,
    IReadOnlyList<SeguimientoResponse> Seguimientos, DateTimeOffset CreadoEn)
{
    public static AnotacionResponse Desde(Anotacion a, string estudianteNombre) => new(
        a.Id, a.EstudianteId, estudianteNombre, a.Grado, a.Grupo, a.Tipo, a.Fecha, a.Descripcion, a.Compromisos, a.AutorNombre, a.Estado,
        a.EnteradoEn, a.EnteradoPor,
        [.. a.Seguimientos.OrderBy(s => s.CreadoEn).Select(s => new SeguimientoResponse(s.Id, s.Texto, s.AutorNombre, s.CreadoEn))],
        a.CreadoEn);
}

public sealed record CrearAnotacionRequest(Guid EstudianteId, TipoAnotacion Tipo, DateOnly Fecha, string Descripcion, string? Compromisos);

public sealed record SeguimientoRequest(string Texto, EstadoCaso? Estado);

public sealed record AnotacionQuery(
    TipoAnotacion? Tipo = null, EstadoCaso? Estado = null, int? Grado = null, string? Grupo = null, string? Buscar = null,
    bool SoloSituaciones = false, int Pagina = 1, int TamanoPagina = 20);

/// <summary>Filtros del historial del observador de un curso en el campus del docente.</summary>
public sealed record HistorialCursoQuery(TipoAnotacion? Tipo = null, string? Buscar = null, int Pagina = 1, int TamanoPagina = 20);

public sealed record ConteoTipo(TipoAnotacion Tipo, int Total);

public sealed record ObservadorResponse(
    Guid EstudianteId, string NombreCompleto, string Documento, int? Grado, string? Grupo, IReadOnlyList<ConteoTipo> Resumen,
    IReadOnlyList<AnotacionResponse> Anotaciones);

public interface IConvivenciaRepository
{
    Task<Anotacion?> ObtenerAsync(Guid id, CancellationToken ct);

    Task<List<Anotacion>> DeEstudianteAsync(Guid estudianteId, CancellationToken ct);

    /// <summary>Página de anotaciones (con nombre del estudiante para la búsqueda).</summary>
    Task<PagedResult<Anotacion>> ListarAsync(AnotacionQuery query, CancellationToken ct);

    void Agregar(Anotacion anotacion);

    void Agregar(SeguimientoAnotacion seguimiento);
}

public sealed class CrearAnotacionValidator : AbstractValidator<CrearAnotacionRequest>
{
    public CrearAnotacionValidator()
    {
        RuleFor(x => x.EstudianteId).NotEmpty().WithMessage("Elija al estudiante.");
        RuleFor(x => x.Tipo).IsInEnum().WithMessage("Elija el tipo de anotación.");
        RuleFor(x => x.Descripcion).NotEmpty().WithMessage("Describa lo ocurrido.").MaximumLength(Anotacion.DescripcionMaxLength);
        RuleFor(x => x.Compromisos).MaximumLength(Anotacion.CompromisosMaxLength);
    }
}

public sealed class SeguimientoValidator : AbstractValidator<SeguimientoRequest>
{
    public SeguimientoValidator()
    {
        RuleFor(x => x.Texto).NotEmpty().WithMessage("Escriba el seguimiento.").MaximumLength(SeguimientoAnotacion.TextoMaxLength);
        RuleFor(x => x.Estado).IsInEnum();
    }
}

public interface IConvivenciaService
{
    Task<Result<ObservadorResponse>> ObservadorAsync(MiembroCampus actor, Guid estudianteId, CancellationToken ct);

    Task<Result<AnotacionResponse>> CrearDesdeCampusAsync(MiembroCampus docente, CrearAnotacionRequest request, CancellationToken ct);

    /// <summary>Historial del observador del curso de una clase del docente (todas las anotaciones del grado y grupo).</summary>
    Task<Result<PagedResult<AnotacionResponse>>> HistorialCursoAsync(
        MiembroCampus docente, Guid asignacionId, HistorialCursoQuery query, CancellationToken ct);

    Task<Result<AnotacionResponse>> MarcarEnteradoAsync(MiembroCampus acudiente, Guid anotacionId, CancellationToken ct);

    Task<PagedResult<AnotacionResponse>> ListarAsync(AnotacionQuery query, CancellationToken ct);

    Task<Result<AnotacionResponse>> ObtenerAsync(Guid id, CancellationToken ct);

    Task<Result<ObservadorResponse>> ObservadorAdminAsync(Guid estudianteId, CancellationToken ct);

    Task<Result<AnotacionResponse>> CrearDesdePanelAsync(string autor, CrearAnotacionRequest request, CancellationToken ct);

    Task<Result<AnotacionResponse>> AgregarSeguimientoAsync(Guid id, string autor, SeguimientoRequest request, CancellationToken ct);
}

/// <summary>Observador del estudiante y ruta de atención de convivencia escolar (Ley 1620 de 2013).</summary>
public sealed class ConvivenciaService(
    IConvivenciaRepository repository,
    IMiembroCampusRepository miembros,
    IAcademicoRepository academico,
    IAccesoEstudiantes acceso,
    INotificador notificador,
    IUnitOfWork unitOfWork,
    TimeProvider reloj,
    IValidator<CrearAnotacionRequest> crearValidator,
    IValidator<SeguimientoRequest> seguimientoValidator) : IConvivenciaService
{
    private static Error NoEncontrada(Guid id) => Error.NoEncontrado("Anotacion.NoEncontrada", $"No existe la anotación '{id}'.");

    public async Task<Result<ObservadorResponse>> ObservadorAsync(MiembroCampus actor, Guid estudianteId, CancellationToken ct)
    {
        var estudiante = await acceso.EstudianteVisibleAsync(actor, estudianteId, ct);
        return estudiante.IsSuccess ? await ArmarObservadorAsync(estudiante.Value, ct) : estudiante.Error;
    }

    public async Task<Result<AnotacionResponse>> CrearDesdeCampusAsync(MiembroCampus docente, CrearAnotacionRequest request, CancellationToken ct)
    {
        if (docente.Rol != RolCampus.Docente)
        {
            return CalificacionesService.SoloDocentes;
        }

        if (await crearValidator.ValidarAsync(request, ct) is { } invalido)
        {
            return invalido;
        }

        var estudiante = await acceso.EstudianteVisibleAsync(docente, request.EstudianteId, ct);
        return estudiante.IsSuccess ? await CrearAsync(estudiante.Value, request, docente.NombreCompleto, docente.Id, ct) : estudiante.Error;
    }

    public async Task<Result<PagedResult<AnotacionResponse>>> HistorialCursoAsync(
        MiembroCampus docente, Guid asignacionId, HistorialCursoQuery query, CancellationToken ct)
    {
        if (docente.Rol != RolCampus.Docente)
        {
            return CalificacionesService.SoloDocentes;
        }

        var asignacion = await academico.AsignacionAsync(asignacionId, ct);
        if (asignacion is null || asignacion.DocenteId != docente.Id)
        {
            return Error.NoEncontrado("Asignacion.NoEncontrada", "Esa clase no está en su carga académica.");
        }

        if (query.Tipo is { } tipo && !Enum.IsDefined(tipo))
        {
            return Error.Campo("Tipo", "Tipo de anotación no válido.");
        }

        // Si la asignación es de todo el grado (sin grupo), el historial incluye todos los grupos del grado.
        return await ListarAsync(
            new AnotacionQuery(
                Tipo: query.Tipo, Grado: asignacion.Grado, Grupo: asignacion.Grupo, Buscar: query.Buscar,
                Pagina: query.Pagina, TamanoPagina: query.TamanoPagina),
            ct);
    }

    public async Task<Result<AnotacionResponse>> MarcarEnteradoAsync(MiembroCampus acudiente, Guid anotacionId, CancellationToken ct)
    {
        var anotacion = await repository.ObtenerAsync(anotacionId, ct);
        if (anotacion is null || acudiente.Rol != RolCampus.Acudiente)
        {
            return NoEncontrada(anotacionId);
        }

        var estudiante = await acceso.EstudianteVisibleAsync(acudiente, anotacion.EstudianteId, ct);
        if (!estudiante.IsSuccess)
        {
            return NoEncontrada(anotacionId);
        }

        anotacion.MarcarEnterado(acudiente.NombreCompleto, reloj.GetUtcNow());
        await unitOfWork.SaveChangesAsync(ct);
        return AnotacionResponse.Desde(anotacion, estudiante.Value.NombreCompleto);
    }

    public async Task<PagedResult<AnotacionResponse>> ListarAsync(AnotacionQuery query, CancellationToken ct)
    {
        var normalizada = query with
        {
            Grupo = AcademicoService.Normalizar(query.Grupo),
            Pagina = Math.Max(1, query.Pagina),
            TamanoPagina = Math.Clamp(query.TamanoPagina, 1, 100),
        };
        var pagina = await repository.ListarAsync(normalizada, ct);
        var nombres = await NombresAsync(pagina.Items.Select(a => a.EstudianteId), ct);
        return pagina.Map(a => AnotacionResponse.Desde(a, nombres.GetValueOrDefault(a.EstudianteId, "(estudiante)")));
    }

    public async Task<Result<AnotacionResponse>> ObtenerAsync(Guid id, CancellationToken ct)
    {
        var anotacion = await repository.ObtenerAsync(id, ct);
        if (anotacion is null)
        {
            return NoEncontrada(id);
        }

        var nombres = await NombresAsync([anotacion.EstudianteId], ct);
        return AnotacionResponse.Desde(anotacion, nombres.GetValueOrDefault(anotacion.EstudianteId, "(estudiante)"));
    }

    public async Task<Result<ObservadorResponse>> ObservadorAdminAsync(Guid estudianteId, CancellationToken ct)
    {
        var estudiante = await miembros.ObtenerPorIdAsync(estudianteId, ct);
        return estudiante is { Rol: RolCampus.Estudiante }
            ? await ArmarObservadorAsync(estudiante, ct)
            : AccesoEstudiantes.NoVisible;
    }

    public async Task<Result<AnotacionResponse>> CrearDesdePanelAsync(string autor, CrearAnotacionRequest request, CancellationToken ct)
    {
        if (await crearValidator.ValidarAsync(request, ct) is { } invalido)
        {
            return invalido;
        }

        var estudiante = await miembros.ObtenerPorIdAsync(request.EstudianteId, ct);
        return estudiante is { Rol: RolCampus.Estudiante }
            ? await CrearAsync(estudiante, request, autor, null, ct)
            : AccesoEstudiantes.NoVisible;
    }

    public async Task<Result<AnotacionResponse>> AgregarSeguimientoAsync(Guid id, string autor, SeguimientoRequest request, CancellationToken ct)
    {
        if (await seguimientoValidator.ValidarAsync(request, ct) is { } invalido)
        {
            return invalido;
        }

        var anotacion = await repository.ObtenerAsync(id, ct);
        if (anotacion is null)
        {
            return NoEncontrada(id);
        }

        repository.Agregar(anotacion.AgregarSeguimiento(request.Texto, autor, request.Estado));
        await unitOfWork.SaveChangesAsync(ct);
        return await ObtenerAsync(id, ct);
    }

    private async Task<Result<AnotacionResponse>> CrearAsync(
        MiembroCampus estudiante, CrearAnotacionRequest request, string autor, Guid? autorMiembroId, CancellationToken ct)
    {
        var hoy = FechaColombia.Hoy(reloj);
        if (request.Fecha > hoy)
        {
            return Error.Campo("Fecha", "La fecha no puede ser futura.");
        }

        var anotacion = Anotacion.Crear(
            estudiante.Id, estudiante.Grado, estudiante.Grupo, request.Tipo, request.Fecha, request.Descripcion, request.Compromisos,
            autor, autorMiembroId);
        repository.Agregar(anotacion);

        var vinculos = await miembros.VinculosDeEstudiantesAsync([estudiante.Id], ct);
        var acudientes = (await miembros.ObtenerPorIdsAsync([.. vinculos.Select(v => v.AcudienteId)], ct)).Where(a => a.Activo).ToList();
        var positiva = request.Tipo == TipoAnotacion.Positiva;
        notificador.Notificar(
            acudientes, TipoAviso.Convivencia,
            positiva ? $"Felicitación para {estudiante.NombreCompleto}" : $"Nueva anotación en el observador de {estudiante.NombreCompleto}",
            positiva
                ? $"{autor} registró un reconocimiento para {estudiante.NombreCompleto}. Puede leerlo en el observador del campus."
                : $"{autor} registró una anotación ({NombreTipo(request.Tipo)}) el {request.Fecha:dd/MM/yyyy}. Léala en el campus y confirme que está enterado.",
            "/campus/observador", estudiante.Id, porCorreo: positiva || anotacion.EsSituacion);

        // El estudiante también se entera (campana del campus y notificación en su celular).
        notificador.Notificar(
            [estudiante], TipoAviso.Convivencia,
            positiva ? "¡Felicitaciones! Tiene un reconocimiento" : "Nueva anotación en su observador",
            positiva
                ? $"{autor} registró un reconocimiento en su observador. ¡Siga así!"
                : $"{autor} registró una anotación ({NombreTipo(request.Tipo)}) el {request.Fecha:dd/MM/yyyy}. Puede leerla en el observador del campus.",
            "/campus/observador", estudiante.Id, porCorreo: false);

        await unitOfWork.SaveChangesAsync(ct);
        return AnotacionResponse.Desde(anotacion, estudiante.NombreCompleto);
    }

    private async Task<ObservadorResponse> ArmarObservadorAsync(MiembroCampus estudiante, CancellationToken ct)
    {
        var anotaciones = await repository.DeEstudianteAsync(estudiante.Id, ct);
        return new ObservadorResponse(
            estudiante.Id, estudiante.NombreCompleto, estudiante.Documento, estudiante.Grado, estudiante.Grupo,
            [.. anotaciones.GroupBy(a => a.Tipo).OrderBy(g => g.Key).Select(g => new ConteoTipo(g.Key, g.Count()))],
            [.. anotaciones.OrderByDescending(a => a.Fecha).ThenByDescending(a => a.CreadoEn).Select(a => AnotacionResponse.Desde(a, estudiante.NombreCompleto))]);
    }

    private async Task<Dictionary<Guid, string>> NombresAsync(IEnumerable<Guid> ids, CancellationToken ct) =>
        (await miembros.ObtenerPorIdsAsync([.. ids.Distinct()], ct)).ToDictionary(m => m.Id, m => m.NombreCompleto);

    public static string NombreTipo(TipoAnotacion tipo) => tipo switch
    {
        TipoAnotacion.Positiva => "reconocimiento",
        TipoAnotacion.Observacion => "observación",
        TipoAnotacion.SituacionTipoI => "situación tipo I",
        TipoAnotacion.SituacionTipoII => "situación tipo II",
        _ => "situación tipo III",
    };
}
