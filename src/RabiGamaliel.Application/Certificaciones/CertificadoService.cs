using FluentValidation;
using RabiGamaliel.Application.Abstractions;
using RabiGamaliel.Application.Common;
using RabiGamaliel.Domain.Certificaciones;

namespace RabiGamaliel.Application.Certificaciones;

public interface ICertificadoService
{
    Task<PagedResult<CertificadoResponse>> ListarAsync(CertificadoQuery query, CancellationToken ct);

    Task<Result<CertificadoResponse>> ObtenerAsync(Guid id, CancellationToken ct);

    Task<Result<CertificadoResponse>> ExpedirAsync(ExpedirCertificadoRequest request, CancellationToken ct);

    Task<Result<CertificadoResponse>> AnularAsync(Guid id, AnularCertificadoRequest request, CancellationToken ct);

    Task<Result<VerificacionCertificadoResponse>> VerificarAsync(string codigo, CancellationToken ct);
}

/// <summary>Expedición, consulta, anulación y verificación pública de certificados.</summary>
public sealed class CertificadoService(
    ICertificadoRepository repository,
    IUnitOfWork unitOfWork,
    IValidator<ExpedirCertificadoRequest> expedirValidator,
    IValidator<AnularCertificadoRequest> anularValidator) : ICertificadoService
{
    private static Error NoEncontrado(Guid id) =>
        Error.NoEncontrado("Certificado.NoEncontrado", $"No existe un certificado con id '{id}'.");

    public async Task<PagedResult<CertificadoResponse>> ListarAsync(CertificadoQuery query, CancellationToken ct)
    {
        var normalizada = query with
        {
            Pagina = Math.Max(1, query.Pagina),
            TamanoPagina = Math.Clamp(query.TamanoPagina, 1, CertificadoQuery.TamanoPaginaMaximo),
        };

        var pagina = await repository.ListarAsync(normalizada, ct);
        return pagina.Map(CertificadoResponse.Desde);
    }

    public async Task<Result<CertificadoResponse>> ObtenerAsync(Guid id, CancellationToken ct)
    {
        var certificado = await repository.ObtenerPorIdAsync(id, ct);
        return certificado is null ? NoEncontrado(id) : CertificadoResponse.Desde(certificado);
    }

    public async Task<Result<CertificadoResponse>> ExpedirAsync(ExpedirCertificadoRequest request, CancellationToken ct)
    {
        if (await expedirValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var certificado = Certificado.Expedir(
            request.Tipo, request.Titulo, request.NombreBeneficiario, request.TipoDocumento, request.NumeroDocumento,
            request.Grado, request.AnioLectivo, request.Contenido, request.FechaExpedicion,
            request.FirmanteNombre, request.FirmanteCargo);

        repository.Agregar(certificado);
        await unitOfWork.SaveChangesAsync(ct);

        return CertificadoResponse.Desde(certificado);
    }

    public async Task<Result<CertificadoResponse>> AnularAsync(Guid id, AnularCertificadoRequest request, CancellationToken ct)
    {
        if (await anularValidator.ValidarAsync(request, ct) is { } error)
        {
            return error;
        }

        var certificado = await repository.ObtenerPorIdAsync(id, ct);
        if (certificado is null)
        {
            return NoEncontrado(id);
        }

        if (certificado.Anulado)
        {
            return Error.Conflicto("Certificado.YaAnulado", "El certificado ya está anulado.");
        }

        certificado.Anular(request.Motivo);
        await unitOfWork.SaveChangesAsync(ct);
        return CertificadoResponse.Desde(certificado);
    }

    public async Task<Result<VerificacionCertificadoResponse>> VerificarAsync(string codigo, CancellationToken ct)
    {
        var normalizado = codigo.Trim().ToUpperInvariant();
        var certificado = normalizado.Length is > 0 and <= Certificado.CodigoMaxLength
            ? await repository.ObtenerPorCodigoAsync(normalizado, ct)
            : null;

        return certificado is null
            ? Error.NoEncontrado("Certificado.CodigoInvalido", "No existe un certificado con ese código de verificación.")
            : VerificacionCertificadoResponse.Desde(certificado);
    }
}
