using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Campus;

public enum RolCampus
{
    Estudiante = 1,
    Docente,

    /// <summary>Padre, madre o acudiente: consulta boletines, asistencia, observador y circulares de sus estudiantes.</summary>
    Acudiente,
}

/// <summary>
/// Estudiante, docente o acudiente que ingresa al campus virtual con su número de documento y una clave de acceso
/// que genera el colegio. Los estudiantes votan en las elecciones escolares; los docentes siguen la jornada.
/// </summary>
public sealed class MiembroCampus : Entity
{
    public const int DocumentoMinLength = 4;
    public const int DocumentoMaxLength = 20;
    public const int NombreMaxLength = 120;
    public const int GrupoMaxLength = 5;
    public const int CorreoMaxLength = 150;
    public const int TelefonoMaxLength = 20;
    public const int CodigoCarnetLength = 10;
    public const int IntentosAntesDeBloquear = 5;
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    private MiembroCampus()
    {
    }

    public RolCampus Rol { get; private set; }

    /// <summary>Número de documento sin puntos ni espacios; es el usuario de ingreso.</summary>
    public string Documento { get; private set; } = string.Empty;

    public string NombreCompleto { get; private set; } = string.Empty;

    /// <summary>Grado del estudiante (0 = transición, 1 a 11). Null para docentes.</summary>
    public int? Grado { get; private set; }

    /// <summary>Grupo dentro del grado (p. ej. "A" o "01"). Null para docentes.</summary>
    public string? Grupo { get; private set; }

    public bool Activo { get; private set; } = true;

    /// <summary>Correo para avisos (inasistencias, observador, circulares). Sobre todo de acudientes.</summary>
    public string? Correo { get; private set; }

    /// <summary>Celular con WhatsApp, para que el docente avise con un clic.</summary>
    public string? Telefono { get; private set; }

    /// <summary>Código del carnet digital; el QR del carnet apunta a su verificación pública.</summary>
    public string CodigoCarnet { get; private set; } = string.Empty;

    /// <summary>Hash de la clave de acceso; null mientras el colegio no la haya generado.</summary>
    public string? HashClave { get; private set; }

    /// <summary>Cambia con cada clave nueva o desactivación: las sesiones anteriores dejan de valer.</summary>
    public Guid SelloSeguridad { get; private set; } = Guid.NewGuid();

    public int IntentosFallidos { get; private set; }

    public DateTimeOffset? BloqueadoHasta { get; private set; }

    public DateTimeOffset? UltimoAcceso { get; private set; }

    public bool TieneClave => HashClave is not null;

    public static MiembroCampus Crear(RolCampus rol, string documento, string nombreCompleto, int? grado, string? grupo)
    {
        var miembro = new MiembroCampus { Documento = NormalizarDocumento(documento), CodigoCarnet = GenerarCodigoCarnet() };
        miembro.Actualizar(rol, nombreCompleto, grado, grupo, activo: true);
        return miembro;
    }

    /// <summary>Código aleatorio sin caracteres que se confundan (O/0, I/1).</summary>
    public static string GenerarCodigoCarnet() =>
        new(System.Security.Cryptography.RandomNumberGenerator.GetItems<char>("ABCDEFGHJKMNPQRSTUVWXYZ23456789", CodigoCarnetLength));

    /// <summary>Quita puntos, espacios y guiones: "1.043.555 123" y "1043555123" son el mismo documento.</summary>
    public static string NormalizarDocumento(string documento) =>
        new([.. documento.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant)]);

    public static bool GradoValido(int grado) => grado is >= GradosEscolares.Transicion and <= GradosEscolares.Ultimo;

    public void Actualizar(RolCampus rol, string nombreCompleto, int? grado, string? grupo, bool activo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreCompleto);
        if (rol == RolCampus.Estudiante && (grado is not { } g || !GradoValido(g)))
        {
            throw new ArgumentOutOfRangeException(nameof(grado), "Un estudiante necesita un grado entre 0 y 11.");
        }

        Rol = rol;
        NombreCompleto = nombreCompleto.Trim();
        Grado = rol == RolCampus.Estudiante ? grado : null;
        Grupo = rol == RolCampus.Estudiante && !string.IsNullOrWhiteSpace(grupo) ? grupo.Trim().ToUpperInvariant() : null;
        if (activo != Activo)
        {
            Activo = activo;
            SelloSeguridad = Guid.NewGuid();
        }
    }

    public void ActualizarContacto(string? correo, string? telefono)
    {
        Correo = string.IsNullOrWhiteSpace(correo) ? null : correo.Trim().ToLowerInvariant();
        Telefono = string.IsNullOrWhiteSpace(telefono) ? null : new string([.. telefono.Where(c => char.IsDigit(c) || c == '+')]);
    }

    public void AsignarClave(string hashClave)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashClave);
        HashClave = hashClave;
        IntentosFallidos = 0;
        BloqueadoHasta = null;
        SelloSeguridad = Guid.NewGuid();
    }

    public bool EstaBloqueado(DateTimeOffset ahora) => BloqueadoHasta is { } hasta && hasta > ahora;

    public void RegistrarIngreso(DateTimeOffset ahora)
    {
        IntentosFallidos = 0;
        BloqueadoHasta = null;
        UltimoAcceso = ahora;
    }

    public void RegistrarIntentoFallido(DateTimeOffset ahora)
    {
        IntentosFallidos++;
        if (IntentosFallidos >= IntentosAntesDeBloquear)
        {
            IntentosFallidos = 0;
            BloqueadoHasta = ahora + DuracionBloqueo;
        }
    }
}

/// <summary>Grados que ofrece la institución: transición (0) y de primero a once.</summary>
public static class GradosEscolares
{
    public const int Transicion = 0;
    public const int Ultimo = 11;

    public static string Nombre(int grado) => grado == Transicion ? "Transición" : $"{grado}°";
}
