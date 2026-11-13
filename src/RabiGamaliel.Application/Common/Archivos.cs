namespace RabiGamaliel.Application.Common;

/// <summary>Archivo recibido en una carga (nombre, tipo MIME y contenido).</summary>
public sealed record ArchivoSubido(string NombreArchivo, string TipoContenido, byte[] Contenido);

/// <summary>Tipos de archivo aceptados en el aula virtual y verificación de su firma (no basta con la extensión).</summary>
public static class Archivos
{
    private const string Word = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string Excel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string PowerPoint = "application/vnd.openxmlformats-officedocument.presentationml.presentation";

    public static readonly IReadOnlySet<string> Permitidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf", "image/jpeg", "image/png", Word, Excel, PowerPoint,
    };

    public static bool FirmaValida(ArchivoSubido archivo)
    {
        var c = archivo.Contenido;
        return archivo.TipoContenido.ToLowerInvariant() switch
        {
            "application/pdf" => c.Length > 4 && c[0] == 0x25 && c[1] == 0x50 && c[2] == 0x44 && c[3] == 0x46,
            "image/png" => c.Length > 8 && c[0] == 0x89 && c[1] == 0x50 && c[2] == 0x4E && c[3] == 0x47,
            "image/jpeg" => c.Length > 3 && c[0] == 0xFF && c[1] == 0xD8 && c[2] == 0xFF,
            Word or Excel or PowerPoint => c.Length > 4 && c[0] == 0x50 && c[1] == 0x4B && c[2] == 0x03 && c[3] == 0x04,
            _ => false,
        };
    }

    /// <summary>¿Es un enlace https (o vacío)?</summary>
    public static bool EnlaceValido(string? enlace) =>
        string.IsNullOrWhiteSpace(enlace) || (Uri.TryCreate(enlace.Trim(), UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps);
}
