using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Academico;
using RabiGamaliel.Application.Avisos;
using RabiGamaliel.Application.Campus;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Avisos;
using RabiGamaliel.Domain.Campus;
using RabiGamaliel.Domain.Mensajeria;

namespace RabiGamaliel.Application.Mensajeria;

public interface IMensajeriaRepository
{
    Task<List<Conversacion>> ConversacionesAsync(Guid miembroId, CancellationToken ct);

    Task<Conversacion?> ConversacionAsync(Guid id, CancellationToken ct);

    Task<List<Mensaje>> MensajesAsync(Guid conversacionId, CancellationToken ct);

    /// <summary>Último mensaje de cada conversación (para la bandeja).</summary>
    Task<Dictionary<Guid, string>> UltimosTextosAsync(IReadOnlyCollection<Guid> conversacionIds, CancellationToken ct);

    void Agregar(Conversacion conversacion);

    void Agregar(Mensaje mensaje);
}

public sealed record ContactoMensaje(Guid Id, string NombreCompleto, RolCampus Rol, string Detalle, Guid EstudianteId, string Estudiante);

public sealed record ConversacionResumen(
    Guid Id, string Asunto, Guid OtroId, string Otro, RolCampus OtroRol, Guid EstudianteId, string Estudiante, string Curso, DateTimeOffset UltimoMensajeEn,
    bool NoLeida, bool Cerrada, string? UltimoTexto);

public sealed record MensajeResponse(Guid Id, Guid AutorId, string Autor, bool Propio, string Texto, DateTimeOffset EnviadoEn);

public sealed record ConversacionResponse(ConversacionResumen Conversacion, IReadOnlyList<MensajeResponse> Mensajes);

public sealed record BandejaResponse(int NoLeidas, IReadOnlyList<ConversacionResumen> Conversaciones);

public sealed record IniciarConversacionRequest(Guid DestinatarioId, Guid EstudianteId, string Asunto, string Texto);

public sealed record EnviarMensajeRequest(string Texto);

public interface IMensajeriaService
{
    Task<BandejaResponse> BandejaAsync(MiembroCampus actor, CancellationToken ct);

    Task<IReadOnlyList<ContactoMensaje>> ContactosAsync(MiembroCampus actor, CancellationToken ct);

    Task<Result<ConversacionResponse>> IniciarAsync(MiembroCampus actor, IniciarConversacionRequest request, CancellationToken ct);

    Task<Result<ConversacionResponse>> LeerAsync(MiembroCampus actor, Guid id, CancellationToken ct);

    Task<Result<ConversacionResponse>> ResponderAsync(MiembroCampus actor, Guid id, EnviarMensajeRequest request, CancellationToken ct);

    Task<Result<ConversacionResponse>> CerrarAsync(MiembroCampus docente, Guid id, CancellationToken ct);
}

/// <summary>
/// Mensajería privada entre docentes y familias (acudientes o estudiantes) dentro del campus: cada conversación es
/// sobre un estudiante y solo con los docentes que le dictan clase este año.
/// </summary>
public sealed class MensajeriaService(
    IMensajeriaRepository repository,
    IAcademicoRepository academico,
    IMiembroCampusRepository miembros,
    IAccesoEstudiantes acceso,
    INotificador notificador,
    IUnitOfWork unitOfWork,
    TimeProvider reloj) : IMensajeriaService
{
    private static readonly Error NoEncontrada = Error.NoEncontrado("Conversacion.NoEncontrada", "No encontramos esa conversación.");

    public async Task<BandejaResponse> BandejaAsync(MiembroCampus actor, CancellationToken ct)
    {
        var conversaciones = await repository.ConversacionesAsync(actor.Id, ct);
        var resumenes = await ResumirAsync(actor, conversaciones, ct);
        return new BandejaResponse(resumenes.Count(r => r.NoLeida), resumenes);
    }

    public async Task<IReadOnlyList<ContactoMensaje>> ContactosAsync(MiembroCampus actor, CancellationToken ct)
    {
        var anio = FechaColombia.AnioLectivo(reloj);
        var asignaturas = (await academico.AsignaturasAsync(soloActivas: false, ct)).ToDictionary(a => a.Id, a => a.Nombre);
        var contactos = new List<ContactoMensaje>();

        if (actor.Rol == RolCampus.Docente)
        {
            var asignaciones = await academico.AsignacionesAsync(anio, actor.Id, null, null, ct);
            var estudiantes = new Dictionary<Guid, MiembroCampus>();
            foreach (var a in asignaciones)
            {
                foreach (var e in await miembros.EstudiantesDelCursoAsync(a.Grado, a.Grupo, ct))
                {
                    estudiantes.TryAdd(e.Id, e);
                }
            }

            var vinculos = estudiantes.Count == 0 ? [] : await miembros.VinculosDeEstudiantesAsync([.. estudiantes.Keys], ct);
            var acudientes = (await miembros.ObtenerPorIdsAsync([.. vinculos.Select(v => v.AcudienteId).Distinct()], ct))
                .Where(a => a.Activo).ToDictionary(a => a.Id);
            foreach (var e in estudiantes.Values.OrderBy(e => e.Grado).ThenBy(e => e.Grupo).ThenBy(e => e.NombreCompleto))
            {
                var curso = Familias.Curso(e.Grado, e.Grupo);
                foreach (var v in vinculos.Where(v => v.EstudianteId == e.Id && acudientes.ContainsKey(v.AcudienteId)))
                {
                    var acudiente = acudientes[v.AcudienteId];
                    contactos.Add(new ContactoMensaje(
                        acudiente.Id, acudiente.NombreCompleto, RolCampus.Acudiente, $"{v.Parentesco ?? "Acudiente"} de {e.NombreCompleto} ({curso})", e.Id, e.NombreCompleto));
                }

                contactos.Add(new ContactoMensaje(e.Id, e.NombreCompleto, RolCampus.Estudiante, $"Estudiante de {curso}", e.Id, e.NombreCompleto));
            }

            return contactos;
        }

        foreach (var estudiante in await acceso.EstudiantesPropiosAsync(actor, ct))
        {
            if (estudiante.Grado is not { } grado)
            {
                continue;
            }

            var asignaciones = (await academico.AsignacionesAsync(anio, null, grado, estudiante.Grupo, ct))
                .Where(a => a.Incluye(grado, estudiante.Grupo))
                .ToList();
            var docentes = (await miembros.ObtenerPorIdsAsync([.. asignaciones.Select(a => a.DocenteId).Distinct()], ct)).Where(d => d.Activo);
            foreach (var docente in docentes.OrderBy(d => d.NombreCompleto))
            {
                var materias = asignaciones.Where(a => a.DocenteId == docente.Id).Select(a => asignaturas.GetValueOrDefault(a.AsignaturaId, "(asignatura)")).Distinct();
                contactos.Add(new ContactoMensaje(
                    docente.Id, docente.NombreCompleto, RolCampus.Docente, string.Join(", ", materias), estudiante.Id, estudiante.NombreCompleto));
            }
        }

        return contactos;
    }

    public async Task<Result<ConversacionResponse>> IniciarAsync(MiembroCampus actor, IniciarConversacionRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Asunto) || request.Asunto.Trim().Length > Conversacion.AsuntoMaxLength)
        {
            return Error.Campo("Asunto", $"Escriba el asunto (máximo {Conversacion.AsuntoMaxLength} caracteres).");
        }

        if (Validar(request.Texto) is { } invalido)
        {
            return invalido;
        }

        var contacto = (await ContactosAsync(actor, ct)).FirstOrDefault(c => c.Id == request.DestinatarioId && c.EstudianteId == request.EstudianteId);
        if (contacto is null)
        {
            return Error.Campo("DestinatarioId", "Solo puede escribir a los docentes del estudiante o a las familias de sus cursos.");
        }

        var conversacion = actor.Rol == RolCampus.Docente
            ? Conversacion.Crear(actor.Id, contacto.Id, request.EstudianteId, request.Asunto)
            : Conversacion.Crear(contacto.Id, actor.Id, request.EstudianteId, request.Asunto);
        repository.Agregar(conversacion);
        await AgregarMensajeAsync(actor, conversacion, request.Texto, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return await ArmarAsync(actor, conversacion, ct);
    }

    public async Task<Result<ConversacionResponse>> LeerAsync(MiembroCampus actor, Guid id, CancellationToken ct)
    {
        var conversacion = await repository.ConversacionAsync(id, ct);
        if (conversacion is null || !conversacion.Participa(actor.Id))
        {
            return NoEncontrada;
        }

        if (conversacion.NoLeida(actor.Id))
        {
            conversacion.MarcarLeida(actor.Id, reloj.GetUtcNow());
            await unitOfWork.SaveChangesAsync(ct);
        }

        return await ArmarAsync(actor, conversacion, ct);
    }

    public async Task<Result<ConversacionResponse>> ResponderAsync(MiembroCampus actor, Guid id, EnviarMensajeRequest request, CancellationToken ct)
    {
        var conversacion = await repository.ConversacionAsync(id, ct);
        if (conversacion is null || !conversacion.Participa(actor.Id))
        {
            return NoEncontrada;
        }

        if (conversacion.Cerrada && actor.Id != conversacion.DocenteId)
        {
            return Error.Conflicto("Conversacion.Cerrada", "El docente cerró esta conversación. Si necesita algo más, inicie una nueva.");
        }

        if (Validar(request.Texto) is { } invalido)
        {
            return invalido;
        }

        await AgregarMensajeAsync(actor, conversacion, request.Texto, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return await ArmarAsync(actor, conversacion, ct);
    }

    public async Task<Result<ConversacionResponse>> CerrarAsync(MiembroCampus docente, Guid id, CancellationToken ct)
    {
        var conversacion = await repository.ConversacionAsync(id, ct);
        if (conversacion is null || conversacion.DocenteId != docente.Id)
        {
            return NoEncontrada;
        }

        conversacion.Cerrar();
        await unitOfWork.SaveChangesAsync(ct);
        return await ArmarAsync(docente, conversacion, ct);
    }

    // ---------- Apoyo ----------

    private static Error? Validar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) || texto.Trim().Length > Mensaje.TextoMaxLength
            ? Error.Campo("Texto", $"Escriba el mensaje (máximo {Mensaje.TextoMaxLength} caracteres).")
            : null;

    private async Task AgregarMensajeAsync(MiembroCampus autor, Conversacion conversacion, string texto, CancellationToken ct)
    {
        repository.Agregar(Mensaje.Crear(conversacion.Id, autor.Id, texto));
        conversacion.RegistrarMensaje(autor.Id, reloj.GetUtcNow());
        if (await miembros.ObtenerPorIdAsync(conversacion.Otro(autor.Id), ct) is { Activo: true } otro)
        {
            var resumen = texto.Trim();
            notificador.Notificar(
                [otro], TipoAviso.Mensaje, $"Mensaje de {autor.NombreCompleto}",
                $"{conversacion.Asunto}: {(resumen.Length > 180 ? resumen[..180] + "…" : resumen)}",
                $"/campus/mensajes?c={conversacion.Id}", conversacion.EstudianteId, porCorreo: false);
        }
    }

    private async Task<List<ConversacionResumen>> ResumirAsync(MiembroCampus actor, List<Conversacion> conversaciones, CancellationToken ct)
    {
        if (conversaciones.Count == 0)
        {
            return [];
        }

        var ids = conversaciones.SelectMany(c => new[] { c.DocenteId, c.FamiliaId, c.EstudianteId }).Distinct().ToList();
        var personas = (await miembros.ObtenerPorIdsAsync(ids, ct)).ToDictionary(m => m.Id);
        var ultimos = await repository.UltimosTextosAsync([.. conversaciones.Select(c => c.Id)], ct);
        return
        [
            .. conversaciones.OrderByDescending(c => c.UltimoMensajeEn).Select(c =>
            {
                var otro = personas.GetValueOrDefault(c.Otro(actor.Id));
                var estudiante = personas.GetValueOrDefault(c.EstudianteId);
                return new ConversacionResumen(
                    c.Id, c.Asunto, c.Otro(actor.Id), otro?.NombreCompleto ?? "(usuario)", otro?.Rol ?? RolCampus.Docente, c.EstudianteId,
                    estudiante?.NombreCompleto ?? "(estudiante)", Familias.Curso(estudiante?.Grado, estudiante?.Grupo), c.UltimoMensajeEn,
                    c.NoLeida(actor.Id), c.Cerrada, ultimos.GetValueOrDefault(c.Id));
            }),
        ];
    }

    private async Task<ConversacionResponse> ArmarAsync(MiembroCampus actor, Conversacion conversacion, CancellationToken ct)
    {
        var resumen = (await ResumirAsync(actor, [conversacion], ct))[0];
        var nombres = new Dictionary<Guid, string> { [actor.Id] = actor.NombreCompleto, [resumen.OtroId] = resumen.Otro };
        var mensajes = await repository.MensajesAsync(conversacion.Id, ct);
        return new ConversacionResponse(
            resumen,
            [.. mensajes.OrderBy(m => m.CreadoEn).Select(m => new MensajeResponse(m.Id, m.AutorId, nombres.GetValueOrDefault(m.AutorId, string.Empty), m.AutorId == actor.Id, m.Texto, m.CreadoEn))]);
    }
}
