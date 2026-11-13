using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Certificaciones;

/// <summary>
/// Certificado o constancia expedido con el membrete de la institución.
/// Una vez expedido su contenido no cambia: si tiene un error se anula y se expide uno nuevo,
/// así el código de verificación siempre corresponde al documento impreso.
/// </summary>
public sealed class Certificado : Entity
{
    public const int CodigoMaxLength = 20;
    public const int TituloMaxLength = 150;
    public const int NombreMaxLength = 150;
    public const int NumeroDocumentoMaxLength = 20;
    public const int GradoMaxLength = 50;
    public const int ContenidoMaxLength = 5000;
    public const int FirmanteNombreMaxLength = 120;
    public const int FirmanteCargoMaxLength = 80;
    public const int MotivoAnulacionMaxLength = 300;

    private Certificado()
    {
    }

    /// <summary>Código impreso en el documento para verificarlo en el sitio, p. ej. "CERT-2026-0A1B2C".</summary>
    public string Codigo { get; private set; } = string.Empty;

    public TipoCertificado Tipo { get; private set; }

    /// <summary>Título que encabeza el documento ("CERTIFICADO DE ESTUDIOS", "CONSTANCIA DE MATRÍCULA"...).</summary>
    public string Titulo { get; private set; } = string.Empty;

    public string NombreBeneficiario { get; private set; } = string.Empty;

    public TipoDocumento TipoDocumento { get; private set; }

    public string NumeroDocumento { get; private set; } = string.Empty;

    public string? Grado { get; private set; }

    public int? AnioLectivo { get; private set; }

    /// <summary>Texto completo que certifica la institución.</summary>
    public string Contenido { get; private set; } = string.Empty;

    public DateOnly FechaExpedicion { get; private set; }

    public string FirmanteNombre { get; private set; } = string.Empty;

    public string FirmanteCargo { get; private set; } = string.Empty;

    public bool Anulado { get; private set; }

    public string? MotivoAnulacion { get; private set; }

    public static Certificado Expedir(
        TipoCertificado tipo,
        string titulo,
        string nombreBeneficiario,
        TipoDocumento tipoDocumento,
        string numeroDocumento,
        string? grado,
        int? anioLectivo,
        string contenido,
        DateOnly fechaExpedicion,
        string firmanteNombre,
        string firmanteCargo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titulo);
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreBeneficiario);
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroDocumento);
        ArgumentException.ThrowIfNullOrWhiteSpace(contenido);
        ArgumentException.ThrowIfNullOrWhiteSpace(firmanteNombre);
        ArgumentException.ThrowIfNullOrWhiteSpace(firmanteCargo);

        var certificado = new Certificado
        {
            Tipo = tipo,
            Titulo = titulo.Trim(),
            NombreBeneficiario = nombreBeneficiario.Trim(),
            TipoDocumento = tipoDocumento,
            NumeroDocumento = numeroDocumento.Trim(),
            Grado = string.IsNullOrWhiteSpace(grado) ? null : grado.Trim(),
            AnioLectivo = anioLectivo,
            Contenido = contenido.Trim(),
            FechaExpedicion = fechaExpedicion,
            FirmanteNombre = firmanteNombre.Trim(),
            FirmanteCargo = firmanteCargo.Trim(),
        };

        // Igual que el radicado PQRS: los últimos 6 caracteres del Guid v7 son aleatorios.
        certificado.Codigo = $"CERT-{fechaExpedicion.Year}-{certificado.Id.ToString("N")[^6..].ToUpperInvariant()}";
        return certificado;
    }

    public void Anular(string motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);

        if (Anulado)
        {
            throw new InvalidOperationException("El certificado ya está anulado.");
        }

        Anulado = true;
        MotivoAnulacion = motivo.Trim();
    }
}
