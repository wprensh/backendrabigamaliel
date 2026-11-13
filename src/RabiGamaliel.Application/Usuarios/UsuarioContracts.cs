using RabiGamaliel.Domain.Usuarios;

namespace RabiGamaliel.Application.Usuarios;

/// <summary>Usuario tal como lo ve el módulo de usuarios. Nunca incluye el hash de la clave.</summary>
public sealed record UsuarioResponse(
    Guid Id,
    string NombreUsuario,
    string NombreCompleto,
    string? Correo,
    bool EsSuperAdministrador,
    bool Activo,
    bool Bloqueado,
    IReadOnlyList<string> Permisos,
    DateTimeOffset? UltimoAcceso,
    DateTimeOffset CreadoEn)
{
    public static UsuarioResponse Desde(Usuario u, DateTimeOffset ahora) => new(
        u.Id, u.NombreUsuario, u.NombreCompleto, u.Correo, u.EsSuperAdministrador, u.Activo, u.EstaBloqueado(ahora),
        [.. u.Permisos], u.UltimoAcceso, u.CreadoEn);
}

/// <summary>Cuerpo de POST /api/usuarios.</summary>
public sealed record CrearUsuarioRequest(
    string NombreUsuario,
    string NombreCompleto,
    string? Correo,
    string Clave,
    bool EsSuperAdministrador,
    IReadOnlyList<string>? Permisos);

/// <summary>Cuerpo de PUT /api/usuarios/{id}. El nombre de usuario no cambia.</summary>
public sealed record ActualizarUsuarioRequest(
    string NombreCompleto,
    string? Correo,
    bool EsSuperAdministrador,
    bool Activo,
    IReadOnlyList<string>? Permisos);

public sealed record RestablecerClaveRequest(string ClaveNueva);

/// <param name="OcultarSuperAdministradores">true cuando quien consulta no es superadministrador (rector, coordinador, secretaria...).</param>
public sealed record UsuarioQuery(string? Buscar = null, int Pagina = 1, int TamanoPagina = 20, bool OcultarSuperAdministradores = false)
{
    public const int TamanoPaginaMaximo = 100;
}

/// <summary>Un módulo con las acciones que admite, para dibujar la matriz de permisos.</summary>
public sealed record ModuloPermisosResponse(ModuloSistema Modulo, IReadOnlyList<AccionPermiso> Acciones);

// ---------- Inicio de sesión ----------

public sealed record IniciarSesionRequest(string Usuario, string Clave);

/// <summary>Datos de quien inició sesión y sus permisos efectivos ("Modulo:Accion").</summary>
public sealed record PerfilResponse(
    Guid Id,
    string NombreUsuario,
    string NombreCompleto,
    string? Correo,
    bool EsSuperAdministrador,
    IReadOnlyList<string> Permisos)
{
    public static PerfilResponse Desde(Usuario u) => new(
        u.Id, u.NombreUsuario, u.NombreCompleto, u.Correo, u.EsSuperAdministrador,
        [.. u.PermisosEfectivos().Select(p => p.ToString())]);
}

public sealed record SesionResponse(string Token, DateTimeOffset Expira, PerfilResponse Usuario);

public sealed record EstadoAutenticacionResponse(bool RequiereConfiguracion);

/// <summary>Primer superadministrador, cuando todavía no existe ningún usuario.</summary>
public sealed record ConfiguracionInicialRequest(string NombreUsuario, string NombreCompleto, string? Correo, string Clave);

public sealed record CambiarMiClaveRequest(string ClaveActual, string ClaveNueva);
