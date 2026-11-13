using RabiGamaliel.Domain.Certificaciones;

namespace RabiGamaliel.Application.Certificaciones;

/// <summary>Datos de un certificado para el panel administrativo y la impresión.</summary>
public sealed record CertificadoResponse(
    Guid Id,
    string Codigo,
    TipoCertificado Tipo,
    string Titulo,
    string NombreBeneficiario,
    TipoDocumento TipoDocumento,
    string NumeroDocumento,
    string? Grado,
    int? AnioLectivo,
    string Contenido,
    DateOnly FechaExpedicion,
    string FirmanteNombre,
    string FirmanteCargo,
    bool Anulado,
    string? MotivoAnulacion,
    DateTimeOffset CreadoEn)
{
    public static CertificadoResponse Desde(Certificado c) => new(
        c.Id, c.Codigo, c.Tipo, c.Titulo, c.NombreBeneficiario, c.TipoDocumento, c.NumeroDocumento,
        c.Grado, c.AnioLectivo, c.Contenido, c.FechaExpedicion, c.FirmanteNombre, c.FirmanteCargo,
        c.Anulado, c.MotivoAnulacion, c.CreadoEn);
}

/// <summary>Cuerpo de POST /api/certificados.</summary>
public sealed record ExpedirCertificadoRequest(
    TipoCertificado Tipo,
    string Titulo,
    string NombreBeneficiario,
    TipoDocumento TipoDocumento,
    string NumeroDocumento,
    string? Grado,
    int? AnioLectivo,
    string Contenido,
    DateOnly FechaExpedicion,
    string FirmanteNombre,
    string FirmanteCargo);

public sealed record AnularCertificadoRequest(string Motivo);

/// <summary>
/// Lo que ve cualquier persona al verificar un código: suficiente para confirmar que el documento
/// es auténtico, sin exponer el número de documento completo (Ley 1581 de protección de datos).
/// </summary>
public sealed record VerificacionCertificadoResponse(
    string Codigo,
    TipoCertificado Tipo,
    string Titulo,
    string NombreBeneficiario,
    string DocumentoParcial,
    DateOnly FechaExpedicion,
    bool Anulado)
{
    public static VerificacionCertificadoResponse Desde(Certificado c) => new(
        c.Codigo, c.Tipo, c.Titulo, c.NombreBeneficiario, OcultarDocumento(c.NumeroDocumento),
        c.FechaExpedicion, c.Anulado);

    /// <summary>"1043567890" → "******7890".</summary>
    public static string OcultarDocumento(string numero) =>
        numero.Length <= 4 ? new string('*', numero.Length) : new string('*', numero.Length - 4) + numero[^4..];
}

/// <summary>Filtros y paginación del listado de certificados.</summary>
public sealed record CertificadoQuery(
    string? Buscar = null,
    TipoCertificado? Tipo = null,
    int Pagina = 1,
    int TamanoPagina = 10)
{
    public const int TamanoPaginaMaximo = 100;
}
