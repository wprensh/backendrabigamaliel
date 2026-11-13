using RabiGamaliel.Domain.Common;

namespace RabiGamaliel.Domain.Usuarios;

/// <summary>
/// Persona que ingresa al panel administrativo. El superadministrador tiene todos los permisos;
/// los demás solo los que se les asignen por módulo.
/// </summary>
public sealed class Usuario : Entity
{
    public const int NombreUsuarioMinLength = 3;
    public const int NombreUsuarioMaxLength = 50;
    public const int NombreCompletoMaxLength = 120;
    public const int CorreoMaxLength = 150;
    public const int ClaveMinLength = 8;
    public const int ClaveMaxLength = 128;
    public const int IntentosAntesDeBloquear = 5;
    public static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    private List<string> _permisos = [];

    private Usuario()
    {
    }

    /// <summary>Nombre con el que inicia sesión, siempre en minúsculas.</summary>
    public string NombreUsuario { get; private set; } = string.Empty;

    public string NombreCompleto { get; private set; } = string.Empty;

    public string? Correo { get; private set; }

    public string HashClave { get; private set; } = string.Empty;

    public bool EsSuperAdministrador { get; private set; }

    public bool Activo { get; private set; } = true;

    /// <summary>Permisos como texto "Modulo:Accion"; ver <see cref="Permiso"/>.</summary>
    public IReadOnlyList<string> Permisos => _permisos;

    /// <summary>
    /// Cambia cada vez que cambian la clave, los permisos o el estado: las sesiones abiertas
    /// con un sello anterior dejan de valer en la siguiente petición.
    /// </summary>
    public Guid SelloSeguridad { get; private set; } = Guid.NewGuid();

    public int IntentosFallidos { get; private set; }

    public DateTimeOffset? BloqueadoHasta { get; private set; }

    public DateTimeOffset? UltimoAcceso { get; private set; }

    public static Usuario Crear(
        string nombreUsuario,
        string nombreCompleto,
        string? correo,
        string hashClave,
        bool esSuperAdministrador,
        IEnumerable<Permiso> permisos)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreUsuario);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashClave);

        var usuario = new Usuario
        {
            NombreUsuario = NormalizarNombreUsuario(nombreUsuario),
            HashClave = hashClave,
        };
        usuario.ActualizarDatos(nombreCompleto, correo);
        usuario.AsignarAcceso(esSuperAdministrador, permisos, activo: true);
        return usuario;
    }

    public static string NormalizarNombreUsuario(string nombreUsuario) => nombreUsuario.Trim().ToLowerInvariant();

    public void ActualizarDatos(string nombreCompleto, string? correo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreCompleto);
        NombreCompleto = nombreCompleto.Trim();
        Correo = string.IsNullOrWhiteSpace(correo) ? null : correo.Trim().ToLowerInvariant();
    }

    public void AsignarAcceso(bool esSuperAdministrador, IEnumerable<Permiso> permisos, bool activo)
    {
        // Quien puede crear, editar o eliminar también puede ver.
        var conjunto = permisos
            .SelectMany(p => p.Accion == AccionPermiso.Ver ? [p] : new[] { p, p with { Accion = AccionPermiso.Ver } })
            .Distinct()
            .OrderBy(p => p.Modulo).ThenBy(p => p.Accion)
            .Select(p => p.ToString())
            .ToList();

        var cambio = esSuperAdministrador != EsSuperAdministrador || activo != Activo || !conjunto.SequenceEqual(_permisos);
        EsSuperAdministrador = esSuperAdministrador;
        Activo = activo;
        _permisos = esSuperAdministrador ? [] : conjunto;
        if (cambio)
        {
            SelloSeguridad = Guid.NewGuid();
        }
    }

    public bool Puede(ModuloSistema modulo, AccionPermiso accion) =>
        Activo && (EsSuperAdministrador || _permisos.Contains(new Permiso(modulo, accion).ToString()));

    /// <summary>Permisos efectivos: todos para el superadministrador.</summary>
    public IEnumerable<Permiso> PermisosEfectivos() =>
        EsSuperAdministrador
            ? Permiso.Todos
            : _permisos.Select(p => Permiso.TryParse(p, out var permiso) ? permiso : (Permiso?)null)
                       .Where(p => p is not null)
                       .Select(p => p!.Value);

    public void CambiarClave(string hashClave)
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

    /// <summary>Tras <see cref="IntentosAntesDeBloquear"/> claves erradas seguidas, la cuenta se bloquea un rato.</summary>
    public void RegistrarIntentoFallido(DateTimeOffset ahora)
    {
        IntentosFallidos++;
        if (IntentosFallidos >= IntentosAntesDeBloquear)
        {
            IntentosFallidos = 0;
            BloqueadoHasta = ahora + DuracionBloqueo;
        }
    }

    /// <summary>Al rehashear una clave con un algoritmo más reciente no se cierran las sesiones.</summary>
    public void ActualizarHash(string hashClave) => HashClave = hashClave;
}
