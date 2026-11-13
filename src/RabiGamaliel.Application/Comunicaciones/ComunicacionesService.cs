using FluentValidation;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Comunicaciones;

namespace RabiGamaliel.Application.Comunicaciones;

public sealed record CircularResponse(
    Guid Id, string Codigo, int Anio, int Numero, string Titulo, string Contenido, PublicoCircular Publico, IReadOnlyList<int> Grados,
    bool RequiereConfirmacion, string? EnlaceAdjunto, DateTimeOffset? PublicadaEn, DateTimeOffset CreadoEn, int Destinatarios, int Leidas,
    int Confirmadas);

public sealed record GuardarCircularRequest(
    string Titulo, string Contenido, PublicoCircular Publico, IReadOnlyList<int>? Grados, bool RequiereConfirmacion, string? EnlaceAdjunto);

public sealed record PendienteCircular(Guid MiembroId, string NombreCompleto, RolCampus Rol, string Curso, string? Telefono, bool Leida);

public sealed record CircularCampusResponse(
    Guid Id, string Codigo, string Titulo, string Contenido, bool RequiereConfirmacion, string? EnlaceAdjunto, DateTimeOffset PublicadaEn,
    bool Leida, bool Confirmada);

public sealed record EventoResponse(
    Guid Id, string Titulo, string? Descripcion, DateOnly Inicio, DateOnly Fin, string? Hora, string? Lugar, TipoEvento Tipo, bool Publico)
{
    public static EventoResponse Desde(EventoCalendario e) => new(e.Id, e.Titulo, e.Descripcion, e.Inicio, e.Fin, e.Hora, e.Lugar, e.Tipo, e.Publico);
}

public sealed record GuardarEventoRequest(
    string Titulo, string? Descripcion, DateOnly Inicio, DateOnly? Fin, string? Hora, string? Lugar, TipoEvento Tipo, bool Publico);

public sealed record ConteoLecturas(Guid CircularId, int Leidas, int Confirmadas);

public interface IComunicacionesRepository
{
    Task<List<Circular>> CircularesAsync(bool soloPublicadas, CancellationToken ct);

    Task<Circular?> CircularAsync(Guid id, CancellationToken ct);

    Task<int> SiguienteNumeroAsync(int anio, CancellationToken ct);

    void Agregar(Circular circular);

    void Eliminar(Circular circular);

    Task<List<ConteoLecturas>> ConteoLecturasAsync(CancellationToken ct);

    Task<List<LecturaCircular>> LecturasAsync(Guid circularId, CancellationToken ct);

    Task<List<LecturaCircular>> LecturasDeMiembroAsync(Guid miembroId, CancellationToken ct);

    void Agregar(LecturaCircular lectura);

    Task<List<EventoCalendario>> EventosAsync(DateOnly desde, DateOnly hasta, bool soloPublicos, CancellationToken ct);

    Task<EventoCalendario?> EventoAsync(Guid id, CancellationToken ct);

    void Agregar(EventoCalendario evento);

    void Eliminar(EventoCalendario evento);
}

public sealed class GuardarCircularValidator : AbstractValidator<GuardarCircularRequest>
{
    public GuardarCircularValidator()
    {
        RuleFor(x => x.Titulo).NotEmpty().WithMessage("Escriba el asunto de la circular.").MaximumLength(Circular.TituloMaxLength);
        RuleFor(x => x.Contenido).NotEmpty().WithMessage("Escriba el contenido.").MaximumLength(Circular.ContenidoMaxLength);
        RuleFor(x => x.Publico).IsInEnum().WithMessage("Elija a quién va dirigida.");
        RuleForEach(x => x.Grados).Must(MiembroCampus.GradoValido).WithMessage("Los grados van de transición a 11°.");
        RuleFor(x => x.EnlaceAdjunto).MaximumLength(Circular.EnlaceMaxLength)
            .Must(u => string.IsNullOrWhiteSpace(u) || Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
            .WithMessage("El adjunto debe ser un enlace https.");
    }
}

public sealed class GuardarEventoValidator : AbstractValidator<GuardarEventoRequest>
{
    public GuardarEventoValidator()
    {
        RuleFor(x => x.Titulo).NotEmpty().WithMessage("Escriba el nombre del evento.").MaximumLength(EventoCalendario.TituloMaxLength);
        RuleFor(x => x.Descripcion).MaximumLength(EventoCalendario.DescripcionMaxLength);
        RuleFor(x => x.Hora).MaximumLength(EventoCalendario.HoraMaxLength);
        RuleFor(x => x.Lugar).MaximumLength(EventoCalendario.LugarMaxLength);
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.Fin).Must((x, fin) => fin is null || fin >= x.Inicio).WithMessage("El evento debe terminar después de empezar.");
    }
}

public interface IComunicacionesService
{
    Task<IReadOnlyList<CircularResponse>> CircularesAsync(CancellationToken ct);

    Task<Result<CircularResponse>> CircularAsync(Guid id, CancellationToken ct);

    Task<Result<CircularResponse>> CrearCircularAsync(GuardarCircularRequest request, CancellationToken ct);

    Task<Result<CircularResponse>> ActualizarCircularAsync(Guid id, GuardarCircularRequest request, CancellationToken ct);

    Task<Result<CircularResponse>> PublicarAsync(Guid id, CancellationToken ct);

    Task<Result<Unit>> EliminarCircularAsync(Guid id, CancellationToken ct);

    Task<Result<IReadOnlyList<PendienteCircular>>> PendientesAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<CircularCampusResponse>> MisCircularesAsync(MiembroCampus miembro, CancellationToken ct);

    Task<Result<CircularCampusResponse>> LeerAsync(MiembroCampus miembro, Guid id, bool confirmar, CancellationToken ct);

    Task<IReadOnlyList<EventoResponse>> EventosAsync(DateOnly desde, DateOnly hasta, bool soloPublicos, CancellationToken ct);

    Task<Result<EventoResponse>> CrearEventoAsync(GuardarEventoRequest request, CancellationToken ct);

    Task<Result<EventoResponse>> ActualizarEventoAsync(Guid id, GuardarEventoRequest request, CancellationToken ct);

    Task<Result<Unit>> EliminarEventoAsync(Guid id, CancellationToken ct);
}

/// <summary>Circulares con confirmación de lectura y calendario escolar.</summary>
public sealed class ComunicacionesService(
    IComunicacionesRepository repository,
    IMiembroCampusRepository miembros,
    IColaCorreos correos,
    IUnitOfWork unitOfWork,
    TimeProvider reloj,
    IValidator<GuardarCircularRequest> circularValidator,
    IValidator<GuardarEventoRequest> eventoValidator) : IComunicacionesService
{
    private const int MaximoDiasCalendario = 400;

    private static Error CircularNoEncontrada(Guid id) => Error.NoEncontrado("Circular.NoEncontrada", $"No existe la circular '{id}'.");

    private static Error EventoNoEncontrado(Guid id) => Error.NoEncontrado("Evento.NoEncontrado", $"No existe el evento '{id}'.");

    public async Task<IReadOnlyList<CircularResponse>> CircularesAsync(CancellationToken ct)
    {
        var circulares = await repository.CircularesAsync(soloPublicadas: false, ct);
        var lecturas = (await repository.ConteoLecturasAsync(ct)).ToDictionary(l => l.CircularId);
        var poblacion = await PoblacionAsync(ct);
        return [.. circulares.Select(c => Responder(c, poblacion, lecturas.GetValueOrDefault(c.Id)))];
    }

    public async Task<Result<CircularResponse>> CircularAsync(Guid id, CancellationToken ct)
    {
        var circular = await repository.CircularAsync(id, ct);
        if (circular is null)
        {
            return CircularNoEncontrada(id);
        }

        var lecturas = (await repository.ConteoLecturasAsync(ct)).FirstOrDefault(l => l.CircularId == id);
        return Responder(circular, await PoblacionAsync(ct), lecturas);
    }

    public async Task<Result<CircularResponse>> CrearCircularAsync(GuardarCircularRequest request, CancellationToken ct)
    {
        if (await circularValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var anio = FechaColombia.AnioLectivo(reloj);
        var circular = Circular.Crear(
            anio, await repository.SiguienteNumeroAsync(anio, ct), request.Titulo, request.Contenido, request.Publico, request.Grados ?? [],
            request.RequiereConfirmacion, request.EnlaceAdjunto);
        repository.Agregar(circular);
        await unitOfWork.SaveChangesAsync(ct);
        return Responder(circular, await PoblacionAsync(ct), null);
    }

    public async Task<Result<CircularResponse>> ActualizarCircularAsync(Guid id, GuardarCircularRequest request, CancellationToken ct)
    {
        if (await circularValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var circular = await repository.CircularAsync(id, ct);
        if (circular is null)
        {
            return CircularNoEncontrada(id);
        }

        circular.Actualizar(request.Titulo, request.Contenido, request.Publico, request.Grados ?? [], request.RequiereConfirmacion, request.EnlaceAdjunto);
        await unitOfWork.SaveChangesAsync(ct);
        return await CircularAsync(id, ct);
    }

    public async Task<Result<CircularResponse>> PublicarAsync(Guid id, CancellationToken ct)
    {
        var circular = await repository.CircularAsync(id, ct);
        if (circular is null)
        {
            return CircularNoEncontrada(id);
        }

        var primeraVez = circular.PublicadaEn is null;
        circular.Publicar(reloj.GetUtcNow());
        await unitOfWork.SaveChangesAsync(ct);

        if (primeraVez)
        {
            var poblacion = await PoblacionAsync(ct);
            foreach (var destinatario in Destinatarios(circular, poblacion).Where(d => d.Miembro.Correo is not null))
            {
                correos.Encolar(new CorreoSaliente(
                    destinatario.Miembro.Correo!,
                    $"{circular.Codigo}: {circular.Titulo}",
                    $"Hola, {destinatario.Miembro.NombreCompleto}:\n\n{circular.Contenido}" +
                    (circular.EnlaceAdjunto is { } enlace ? $"\n\nDocumento adjunto: {enlace}" : string.Empty) +
                    (circular.RequiereConfirmacion ? "\n\nPor favor confirme la lectura en el campus virtual." : string.Empty) +
                    Notificador.PieCorreo));
            }
        }

        return await CircularAsync(id, ct);
    }

    public async Task<Result<Unit>> EliminarCircularAsync(Guid id, CancellationToken ct)
    {
        var circular = await repository.CircularAsync(id, ct);
        if (circular is null)
        {
            return CircularNoEncontrada(id);
        }

        repository.Eliminar(circular);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    public async Task<Result<IReadOnlyList<PendienteCircular>>> PendientesAsync(Guid id, CancellationToken ct)
    {
        var circular = await repository.CircularAsync(id, ct);
        if (circular is null)
        {
            return CircularNoEncontrada(id);
        }

        var lecturas = (await repository.LecturasAsync(id, ct)).ToDictionary(l => l.MiembroId);
        var poblacion = await PoblacionAsync(ct);
        return Destinatarios(circular, poblacion)
            .Where(d => !lecturas.TryGetValue(d.Miembro.Id, out var l) || (circular.RequiereConfirmacion && l.ConfirmadaEn is null))
            .Select(d => new PendienteCircular(
                d.Miembro.Id, d.Miembro.NombreCompleto, d.Miembro.Rol, d.Curso, d.Miembro.Telefono, lecturas.ContainsKey(d.Miembro.Id)))
            .OrderBy(p => p.Curso).ThenBy(p => p.NombreCompleto)
            .ToList();
    }

    public async Task<IReadOnlyList<CircularCampusResponse>> MisCircularesAsync(MiembroCampus miembro, CancellationToken ct)
    {
        var grados = await GradosDeAsync(miembro, ct);
        var lecturas = (await repository.LecturasDeMiembroAsync(miembro.Id, ct)).ToDictionary(l => l.CircularId);
        return
        [
            .. (await repository.CircularesAsync(soloPublicadas: true, ct))
                .Where(c => c.EsPara(miembro.Rol) && c.AplicaAGrados(miembro.Rol, grados))
                .OrderByDescending(c => c.PublicadaEn)
                .Select(c => ParaCampus(c, lecturas.GetValueOrDefault(c.Id))),
        ];
    }

    public async Task<Result<CircularCampusResponse>> LeerAsync(MiembroCampus miembro, Guid id, bool confirmar, CancellationToken ct)
    {
        var circular = await repository.CircularAsync(id, ct);
        if (circular?.PublicadaEn is null || !circular.EsPara(miembro.Rol) || !circular.AplicaAGrados(miembro.Rol, await GradosDeAsync(miembro, ct)))
        {
            return CircularNoEncontrada(id);
        }

        var ahora = reloj.GetUtcNow();
        var lectura = (await repository.LecturasDeMiembroAsync(miembro.Id, ct)).FirstOrDefault(l => l.CircularId == id);
        if (lectura is null)
        {
            lectura = LecturaCircular.Registrar(id, miembro.Id, ahora);
            repository.Agregar(lectura);
        }

        if (confirmar)
        {
            lectura.Confirmar(ahora);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return ParaCampus(circular, lectura);
    }

    public async Task<IReadOnlyList<EventoResponse>> EventosAsync(DateOnly desde, DateOnly hasta, bool soloPublicos, CancellationToken ct)
    {
        if (hasta < desde || hasta.DayNumber - desde.DayNumber > MaximoDiasCalendario)
        {
            hasta = desde.AddDays(MaximoDiasCalendario);
        }

        return [.. (await repository.EventosAsync(desde, hasta, soloPublicos, ct)).Select(EventoResponse.Desde)];
    }

    public async Task<Result<EventoResponse>> CrearEventoAsync(GuardarEventoRequest request, CancellationToken ct)
    {
        if (await eventoValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var evento = EventoCalendario.Crear(
            request.Titulo, request.Descripcion, request.Inicio, request.Fin, request.Hora, request.Lugar, request.Tipo, request.Publico);
        repository.Agregar(evento);
        await unitOfWork.SaveChangesAsync(ct);
        return EventoResponse.Desde(evento);
    }

    public async Task<Result<EventoResponse>> ActualizarEventoAsync(Guid id, GuardarEventoRequest request, CancellationToken ct)
    {
        if (await eventoValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var evento = await repository.EventoAsync(id, ct);
        if (evento is null)
        {
            return EventoNoEncontrado(id);
        }

        evento.Actualizar(request.Titulo, request.Descripcion, request.Inicio, request.Fin, request.Hora, request.Lugar, request.Tipo, request.Publico);
        await unitOfWork.SaveChangesAsync(ct);
        return EventoResponse.Desde(evento);
    }

    public async Task<Result<Unit>> EliminarEventoAsync(Guid id, CancellationToken ct)
    {
        var evento = await repository.EventoAsync(id, ct);
        if (evento is null)
        {
            return EventoNoEncontrado(id);
        }

        repository.Eliminar(evento);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }

    // ---------- Destinatarios ----------

    private sealed record Destinatario(MiembroCampus Miembro, IReadOnlyCollection<int> Grados, string Curso);

    private sealed record Poblacion(IReadOnlyList<Destinatario> Miembros);

    /// <summary>Miembros activos con los grados que les corresponden (los de sus estudiantes, para los acudientes).</summary>
    private async Task<Poblacion> PoblacionAsync(CancellationToken ct)
    {
        var estudiantes = await miembros.ActivosPorRolAsync(RolCampus.Estudiante, ct);
        var docentes = await miembros.ActivosPorRolAsync(RolCampus.Docente, ct);
        var acudientes = await miembros.ActivosPorRolAsync(RolCampus.Acudiente, ct);
        var porId = estudiantes.ToDictionary(e => e.Id);
        var vinculos = (await miembros.VinculosDeEstudiantesAsync(null, ct)).ToLookup(v => v.AcudienteId);

        var lista = new List<Destinatario>(estudiantes.Count + docentes.Count + acudientes.Count);
        lista.AddRange(estudiantes.Select(e => new Destinatario(e, [e.Grado!.Value], Curso(e))));
        lista.AddRange(docentes.Select(d => new Destinatario(d, [], "Docentes")));
        foreach (var acudiente in acudientes)
        {
            var hijos = vinculos[acudiente.Id].Select(v => porId.GetValueOrDefault(v.EstudianteId)).Where(e => e is not null).Select(e => e!).ToList();
            lista.Add(new Destinatario(
                acudiente, [.. hijos.Select(h => h.Grado!.Value).Distinct()],
                hijos.Count == 0 ? "Acudientes sin estudiante" : "Acudiente de " + string.Join(", ", hijos.Select(Curso).Distinct())));
        }

        return new Poblacion(lista);
    }

    private static IEnumerable<Destinatario> Destinatarios(Circular circular, Poblacion poblacion) =>
        poblacion.Miembros.Where(d => circular.EsPara(d.Miembro.Rol) && circular.AplicaAGrados(d.Miembro.Rol, d.Grados));

    private static CircularResponse Responder(Circular c, Poblacion poblacion, ConteoLecturas? lecturas) => new(
        c.Id, c.Codigo, c.Anio, c.Numero, c.Titulo, c.Contenido, c.Publico, c.Grados, c.RequiereConfirmacion, c.EnlaceAdjunto, c.PublicadaEn,
        c.CreadoEn, Destinatarios(c, poblacion).Count(), lecturas?.Leidas ?? 0, lecturas?.Confirmadas ?? 0);

    private static CircularCampusResponse ParaCampus(Circular c, LecturaCircular? lectura) => new(
        c.Id, c.Codigo, c.Titulo, c.Contenido, c.RequiereConfirmacion, c.EnlaceAdjunto, c.PublicadaEn!.Value, lectura is not null,
        lectura?.ConfirmadaEn is not null);

    private async Task<IReadOnlyCollection<int>> GradosDeAsync(MiembroCampus miembro, CancellationToken ct)
    {
        if (miembro.Rol == RolCampus.Estudiante)
        {
            return miembro.Grado is { } g ? [g] : [];
        }

        if (miembro.Rol != RolCampus.Acudiente)
        {
            return [];
        }

        var ids = (await miembros.VinculosDeAcudienteAsync(miembro.Id, ct)).Select(v => v.EstudianteId).ToList();
        return [.. (await miembros.ObtenerPorIdsAsync(ids, ct)).Where(e => e.Grado is not null).Select(e => e.Grado!.Value).Distinct()];
    }

    private static string Curso(MiembroCampus e) =>
        string.Join(' ', new[] { e.Grado is { } g ? GradosEscolares.Nombre(g) : null, e.Grupo }.Where(x => x is not null));
}
