using RabiGamaliel.Domain.Campus;

namespace RabiGamaliel.Application.Campus;

/// <summary>Estudiante, docente o acudiente tal como lo ve el módulo de comunidad. Nunca incluye la clave.</summary>
public sealed record MiembroCampusResponse(
    Guid Id,
    RolCampus Rol,
    string Documento,
    string NombreCompleto,
    int? Grado,
    string? Grupo,
    bool Activo,
    bool TieneClave,
    bool Bloqueado,
    DateTimeOffset? UltimoAcceso,
    DateTimeOffset CreadoEn,
    string? Correo,
    string? Telefono,
    string CodigoCarnet)
{
    public static MiembroCampusResponse Desde(MiembroCampus m, DateTimeOffset ahora) => new(
        m.Id, m.Rol, m.Documento, m.NombreCompleto, m.Grado, m.Grupo, m.Activo, m.TieneClave, m.EstaBloqueado(ahora),
        m.UltimoAcceso, m.CreadoEn, m.Correo, m.Telefono, m.CodigoCarnet);
}

/// <summary>Cuerpo de POST /api/comunidad.</summary>
public sealed record CrearMiembroRequest(
    RolCampus Rol, string Documento, string NombreCompleto, int? Grado, string? Grupo, string? Correo = null, string? Telefono = null);

/// <summary>Cuerpo de PUT /api/comunidad/{id}. El documento no cambia.</summary>
public sealed record ActualizarMiembroRequest(
    RolCampus Rol, string NombreCompleto, int? Grado, string? Grupo, bool Activo, string? Correo = null, string? Telefono = null);

public sealed record MiembroQuery(
    string? Buscar = null, RolCampus? Rol = null, int? Grado = null, string? Grupo = null, int Pagina = 1, int TamanoPagina = 25)
{
    public const int TamanoPaginaMaximo = 200;
}

/// <summary>Una fila de la lista que se pega desde Excel o del SIMAT.</summary>
public sealed record FilaImportacion(string Documento, string NombreCompleto, int? Grado, string? Grupo);

/// <summary>
/// Cuerpo de POST /api/comunidad/importar. Crea los que no existen y actualiza nombre, grado y grupo
/// de los que ya están (útil al empezar cada año).
/// </summary>
public sealed record ImportarMiembrosRequest(RolCampus Rol, IReadOnlyList<FilaImportacion> Filas)
{
    public const int FilasMaximas = 3000;
}

public sealed record ErrorImportacion(int Fila, string Documento, string Mensaje);

public sealed record ImportacionResponse(int Creados, int Actualizados, IReadOnlyList<ErrorImportacion> Errores);

/// <summary>
/// Cuerpo de POST /api/comunidad/claves. Genera claves nuevas para quienes cumplan el filtro
/// (o para los ids indicados). Las claves anteriores dejan de servir.
/// </summary>
public sealed record GenerarClavesRequest(
    RolCampus? Rol = null, int? Grado = null, string? Grupo = null, bool SoloSinClave = false, IReadOnlyList<Guid>? Ids = null)
{
    public const int MaximoPorSolicitud = 3000;
}

/// <summary>Clave en texto claro: solo se ve en esta respuesta, para imprimirla y entregarla.</summary>
public sealed record ClaveGenerada(Guid Id, string Documento, string NombreCompleto, RolCampus Rol, int? Grado, string? Grupo, string Clave);

// ---------- Ingreso al campus ----------

public sealed record IniciarSesionCampusRequest(string Documento, string Clave);

/// <summary>Estudiante a cargo de un acudiente.</summary>
public sealed record EstudianteVinculado(Guid Id, string NombreCompleto, string Documento, int? Grado, string? Grupo, string? Parentesco);

public sealed record PerfilCampusResponse(
    Guid Id,
    RolCampus Rol,
    string Documento,
    string NombreCompleto,
    int? Grado,
    string? Grupo,
    string CodigoCarnet,
    IReadOnlyList<EstudianteVinculado> Estudiantes)
{
    public static PerfilCampusResponse Desde(MiembroCampus m, IReadOnlyList<EstudianteVinculado>? estudiantes = null) =>
        new(m.Id, m.Rol, m.Documento, m.NombreCompleto, m.Grado, m.Grupo, m.CodigoCarnet, estudiantes ?? []);
}

// ---------- Acudientes ----------

/// <summary>Vínculo acudiente–estudiante, visto desde cualquiera de los dos lados.</summary>
public sealed record VinculoResponse(
    Guid Id, Guid AcudienteId, string AcudienteNombre, string AcudienteDocumento, string? AcudienteTelefono, string? AcudienteCorreo,
    Guid EstudianteId, string EstudianteNombre, string EstudianteDocumento, int? Grado, string? Grupo, string? Parentesco);

/// <summary>Cuerpo de POST /api/comunidad/{id}/vinculos: vincula por documento a la otra persona.</summary>
public sealed record VincularRequest(string Documento, string? Parentesco);

/// <summary>Una fila de la lista de acudientes (del SIMAT o de Excel).</summary>
public sealed record FilaAcudiente(
    string DocumentoAcudiente, string NombreAcudiente, string DocumentoEstudiante, string? Parentesco, string? Correo, string? Telefono);

public sealed record ImportarAcudientesRequest(IReadOnlyList<FilaAcudiente> Filas)
{
    public const int FilasMaximas = 3000;
}

public sealed record ImportacionAcudientesResponse(int AcudientesCreados, int AcudientesActualizados, int Vinculos, IReadOnlyList<ErrorImportacion> Errores);

// ---------- Carnet digital ----------

/// <summary>Lo que muestra la verificación pública del carnet: sin datos sensibles.</summary>
public sealed record CarnetVerificacionResponse(
    string Codigo, string NombreCompleto, RolCampus Rol, int? Grado, string? Grupo, string DocumentoParcial, bool Vigente, int AnioLectivo);

public sealed record SesionCampusResponse(string Token, DateTimeOffset Expira, PerfilCampusResponse Perfil);
