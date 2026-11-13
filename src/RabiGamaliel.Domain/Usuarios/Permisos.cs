namespace RabiGamaliel.Domain.Usuarios;

/// <summary>Módulos del panel administrativo. Un módulo nuevo se agrega aquí y queda disponible en la matriz de permisos.</summary>
public enum ModuloSistema
{
    Noticias = 1,
    Certificados,
    Pqrs,
    Usuarios,
    Elecciones,

    /// <summary>Estudiantes, docentes y acudientes que ingresan al campus virtual.</summary>
    Campus,

    /// <summary>Asignaturas, periodos, carga académica, notas, boletines y asistencia.</summary>
    Academico,

    /// <summary>Asistencia y observador: reporte de inasistencias, observador del estudiante y casos de convivencia (Ley 1620).</summary>
    Convivencia,

    /// <summary>Simulacros de las pruebas Saber y sus resultados.</summary>
    Simulacros,

    /// <summary>Solicitudes de cupo (pre-matrícula) que llegan desde el sitio web.</summary>
    Admisiones,

    /// <summary>Circulares para familias y docentes con confirmación de lectura.</summary>
    Circulares,

    /// <summary>Tablero de indicadores para rectoría y coordinación.</summary>
    Indicadores,

    /// <summary>Eventos del calendario escolar (página pública y campus).</summary>
    Calendario,

    /// <summary>Carnés con QR de estudiantes y docentes.</summary>
    Carnets,

    /// <summary>Plan de estudios (mallas con estándares y DBA) y revisión de las planeaciones de clase de los docentes.</summary>
    Planeacion,

    /// <summary>Recuperaciones y comisiones de evaluación y promoción (Decreto 1290).</summary>
    Evaluacion,

    /// <summary>Reportes oficiales: formato tipo SIMAT, libro final de calificaciones y certificado de estudios.</summary>
    Reportes,
}

/// <summary>Nivel de acción sobre los registros de un módulo.</summary>
public enum AccionPermiso
{
    Ver = 1,
    Crear,
    Editar,
    Eliminar,
}

/// <summary>
/// Permiso de un usuario sobre un módulo, guardado como texto "Modulo:Accion" (p. ej. "Noticias:Editar").
/// Crear, editar o eliminar implican poder ver los registros.
/// </summary>
public readonly record struct Permiso(ModuloSistema Modulo, AccionPermiso Accion)
{
    public static IReadOnlyList<Permiso> Todos { get; } =
    [
        .. from modulo in Enum.GetValues<ModuloSistema>()
           from accion in Enum.GetValues<AccionPermiso>()
           select new Permiso(modulo, accion),
    ];

    public override string ToString() => $"{Modulo}:{Accion}";

    public static bool TryParse(string? valor, out Permiso permiso)
    {
        permiso = default;
        var partes = valor?.Split(':');
        if (partes is not [var modulo, var accion]
            || !Enum.TryParse<ModuloSistema>(modulo, ignoreCase: false, out var m) || !Enum.IsDefined(m)
            || !Enum.TryParse<AccionPermiso>(accion, ignoreCase: false, out var a) || !Enum.IsDefined(a))
        {
            return false;
        }

        permiso = new Permiso(m, a);
        return true;
    }
}
