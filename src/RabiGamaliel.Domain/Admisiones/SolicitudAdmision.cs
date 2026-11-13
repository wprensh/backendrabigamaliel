using RabiGamaliel.Domain.Certificaciones;
using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Admisiones;

public enum EstadoAdmision
{
    Recibida = 1,
    EnRevision,
    DocumentosPendientes,
    Aprobada,
    Rechazada,

    /// <summary>Ya quedó matriculado: el estudiante y su acudiente se crearon en el campus.</summary>
    Matriculada,
}

public enum TipoDocumentoAdmision
{
    DocumentoIdentidad = 1,
    CertificadoNotas,
    CarneVacunas,
    AfiliacionSalud,
    Foto,
    Otro,
}

/// <summary>
/// Solicitud de cupo hecha desde el sitio web. La familia la sigue con el radicado y el documento del aspirante;
/// la secretaría la revisa en el panel y, al matricular, crea al estudiante y al acudiente en el campus.
/// </summary>
public sealed class SolicitudAdmision : Entity
{
    public const int RadicadoLength = 10;
    public const int NombreMaxLength = 80;
    public const int DocumentoMaxLength = 20;
    public const int TextoCortoMaxLength = 150;
    public const int TelefonoMaxLength = 20;
    public const int CorreoMaxLength = 150;
    public const int ObservacionesMaxLength = 1500;
    public const int MensajeMaxLength = 1500;
    public const int MaximoDocumentos = 6;

    private SolicitudAdmision()
    {
    }

    /// <summary>Código para consultar el estado (p. ej. "ADM-7K3Q9X").</summary>
    public string Radicado { get; private set; } = string.Empty;

    public int AnioLectivo { get; private set; }

    public int GradoSolicitado { get; private set; }

    public TipoDocumento AspiranteTipoDocumento { get; private set; }

    public string AspiranteDocumento { get; private set; } = string.Empty;

    public string AspiranteNombres { get; private set; } = string.Empty;

    public string AspiranteApellidos { get; private set; } = string.Empty;

    public DateOnly AspiranteFechaNacimiento { get; private set; }

    public string? ColegioProcedencia { get; private set; }

    public string? Direccion { get; private set; }

    public string? Eps { get; private set; }

    public string AcudienteNombre { get; private set; } = string.Empty;

    public string AcudienteDocumento { get; private set; } = string.Empty;

    public string? AcudienteParentesco { get; private set; }

    public string AcudienteTelefono { get; private set; } = string.Empty;

    public string? AcudienteCorreo { get; private set; }

    public string? Observaciones { get; private set; }

    public EstadoAdmision Estado { get; private set; } = EstadoAdmision.Recibida;

    /// <summary>Mensaje que la familia ve al consultar el radicado.</summary>
    public string? MensajeFamilia { get; private set; }

    /// <summary>Notas de la secretaría; la familia no las ve.</summary>
    public string? NotaInterna { get; private set; }

    public DateTimeOffset? EstadoCambiadoEn { get; private set; }

    /// <summary>Estudiante creado en el campus al matricular.</summary>
    public Guid? EstudianteId { get; private set; }

    public string NombreAspirante => $"{AspiranteNombres} {AspiranteApellidos}";

#pragma warning disable CA1054 // Parámetros que no son URL.
    public static SolicitudAdmision Crear(
        int anioLectivo, int gradoSolicitado, TipoDocumento tipoDocumento, string documento, string nombres, string apellidos,
        DateOnly fechaNacimiento, string? colegioProcedencia, string? direccion, string? eps, string acudienteNombre,
        string acudienteDocumento, string? acudienteParentesco, string acudienteTelefono, string? acudienteCorreo, string? observaciones)
#pragma warning restore CA1054
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documento);
        ArgumentException.ThrowIfNullOrWhiteSpace(nombres);
        ArgumentException.ThrowIfNullOrWhiteSpace(apellidos);
        ArgumentException.ThrowIfNullOrWhiteSpace(acudienteNombre);
        ArgumentException.ThrowIfNullOrWhiteSpace(acudienteTelefono);
        return new SolicitudAdmision
        {
            Radicado = GenerarRadicado(),
            AnioLectivo = anioLectivo,
            GradoSolicitado = gradoSolicitado,
            AspiranteTipoDocumento = tipoDocumento,
            AspiranteDocumento = Campus.MiembroCampus.NormalizarDocumento(documento),
            AspiranteNombres = nombres.Trim(),
            AspiranteApellidos = apellidos.Trim(),
            AspiranteFechaNacimiento = fechaNacimiento,
            ColegioProcedencia = Limpiar(colegioProcedencia),
            Direccion = Limpiar(direccion),
            Eps = Limpiar(eps),
            AcudienteNombre = acudienteNombre.Trim(),
            AcudienteDocumento = Campus.MiembroCampus.NormalizarDocumento(acudienteDocumento),
            AcudienteParentesco = Limpiar(acudienteParentesco),
            AcudienteTelefono = new string([.. acudienteTelefono.Where(c => char.IsDigit(c) || c == '+')]),
            AcudienteCorreo = Limpiar(acudienteCorreo)?.ToLowerInvariant(),
            Observaciones = Limpiar(observaciones),
        };
    }

    public static string GenerarRadicado() =>
        "ADM-" + new string(System.Security.Cryptography.RandomNumberGenerator.GetItems<char>("ABCDEFGHJKMNPQRSTUVWXYZ23456789", 6));

    public void CambiarEstado(EstadoAdmision estado, string? mensajeFamilia, string? notaInterna, DateTimeOffset ahora)
    {
        if (estado != Estado)
        {
            Estado = estado;
            EstadoCambiadoEn = ahora;
        }

        MensajeFamilia = Limpiar(mensajeFamilia);
        NotaInterna = Limpiar(notaInterna);
    }

    public void MarcarMatriculada(Guid estudianteId, DateTimeOffset ahora)
    {
        EstudianteId = estudianteId;
        if (Estado != EstadoAdmision.Matriculada)
        {
            Estado = EstadoAdmision.Matriculada;
            EstadoCambiadoEn = ahora;
        }
    }

    /// <summary>La familia puede seguir cargando documentos mientras no se haya decidido.</summary>
    public bool AdmiteDocumentos => Estado is EstadoAdmision.Recibida or EstadoAdmision.EnRevision or EstadoAdmision.DocumentosPendientes;

    private static string? Limpiar(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}

/// <summary>Archivo (PDF o imagen) adjunto a una solicitud de cupo. Se guarda en la base de datos.</summary>
public sealed class DocumentoAdmision : Entity
{
    public const int NombreArchivoMaxLength = 150;
    public const int TamanoMaximoBytes = 3 * 1024 * 1024;
    public static readonly IReadOnlySet<string> TiposPermitidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/jpeg", "image/png",
    };

    private DocumentoAdmision()
    {
    }

    public Guid SolicitudId { get; private set; }

    public TipoDocumentoAdmision Tipo { get; private set; }

    public string NombreArchivo { get; private set; } = string.Empty;

    public string TipoContenido { get; private set; } = string.Empty;

    public int Tamano { get; private set; }

#pragma warning disable CA1819 // El contenido del archivo es un arreglo de bytes.
    public byte[] Contenido { get; private set; } = [];
#pragma warning restore CA1819

    public static DocumentoAdmision Crear(Guid solicitudId, TipoDocumentoAdmision tipo, string nombreArchivo, string tipoContenido, byte[] contenido)
    {
        var nombre = Path.GetFileName(nombreArchivo).Trim();
        return new DocumentoAdmision
        {
            SolicitudId = solicitudId,
            Tipo = tipo,
            NombreArchivo = nombre.Length > NombreArchivoMaxLength ? nombre[^NombreArchivoMaxLength..] : nombre,
            TipoContenido = tipoContenido,
            Tamano = contenido.Length,
            Contenido = contenido,
        };
    }
}
