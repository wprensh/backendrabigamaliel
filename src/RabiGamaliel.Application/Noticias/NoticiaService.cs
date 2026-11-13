using FluentValidation;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Noticias;

namespace RabiGamaliel.Application.Noticias;

public interface INoticiaService
{
    Task<PagedResult<NoticiaResponse>> ListarAsync(NoticiaQuery query, CancellationToken ct);

    Task<IReadOnlyList<ArchivoNoticiasItem>> ArchivoAsync(bool soloPublicadas, CancellationToken ct);

    Task<Result<NoticiaResponse>> ObtenerAsync(Guid id, CancellationToken ct);

    Task<Result<NoticiaResponse>> CrearAsync(GuardarNoticiaRequest request, CancellationToken ct);

    Task<Result<NoticiaResponse>> ActualizarAsync(Guid id, GuardarNoticiaRequest request, CancellationToken ct);

    Task<Result<Unit>> EliminarAsync(Guid id, CancellationToken ct);
}

/// <summary>
/// Casos de uso del CRUD de noticias.
/// Usa un constructor primario (C# 12+) para recibir sus dependencias por inyección.
/// </summary>
public sealed class NoticiaService(
    INoticiaRepository repository,
    IUnitOfWork unitOfWork,
    IValidator<GuardarNoticiaRequest> validator) : INoticiaService
{
    private static Error NoEncontrada(Guid id) =>
        Error.NoEncontrado("Noticia.NoEncontrada", $"No existe una noticia con id '{id}'.");

    private static Error TituloDuplicado(string titulo) =>
        Error.Conflicto("Noticia.TituloDuplicado", $"Ya existe una noticia con el título '{titulo}'.");

    public async Task<PagedResult<NoticiaResponse>> ListarAsync(NoticiaQuery query, CancellationToken ct)
    {
        // Normaliza la paginación para no permitir páginas negativas ni tamaños excesivos.
        var normalizada = query with
        {
            Pagina = Math.Max(1, query.Pagina),
            TamanoPagina = Math.Clamp(query.TamanoPagina, 1, NoticiaQuery.TamanoPaginaMaximo),
        };

        // Un rango invertido ("desde" posterior a "hasta") se interpreta en el orden correcto.
        if (normalizada is { Desde: { } desde, Hasta: { } hasta } && desde > hasta)
        {
            normalizada = normalizada with { Desde = hasta, Hasta = desde };
        }

        var pagina = await repository.ListarAsync(normalizada, ct);
        return pagina.Map(NoticiaResponse.Desde);
    }

    public Task<IReadOnlyList<ArchivoNoticiasItem>> ArchivoAsync(bool soloPublicadas, CancellationToken ct) =>
        repository.ArchivoAsync(soloPublicadas, ct);

    public async Task<Result<NoticiaResponse>> ObtenerAsync(Guid id, CancellationToken ct)
    {
        var noticia = await repository.ObtenerPorIdAsync(id, ct);
        return noticia is null ? NoEncontrada(id) : NoticiaResponse.Desde(noticia);
    }

    public async Task<Result<NoticiaResponse>> CrearAsync(GuardarNoticiaRequest request, CancellationToken ct)
    {
        if (await validator.ValidarAsync(request, ct) is { } errorValidacion)
        {
            return errorValidacion;
        }

        if (await repository.ExisteTituloAsync(request.Titulo, excluirId: null, ct))
        {
            return TituloDuplicado(request.Titulo);
        }

        var noticia = Noticia.Crear(
            request.Titulo, request.Resumen, request.Contenido ?? string.Empty,
            request.Categoria, request.FechaPublicacion, request.ImagenUrl, request.Publicada);

        repository.Agregar(noticia);
        await unitOfWork.SaveChangesAsync(ct);

        return NoticiaResponse.Desde(noticia);
    }

    public async Task<Result<NoticiaResponse>> ActualizarAsync(Guid id, GuardarNoticiaRequest request, CancellationToken ct)
    {
        if (await validator.ValidarAsync(request, ct) is { } errorValidacion)
        {
            return errorValidacion;
        }

        var noticia = await repository.ObtenerPorIdAsync(id, ct);
        if (noticia is null)
        {
            return NoEncontrada(id);
        }

        if (await repository.ExisteTituloAsync(request.Titulo, excluirId: id, ct))
        {
            return TituloDuplicado(request.Titulo);
        }

        noticia.Actualizar(
            request.Titulo, request.Resumen, request.Contenido ?? string.Empty,
            request.Categoria, request.FechaPublicacion, request.ImagenUrl, request.Publicada);

        await unitOfWork.SaveChangesAsync(ct);
        return NoticiaResponse.Desde(noticia);
    }

    public async Task<Result<Unit>> EliminarAsync(Guid id, CancellationToken ct)
    {
        var noticia = await repository.ObtenerPorIdAsync(id, ct);
        if (noticia is null)
        {
            return NoEncontrada(id);
        }

        repository.Eliminar(noticia);
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
