using RabiGamaliel.Domain.Simulacros;

namespace RabiGamaliel.Application.Simulacros;

/// <summary>Plan de refuerzo de una competencia con bajo resultado.</summary>
public sealed record CompetenciaRefuerzo(string Competencia, double Porcentaje, IReadOnlyList<string> Temas, string Estrategia);

/// <summary>Recomendación de refuerzo para un área en la que el estudiante salió mal o con bajo resultado.</summary>
public sealed record RecomendacionRefuerzo(
    AreaEvaluada Area, double Porcentaje, string Nivel, string Prioridad, string Mensaje,
    IReadOnlyList<CompetenciaRefuerzo> Competencias, IReadOnlyList<int> PreguntasFalladas);

/// <summary>
/// Recomendaciones de refuerzo al terminar una prueba, según las competencias de los marcos de referencia del ICFES
/// y los Estándares Básicos de Competencias del Ministerio de Educación Nacional.
/// Se recomienda reforzar las áreas con menos del 60 % de aciertos y, dentro de las demás, las competencias con menos del 60 %.
/// </summary>
public static class RecomendacionesSimulacro
{
    public const double UmbralRefuerzo = 60;

    public static IReadOnlyList<RecomendacionRefuerzo> Calcular(
        IReadOnlyList<PreguntaSimulacro> preguntas, IReadOnlyDictionary<Guid, int> respuestas)
    {
        var recomendaciones = new List<RecomendacionRefuerzo>();
        foreach (var area in preguntas.GroupBy(p => p.Area).OrderBy(g => g.Key))
        {
            var lista = area.OrderBy(p => p.Orden).ToList();
            var aciertos = lista.Count(p => CalculoSimulacro.Acerto(p, respuestas));
            var porcentaje = PuntajeSaber.Porcentaje(aciertos, lista.Count);

            var competencias = lista
                .GroupBy(p => p.Competencia ?? string.Empty)
                .Select(g =>
                {
                    var bien = g.Count(p => CalculoSimulacro.Acerto(p, respuestas));
                    return (Nombre: g.Key, Porcentaje: PuntajeSaber.Porcentaje(bien, g.Count()));
                })
                .Where(c => c.Porcentaje < UmbralRefuerzo)
                .OrderBy(c => c.Porcentaje)
                .ToList();

            if (porcentaje >= UmbralRefuerzo && competencias.Count == 0)
            {
                continue;
            }

            var guia = Catalogo.TryGetValue(area.Key, out var g) ? g : GuiaGeneral;
            var refuerzos = competencias
                .Select(c =>
                {
                    var plan = guia.Competencias.FirstOrDefault(x => string.Equals(x.Nombre, c.Nombre, StringComparison.OrdinalIgnoreCase));
                    return new CompetenciaRefuerzo(
                        c.Nombre.Length == 0 ? "Preguntas generales del área" : c.Nombre,
                        c.Porcentaje,
                        plan?.Temas ?? guia.TemasGenerales,
                        plan?.Estrategia ?? guia.EstrategiaGeneral);
                })
                .ToList();

            var falladas = lista.Where(p => !CalculoSimulacro.Acerto(p, respuestas)).Select(p => p.Orden).ToList();
            var prioridad = porcentaje < 40 ? "Alta" : porcentaje < UmbralRefuerzo ? "Media" : "Baja";
            var mensaje = porcentaje < 40
                ? $"En {guia.Nombre} acertaste {aciertos} de {lista.Count}. Es el área que más necesita refuerzo: dedícale tiempo cada día."
                : porcentaje < UmbralRefuerzo
                    ? $"En {guia.Nombre} acertaste {aciertos} de {lista.Count}. Con práctica constante puedes subir al nivel satisfactorio."
                    : $"En {guia.Nombre} vas bien ({aciertos} de {lista.Count}), pero hay competencias que conviene reforzar.";

            recomendaciones.Add(new RecomendacionRefuerzo(area.Key, porcentaje, Nivel(porcentaje), prioridad, mensaje, refuerzos, falladas));
        }

        return [.. recomendaciones.OrderBy(r => r.Porcentaje)];
    }

    /// <summary>Nivel de desempeño aproximado, con la escala de cuatro niveles que usa el ICFES.</summary>
    public static string Nivel(double porcentaje) => porcentaje switch
    {
        < 40 => "Nivel 1 · Insuficiente",
        < 60 => "Nivel 2 · Mínimo",
        < 80 => "Nivel 3 · Satisfactorio",
        _ => "Nivel 4 · Avanzado",
    };

    private sealed record PlanCompetencia(string Nombre, IReadOnlyList<string> Temas, string Estrategia);

    private sealed record GuiaArea(
        string Nombre, IReadOnlyList<PlanCompetencia> Competencias, IReadOnlyList<string> TemasGenerales, string EstrategiaGeneral);

    private static readonly GuiaArea GuiaGeneral = new(
        "esta área", [],
        ["Repasar los temas vistos en clase", "Resolver preguntas tipo Saber del banco del colegio"],
        "Repasa con tu docente las preguntas que fallaste y practica con preguntas similares.");

    private static readonly Dictionary<AreaEvaluada, GuiaArea> Catalogo = new()
    {
        [AreaEvaluada.LecturaCritica] = new(
            "Lectura crítica",
            [
                new(CompetenciasSaber.LcIdentificar,
                    ["Vocabulario en contexto", "Idea principal e ideas secundarias", "Información explícita de textos continuos y discontinuos (tablas, avisos, infografías)"],
                    "Lee un texto corto cada día y escribe en una frase de qué trata; subraya las palabras desconocidas y deduce su significado por el contexto."),
                new(CompetenciasSaber.LcArticular,
                    ["Conectores y su función (contraste, causa, consecuencia, ejemplo)", "Estructura de textos argumentativos y expositivos", "Relación entre párrafos y voces del texto"],
                    "Haz esquemas o mapas de los textos que lees: señala la tesis, los argumentos y la conclusión, y qué conector une cada parte."),
                new(CompetenciasSaber.LcReflexionar,
                    ["Diferencia entre hechos, opiniones y propuestas", "Tesis y argumentos que la apoyan o la debilitan", "Intención comunicativa y tipo de texto", "Textos filosóficos"],
                    "Lee columnas de opinión y pregúntate: ¿qué defiende el autor?, ¿con qué argumentos?, ¿qué dato lo debilitaría?"),
            ],
            ["Comprensión de textos continuos y discontinuos", "Argumentación"],
            "Practica lectura diaria de textos variados (noticias, columnas, cuentos, infografías) y responde preguntas tipo Saber."),
        [AreaEvaluada.Matematicas] = new(
            "Matemáticas",
            [
                new(CompetenciasSaber.MatInterpretar,
                    ["Lectura de tablas y gráficas", "Medidas de tendencia central (media, mediana, moda)", "Funciones lineales: pendiente e intercepto", "Probabilidad simple"],
                    "Toma gráficas de noticias o del DANE y explica con tus palabras qué muestran; calcula el promedio y compara datos."),
                new(CompetenciasSaber.MatFormular,
                    ["Porcentajes y descuentos", "Proporcionalidad directa e inversa (regla de tres)", "Ecuaciones de primer grado", "Teorema de Pitágoras, áreas y perímetros"],
                    "Resuelve problemas de la vida diaria paso a paso: identifica los datos, plantea la operación o ecuación y verifica el resultado."),
                new(CompetenciasSaber.MatArgumentar,
                    ["Validación de afirmaciones con contraejemplos", "Propiedades de los números (pares, impares, divisibilidad)", "Efecto de cambiar medidas en áreas y volúmenes", "Dispersión de datos"],
                    "Ante cada afirmación matemática, pregúntate si siempre se cumple y busca un ejemplo que la confirme o un contraejemplo que la desmienta."),
            ],
            ["Aritmética, álgebra, geometría y estadística básicas"],
            "Practica 5 problemas al día de distintos temas y revisa los procedimientos de los que fallaste."),
        [AreaEvaluada.SocialesCiudadanas] = new(
            "Sociales y ciudadanas",
            [
                new(CompetenciasSaber.SocPensamientoSocial,
                    ["Constitución Política de 1991: principios, derechos y mecanismos de participación y protección (tutela, consulta, referendo)", "Ramas del poder público y organismos de control", "Hitos de la historia de Colombia", "Gobierno escolar"],
                    "Elabora fichas de la Constitución (qué es, para qué sirve, ejemplo) y líneas de tiempo de la historia de Colombia."),
                new(CompetenciasSaber.SocPerspectivas,
                    ["Identificación de actores e intereses en un conflicto", "Análisis de fuentes y sus sesgos", "Consulta previa y derechos de comunidades étnicas"],
                    "En noticias de conflictos, escribe quiénes participan, qué quiere cada uno y qué solución respetaría a todos."),
                new(CompetenciasSaber.SocSistemico,
                    ["Relaciones entre economía, ambiente, política y cultura", "Consecuencias de decisiones y políticas públicas", "Problemas ambientales del territorio"],
                    "Dibuja mapas de causa y consecuencia: a partir de un hecho (p. ej., la deforestación) anota sus efectos en distintas dimensiones."),
            ],
            ["Constitución y ciudadanía", "Historia y geografía de Colombia"],
            "Lee y comenta noticias nacionales relacionándolas con la Constitución y la historia."),
        [AreaEvaluada.CienciasNaturales] = new(
            "Ciencias naturales",
            [
                new(CompetenciasSaber.CnUsoComprensivo,
                    ["Célula y organelos", "Mezclas, sustancias y cambios de la materia", "Movimiento: velocidad y distancia", "Conceptos básicos de biología, química y física"],
                    "Haz mapas conceptuales de cada tema y explícaselos a un compañero; usa ejemplos de tu entorno."),
                new(CompetenciasSaber.CnExplicar,
                    ["Leyes de Newton", "Transferencia de calor y conductividad", "Herencia (cruces de Mendel)", "Relaciones en los ecosistemas y cadenas tróficas"],
                    "Para cada fenómeno cotidiano (por qué se calienta una cuchara, por qué avanza un bote) escribe la explicación científica en tres líneas."),
                new(CompetenciasSaber.CnIndagar,
                    ["Diseño experimental: variables y grupo control", "Interpretación de tablas y gráficas de resultados", "Conclusiones a partir de datos"],
                    "Plantea pequeños experimentos en casa: formula una pregunta, define qué vas a cambiar y qué dejarás igual, registra datos y concluye."),
            ],
            ["Biología, química y física", "Ciencia, tecnología y sociedad"],
            "Repasa los conceptos clave y practica con preguntas que incluyan tablas, gráficas y experimentos."),
        [AreaEvaluada.Ingles] = new(
            "Inglés",
            [
                new(CompetenciasSaber.InAvisos,
                    ["Avisos y letreros públicos (signs)", "Vocabulario de objetos, lugares y profesiones"],
                    "Traduce avisos que encuentres en internet o en lugares turísticos y haz tarjetas de vocabulario con imagen y palabra."),
                new(CompetenciasSaber.InConversacion,
                    ["Expresiones de uso diario (saludos, invitaciones, pedir favores)", "Respuestas adecuadas en diálogos cortos"],
                    "Practica diálogos cortos con un compañero o en voz alta: invitar, aceptar, rechazar, pedir y agradecer."),
                new(CompetenciasSaber.InGramatica,
                    ["Presente simple (tercera persona: -s/-es)", "Pasado simple de verbos regulares e irregulares", "Comparativos y superlativos"],
                    "Escribe cada día cinco oraciones sobre tu rutina, lo que hiciste ayer y comparaciones entre cosas."),
                new(CompetenciasSaber.InLectura,
                    ["Comprensión de textos cortos", "Información específica e idea general"],
                    "Lee textos cortos en inglés (cuentos graduados, noticias para estudiantes) y responde quién, qué, dónde, cuándo y por qué."),
            ],
            ["Vocabulario, gramática y lectura en inglés"],
            "Dedica 20 minutos diarios al inglés: vocabulario, una lectura corta y una canción o video con subtítulos."),
        [AreaEvaluada.Lenguaje] = new(
            "Lenguaje",
            [
                new(CompetenciasSaber.LenComprension,
                    ["Sentido global y enseñanza de un texto", "Tipos de texto y su intención", "Estructura de textos narrativos y argumentativos"],
                    "Después de leer, resume el texto en tres frases y di qué tipo de texto es y para qué fue escrito."),
                new(CompetenciasSaber.LenProduccion,
                    ["Uso de conectores", "Ortografía (b/v, h, tildes)", "Registro formal e informal según el destinatario"],
                    "Escribe textos cortos (cartas, solicitudes, opiniones) y revísalos con una lista: ortografía, conectores y a quién va dirigido."),
            ],
            ["Comprensión lectora y producción escrita"],
            "Lee y escribe todos los días; pide a tu docente que revise tus escritos."),
        [AreaEvaluada.CompetenciasCiudadanas] = new(
            "Competencias ciudadanas",
            [
                new(CompetenciasSaber.CcConocimientos,
                    ["Derechos fundamentales (igualdad, libre desarrollo de la personalidad)", "Manual de convivencia y gobierno escolar"],
                    "Lee el manual de convivencia y la Constitución en sus artículos de derechos; relaciónalos con situaciones del colegio."),
                new(CompetenciasSaber.CcArgumentacion,
                    ["Generalizaciones y prejuicios", "Argumentos con evidencias"],
                    "Cuando des una opinión, apóyala con al menos una razón o un dato, y revisa si estás generalizando."),
                new(CompetenciasSaber.CcMultiperspectivismo,
                    ["Resolución pacífica de conflictos", "Respeto por la diversidad"],
                    "Ante un conflicto, ponte en el lugar de cada persona y busca una solución en la que todos ganen."),
                new(CompetenciasSaber.CcSistemico,
                    ["Consecuencias de las acciones en la comunidad y el ambiente"],
                    "Analiza cómo una acción individual (botar basura, ahorrar agua) afecta a la comunidad a corto y largo plazo."),
            ],
            ["Convivencia, participación y diversidad"],
            "Participa en el gobierno escolar y en actividades de convivencia del colegio."),
    };
}

/// <summary>Nombres de las competencias evaluadas, según los marcos de referencia de las pruebas Saber del ICFES.</summary>
public static class CompetenciasSaber
{
    public const string LcIdentificar = "Identificar y entender los contenidos locales de un texto";
    public const string LcArticular = "Comprender cómo se articulan las partes de un texto";
    public const string LcReflexionar = "Reflexionar a partir de un texto y evaluar su contenido";

    public const string MatInterpretar = "Interpretación y representación";
    public const string MatFormular = "Formulación y ejecución";
    public const string MatArgumentar = "Argumentación";

    public const string SocPensamientoSocial = "Pensamiento social";
    public const string SocPerspectivas = "Interpretación y análisis de perspectivas";
    public const string SocSistemico = "Pensamiento reflexivo y sistémico";

    public const string CnUsoComprensivo = "Uso comprensivo del conocimiento científico";
    public const string CnExplicar = "Explicación de fenómenos";
    public const string CnIndagar = "Indagación";

    public const string InAvisos = "Avisos y vocabulario";
    public const string InConversacion = "Conversaciones cotidianas";
    public const string InGramatica = "Gramática y uso del idioma";
    public const string InLectura = "Comprensión de lectura";

    public const string LenComprension = "Comprensión lectora";
    public const string LenProduccion = "Producción escrita";

    public const string CcConocimientos = "Conocimientos ciudadanos";
    public const string CcArgumentacion = "Argumentación";
    public const string CcMultiperspectivismo = "Multiperspectivismo";
    public const string CcSistemico = "Pensamiento sistémico";
}
