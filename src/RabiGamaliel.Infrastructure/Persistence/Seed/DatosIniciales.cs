using RabiGamaliel.Domain.Academico;
using RabiGamaliel.Domain.Simulacros;
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

        var banco = context.Set<PreguntaBanco>();
        var existentes = banco.Select(p => new { p.Area, p.Contexto, p.Enunciado }).AsEnumerable()
            .Select(p => BancoSaber.Clave(p.Area, p.Contexto, p.Enunciado)).ToHashSet();
        banco.AddRange(BancoSaber.Faltantes(existentes));
        context.SaveChanges();

        if (!context.Set<Simulacro>().Any(s => s.Titulo == BancoSaber.TituloSimulacro))
        {
            CrearSimulacro(context, [.. banco]);
            context.SaveChanges();
        }
    }

    public static async Task SembrarAsync(DbContext context, CancellationToken ct)
    {
        var asignaturas = context.Set<Asignatura>();
        if (!await asignaturas.AnyAsync(ct))
        {
            asignaturas.AddRange(CrearPlanDeEstudios());
        }

        await context.SaveChangesAsync(ct);

        var banco = context.Set<PreguntaBanco>();
        var existentes = (await banco.Select(p => new { p.Area, p.Contexto, p.Enunciado }).ToListAsync(ct))
            .Select(p => BancoSaber.Clave(p.Area, p.Contexto, p.Enunciado)).ToHashSet();
        banco.AddRange(BancoSaber.Faltantes(existentes));
        await context.SaveChangesAsync(ct);

        if (!await context.Set<Simulacro>().AnyAsync(s => s.Titulo == BancoSaber.TituloSimulacro, ct))
        {
            CrearSimulacro(context, await banco.ToListAsync(ct));
            await context.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Simulacro Saber 11 de entrenamiento: 20 preguntas del banco (4 por área), para 10.° y 11.°, abierto hasta el
    /// 31 de diciembre de 2027. Coordinación puede cambiar fechas, grados o preguntas desde el panel.
    /// </summary>
    private static void CrearSimulacro(DbContext context, IReadOnlyList<PreguntaBanco> banco)
    {
        var colombia = TimeSpan.FromHours(-5);
        var simulacro = Simulacro.Crear(
            BancoSaber.TituloSimulacro,
            "Prueba de entrenamiento con la estructura del examen Saber 11 del ICFES: 20 preguntas de selección múltiple con única " +
            "respuesta (4 de lectura crítica, 4 de matemáticas, 4 de sociales y ciudadanas, 4 de ciencias naturales y 4 de inglés). " +
            "Tienes 45 minutos. Lee con calma cada pregunta y su texto, descarta las opciones que no son posibles y responde todas. " +
            "Al entregar verás tu puntaje por área y competencia, y recomendaciones de refuerzo en los temas donde te fue más bajo.",
            TipoPrueba.Saber11, [10, 11],
            new DateTimeOffset(2026, 11, 13, 0, 0, 0, colombia),
            new DateTimeOffset(2027, 12, 31, 23, 59, 0, colombia),
            45, mostrarRespuestas: true);
        context.Add(simulacro);

        var orden = 0;
        foreach (var elegida in BancoSaber.DelSimulacro)
        {
            var pregunta = banco.FirstOrDefault(p => p.Area == elegida.Area && p.Contexto == elegida.Contexto && p.Enunciado == elegida.Enunciado);
            if (pregunta is not null)
            {
                context.Add(pregunta.CopiarA(simulacro.Id, ++orden));
            }
        }
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
