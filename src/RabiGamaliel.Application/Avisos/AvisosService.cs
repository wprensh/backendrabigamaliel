using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Avisos;
using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Avisos;

public interface IAvisoRepository
{
    void Agregar(Aviso aviso);

    Task<List<Aviso>> DeMiembroAsync(Guid miembroId, int maximo, CancellationToken ct);

    Task<int> NoLeidosAsync(Guid miembroId, CancellationToken ct);

    Task<List<Aviso>> NoLeidosParaMarcarAsync(Guid miembroId, IReadOnlyCollection<Guid>? ids, CancellationToken ct);

    /// <summary>¿Ya se avisó hoy a ese destinatario sobre ese estudiante? Evita un aviso por cada clase perdida.</summary>
    Task<bool> ExisteDesdeAsync(Guid destinatarioId, Guid estudianteId, TipoAviso tipo, DateTimeOffset desde, CancellationToken ct);
}

public sealed record AvisoResponse(
    Guid Id, TipoAviso Tipo, string Titulo, string Mensaje, string? Enlace, Guid? EstudianteId, bool Leido, DateTimeOffset CreadoEn)
{
    public static AvisoResponse Desde(Aviso a) => new(a.Id, a.Tipo, a.Titulo, a.Mensaje, a.Enlace, a.EstudianteId, a.LeidoEn is not null, a.CreadoEn);
}

public sealed record AvisosResponse(int NoLeidos, IReadOnlyList<AvisoResponse> Items);

/// <summary>Cuerpo de POST /api/campus/avisos/leidos; sin ids marca todos.</summary>
public sealed record MarcarAvisosRequest(IReadOnlyList<Guid>? Ids);

/// <summary>Crea avisos en el campus y, si se pide y hay correo, los envía por email.</summary>
public interface INotificador
{
    void Notificar(
        IEnumerable<MiembroCampus> destinatarios, TipoAviso tipo, string titulo, string mensaje, string? enlace, Guid? estudianteId, bool porCorreo);
}

public sealed class Notificador(IAvisoRepository avisos, IColaCorreos correos, IColaPush? push = null) : INotificador
{
    public const string PieCorreo =
        "\n\n— Fundación Educativa Rabí Gamaliel\nIngrese al campus virtual con su documento y su clave de acceso para ver el detalle.";

    public void Notificar(
        IEnumerable<MiembroCampus> destinatarios, TipoAviso tipo, string titulo, string mensaje, string? enlace, Guid? estudianteId, bool porCorreo)
    {
        var ids = new List<Guid>();
        foreach (var destinatario in destinatarios.DistinctBy(d => d.Id))
        {
            ids.Add(destinatario.Id);
            avisos.Agregar(Aviso.Crear(destinatario.Id, tipo, titulo, mensaje, enlace, estudianteId));
            if (porCorreo && destinatario.Correo is { Length: > 0 } correo)
            {
                correos.Encolar(new CorreoSaliente(correo, titulo, $"Hola, {destinatario.NombreCompleto}:\n\n{mensaje}{PieCorreo}"));
            }
        }

        // También llega como notificación al celular de quien instaló la app y activó las notificaciones.
        if (ids.Count > 0 && push is { Habilitado: true })
        {
            push.Encolar(new PushSaliente(ids, titulo, mensaje, enlace));
        }
    }
}

public interface IAvisosService
{
    Task<AvisosResponse> MisAvisosAsync(MiembroCampus miembro, CancellationToken ct);

    Task<Result<Unit>> MarcarLeidosAsync(MiembroCampus miembro, MarcarAvisosRequest request, CancellationToken ct);
}

public sealed class AvisosService(IAvisoRepository repository, IUnitOfWork unitOfWork, TimeProvider reloj) : IAvisosService
{
    private const int Visibles = 40;

    public async Task<AvisosResponse> MisAvisosAsync(MiembroCampus miembro, CancellationToken ct)
    {
        var items = await repository.DeMiembroAsync(miembro.Id, Visibles, ct);
        return new AvisosResponse(await repository.NoLeidosAsync(miembro.Id, ct), [.. items.Select(AvisoResponse.Desde)]);
    }

    public async Task<Result<Unit>> MarcarLeidosAsync(MiembroCampus miembro, MarcarAvisosRequest request, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow();
        foreach (var aviso in await repository.NoLeidosParaMarcarAsync(miembro.Id, request.Ids, ct))
        {
            aviso.MarcarLeido(ahora);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
