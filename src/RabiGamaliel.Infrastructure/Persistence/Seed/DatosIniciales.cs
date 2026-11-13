using RabiGamaliel.Domain.Academico;
using Microsoft.EntityFrameworkCore;

namespace RabiGamaliel.Infrastructure.Persistence.Seed;

/// <summary>
/// Datos iniciales de una base de datos nueva: solo el plan de estudios base (áreas de ley).
/// No se siembran noticias: las publica la institución desde el panel.
/// Se registran con <c>UseSeeding</c> / <c>UseAsyncSeeding</c>, la API de siembra de EF Core 9,
/// que se ejecuta al aplicar migraciones (<c>Database.MigrateAsync</c> o <c>dotnet ef database update</c>).
/// EF recomienda implementar ambas: la herramienta de línea de comandos usa la versión síncrona.
/// </summary>
internal static class DatosIniciales
{
    public static void Sembrar(DbContext context)
    {
        var asignaturas = context.Set<Asignatura>();
        if (!asignaturas.Any())
        {
            asignaturas.AddRange(CrearPlanDeEstudios());
        }

        context.SaveChanges();
    }

    public static async Task SembrarAsync(DbContext context, CancellationToken ct)
    {
        var asignaturas = context.Set<Asignatura>();
        if (!await asignaturas.AnyAsync(ct))
        {
            asignaturas.AddRange(CrearPlanDeEstudios());
        }

        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Áreas obligatorias y fundamentales (Ley 115 de 1994, arts. 23 y 31). Coordinación puede renombrarlas,
    /// desactivarlas o agregar otras desde el panel.
    /// </summary>
    private static Asignatura[] CrearPlanDeEstudios() =>
    [
        Asignatura.Crear("Lengua Castellana", "Humanidades", 10),
        Asignatura.Crear("Inglés", "Humanidades", 20),
        Asignatura.Crear("Matemáticas", "Matemáticas", 30),
        Asignatura.Crear("Ciencias Naturales y Educación Ambiental", "Ciencias Naturales", 40),
        Asignatura.Crear("Química", "Ciencias Naturales", 41),
        Asignatura.Crear("Física", "Ciencias Naturales", 42),
        Asignatura.Crear("Ciencias Sociales", "Ciencias Sociales", 50),
        Asignatura.Crear("Ciencias Económicas y Políticas", "Ciencias Sociales", 51),
        Asignatura.Crear("Filosofía", "Filosofía", 60),
        Asignatura.Crear("Educación Ética y en Valores Humanos", "Ética y Valores", 70),
        Asignatura.Crear("Educación Religiosa", "Educación Religiosa", 80),
        Asignatura.Crear("Educación Artística y Cultural", "Educación Artística", 90),
        Asignatura.Crear("Educación Física, Recreación y Deportes", "Educación Física", 100),
        Asignatura.Crear("Tecnología e Informática", "Tecnología e Informática", 110),
    ];
}
