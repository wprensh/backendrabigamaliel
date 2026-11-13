using RabiGamaliel.Application.Simulacros;
using RabiGamaliel.Domain.Simulacros;
using C = RabiGamaliel.Application.Simulacros.CompetenciasSaber;

namespace RabiGamaliel.Infrastructure.Persistence.Seed;

/// <summary>
/// Banco inicial de preguntas tipo Saber, escritas para la Fundación Educativa Rabí Gamaliel siguiendo las competencias
/// de los marcos de referencia del ICFES y los Estándares Básicos de Competencias del MEN (selección múltiple con única
/// respuesta, cuatro opciones). Las docentes y los docentes pueden corregirlas o agregar más desde el panel.
/// </summary>
internal static class BancoSaber
{
    public const string TituloSimulacro = "Simulacro Saber 11 · Prueba de entrenamiento";

    private static readonly int[] Media = [10, 11];
    private static readonly int[] Noveno = [9];

    // ---------- Textos compartidos ----------
    private const string TextoManglares =
        "Durante siglos, los manglares de la costa Caribe fueron vistos como terrenos improductivos que convenía talar para construir " +
        "puertos o estanques camaroneros. Hoy sabemos que cumplen funciones que ninguna obra humana reemplaza con facilidad: frenan la " +
        "erosión, filtran el agua que llega al mar y sirven de criadero a buena parte de los peces que luego capturan los pescadores " +
        "artesanales. Sin embargo, conocer su valor no ha bastado para protegerlos. Mientras la tala sea más rentable a corto plazo que la " +
        "conservación, advierten algunos investigadores, los manglares seguirán retrocediendo. Por eso proponen que las comunidades que los " +
        "cuidan reciban beneficios económicos directos, como el pago por servicios ambientales o el ecoturismo controlado.";

    private const string TextoPantallas =
        "Algunos sostienen que leer en pantallas nos ha vuelto lectores superficiales. Yo no estoy tan seguro. Es verdad que saltamos de un " +
        "enlace a otro y que rara vez terminamos un artículo largo; pero también es cierto que nunca antes tantos jóvenes habían escrito a " +
        "diario, aunque sea en mensajes breves. Quizá el problema no sea la pantalla, sino que la escuela todavía no enseña a leer en ella " +
        "con la misma paciencia con que enseñó a leer en el papel.";

    private const string TextoMineria =
        "Una empresa quiere explotar oro cerca de un río en un territorio habitado por una comunidad indígena. La comunidad teme que el " +
        "agua se contamine; la alcaldía espera que el proyecto genere empleos e impuestos; algunos campesinos de la zona están divididos.";

    private const string TextoMaria =
        "Maria lives in Cartagena. Every Saturday she helps her grandmother sell fruit at the market. She likes it because she practices " +
        "math and talks with tourists in English. Next year she wants to study tourism at university.";

    private const string TextoFabula =
        "Durante todo el verano, una hormiga trabajó guardando granos para el invierno, mientras una cigarra pasaba los días cantando. " +
        "Cuando llegó el frío, la cigarra, sin nada que comer, fue a pedirle ayuda a la hormiga.";

    private sealed record P(
        AreaEvaluada Area, string Competencia, string? Contexto, string Enunciado, string[] Opciones, int Correcta, string Explicacion,
        int[] Grados, DificultadPregunta Dificultad);

    private static readonly P[] Preguntas =
    [
        // ===================== LECTURA CRÍTICA (Saber 11) =====================
        new(AreaEvaluada.LecturaCritica, C.LcIdentificar, TextoManglares,
            "Según el texto, ¿qué funciones cumplen los manglares?",
            ["Frenan la erosión, filtran el agua y sirven de criadero de peces.", "Proveen madera para la construcción de puertos.",
             "Sirven de terreno para instalar estanques camaroneros.", "Aumentan la cantidad de agua que llega al mar."],
            0, "El texto enumera tres funciones: frenar la erosión, filtrar el agua y servir de criadero de peces.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.LecturaCritica, C.LcArticular, TextoManglares,
            "En el texto, la expresión «Sin embargo» introduce",
            ["una idea que contrasta con la anterior: saber que los manglares son valiosos no ha bastado para protegerlos.",
             "un ejemplo de las funciones que cumplen los manglares.", "la conclusión que resume todo el texto.",
             "la causa por la cual los manglares fueron talados en el pasado."],
            0, "«Sin embargo» es un conector de contraste: opone el valor conocido de los manglares a que siguen desapareciendo.", Media, DificultadPregunta.Media),
        new(AreaEvaluada.LecturaCritica, C.LcReflexionar, TextoManglares,
            "¿Cuál de los siguientes enunciados del texto es una propuesta y no una descripción?",
            ["Las comunidades que cuidan los manglares deberían recibir beneficios económicos directos.",
             "Los manglares frenan la erosión.", "Los manglares fueron vistos como terrenos improductivos.",
             "Los manglares sirven de criadero a buena parte de los peces."],
            0, "Solo la primera opción plantea algo que se debería hacer; las demás describen hechos.", Media, DificultadPregunta.Media),
        new(AreaEvaluada.LecturaCritica, C.LcReflexionar, TextoManglares,
            "¿Cuál de las siguientes situaciones, de ser cierta, debilitaría la propuesta de los investigadores?",
            ["En las zonas donde se pagó a las comunidades por conservar el manglar, la tala siguió al mismo ritmo.",
             "Los manglares filtran el agua que llega al mar.", "Los pescadores artesanales dependen de los peces que nacen en el manglar.",
             "El ecoturismo controlado atrae visitantes a la región."],
            0, "La propuesta supone que los beneficios económicos frenan la tala; si la tala no disminuye con el pago, el argumento se debilita.", Media, DificultadPregunta.Alta),
        new(AreaEvaluada.LecturaCritica, C.LcIdentificar, TextoManglares,
            "En el texto, la palabra «retrocediendo» significa",
            ["disminuyendo su extensión.", "caminando hacia atrás.", "recuperando su tamaño original.", "trasladándose a otras costas."],
            0, "Por el contexto (la tala), «retroceder» indica que los manglares ocupan cada vez menos territorio.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.LecturaCritica, C.LcIdentificar, TextoPantallas,
            "¿Cuál es la idea que defiende el autor?",
            ["El problema no es la pantalla, sino que la escuela aún no enseña a leer en ella con paciencia.",
             "Leer en pantallas nos ha vuelto lectores superficiales.", "Los jóvenes de hoy ya no escriben.",
             "Hay que dejar las pantallas y volver a leer solo en papel."],
            0, "La tesis aparece al final: el problema no es la pantalla, sino la falta de enseñanza para leer en ella.", Media, DificultadPregunta.Media),
        new(AreaEvaluada.LecturaCritica, C.LcArticular, TextoPantallas,
            "La frase «Yo no estoy tan seguro» cumple la función de",
            ["tomar distancia de la opinión presentada al inicio del texto.", "reconocer que el autor se equivocó antes.",
             "presentar un dato que comprueba la idea inicial.", "cerrar el texto con una conclusión."],
            0, "Con esa frase el autor anuncia que no comparte la opinión de «algunos» y que va a matizarla.", Media, DificultadPregunta.Media),
        new(AreaEvaluada.LecturaCritica, C.LcReflexionar, TextoPantallas,
            "¿Qué tipo de texto es el fragmento?",
            ["Una columna de opinión, porque el autor presenta y defiende un punto de vista.", "Una noticia, porque informa un hecho reciente.",
             "Un texto instructivo, porque indica pasos para leer en pantallas.", "Un informe científico, porque presenta datos de un experimento."],
            0, "El autor expresa su postura («Yo no estoy tan seguro», «Quizá el problema…») y la argumenta: es un texto de opinión.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.LecturaCritica, C.LcReflexionar,
            "Cartel en un colegio: «¡Apaga la luz al salir del salón! El 30 % de la energía del colegio se gasta en aulas vacías».",
            "¿Cuál es el propósito principal del cartel?",
            ["Persuadir a la comunidad para que ahorre energía.", "Informar cuánto cuesta la energía en el colegio.",
             "Explicar cómo funciona la instalación eléctrica.", "Prohibir el uso de la luz en los salones."],
            0, "El cartel usa un dato para convencer de una acción: es un texto persuasivo.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.LecturaCritica, C.LcReflexionar,
            "Se atribuye a Sócrates la frase: «Solo sé que no sé nada».",
            "¿Cuál interpretación es más coherente con el pensamiento socrático?",
            ["Reconocer los límites del propio saber es el punto de partida para buscar el conocimiento.",
             "Es imposible que el ser humano conozca algo.", "Sócrates no había estudiado y por eso no sabía nada.",
             "El conocimiento no tiene ningún valor para la vida."],
            0, "Para Sócrates, admitir la propia ignorancia abre el camino a preguntar y a investigar (método socrático).", Media, DificultadPregunta.Alta),

        // ===================== MATEMÁTICAS (Saber 11) =====================
        new(AreaEvaluada.Matematicas, C.MatFormular, null,
            "Un celular cuesta $800.000 y tiene un descuento del 15 %. ¿Cuánto se paga por él?",
            ["$680.000", "$120.000", "$785.000", "$920.000"],
            0, "El descuento es 800.000 × 0,15 = 120.000; se paga 800.000 − 120.000 = 680.000.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.Matematicas, C.MatInterpretar,
            "Empanadas vendidas en la tienda escolar: lunes 12, martes 18, miércoles 9, jueves 15 y viernes 21.",
            "¿Cuál fue el promedio diario de empanadas vendidas?",
            ["15", "18", "12", "75"],
            0, "Suma: 12 + 18 + 9 + 15 + 21 = 75; promedio: 75 ÷ 5 = 15.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.Matematicas, C.MatArgumentar, null,
            "Un estudiante afirma: «Si duplico el lado de un cuadrado, su área también se duplica». ¿Es correcta la afirmación?",
            ["No, porque el área se cuadruplica: (2l)² = 4l².", "Sí, porque el lado y el área son directamente proporcionales.",
             "Sí, porque el perímetro también se duplica.", "No, porque el área se triplica."],
            0, "El área de un cuadrado es l²; si el lado es 2l, el área es 4l², es decir, cuatro veces mayor.", Media, DificultadPregunta.Media),
        new(AreaEvaluada.Matematicas, C.MatFormular, null,
            "Un taxi cobra $5.000 de arranque y $1.200 por cada kilómetro. Si un pasajero pagó $17.000, ¿cuántos kilómetros recorrió?",
            ["10", "14", "12", "8"],
            0, "5.000 + 1.200k = 17.000 → 1.200k = 12.000 → k = 10.", Media, DificultadPregunta.Media),
        new(AreaEvaluada.Matematicas, C.MatInterpretar, null,
            "En una bolsa hay 3 balotas rojas, 5 azules y 2 verdes. Si se saca una al azar, ¿cuál es la probabilidad de que sea azul?",
            ["1/2", "1/5", "5/3", "3/10"],
            0, "Hay 5 azules de 10 balotas: 5/10 = 1/2.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.Matematicas, C.MatFormular, null,
            "Una escalera de 5 m está apoyada en una pared vertical y su base queda a 3 m de la pared. ¿A qué altura toca la pared?",
            ["4 m", "2 m", "8 m", "5,8 m"],
            0, "Por el teorema de Pitágoras: h² = 5² − 3² = 25 − 9 = 16, h = 4 m.", Media, DificultadPregunta.Media),
        new(AreaEvaluada.Matematicas, C.MatArgumentar,
            "Notas de tres estudiantes del curso A: 3,0; 3,5; 4,0. Notas de tres estudiantes del curso B: 1,5; 3,5; 5,5.",
            "¿Cuál afirmación es correcta?",
            ["Los dos cursos tienen el mismo promedio, pero las notas del curso B están más dispersas.",
             "El curso B tiene mejor promedio porque tiene la nota más alta.", "El curso A tiene mejor promedio porque no tiene notas bajas.",
             "Los dos cursos tienen el mismo promedio y la misma dispersión."],
            0, "Ambos promedios son 3,5; el rango de A es 1,0 y el de B es 4,0, por eso B es más disperso.", Media, DificultadPregunta.Alta),
        new(AreaEvaluada.Matematicas, C.MatInterpretar, null,
            "El costo de un parqueadero, en miles de pesos, es C(x) = 2x + 3, donde x es el número de horas. ¿Qué representa el número 2?",
            ["Que el costo aumenta $2.000 por cada hora.", "Que la primera hora cuesta $2.000.",
             "Que se pueden parquear máximo 2 horas.", "Que el costo total siempre es $2.000."],
            0, "En una función lineal, la pendiente (2) indica cuánto cambia el costo por cada hora adicional.", Media, DificultadPregunta.Media),
        new(AreaEvaluada.Matematicas, C.MatFormular, null,
            "Con 3 tazas de harina se preparan 12 galletas. ¿Cuántas tazas se necesitan para 30 galletas?",
            ["7,5", "10", "6", "9"],
            0, "Cada galleta usa 3/12 = 0,25 tazas; 30 × 0,25 = 7,5 tazas.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.Matematicas, C.MatArgumentar, null,
            "¿Por qué el producto de un número par por cualquier número entero siempre es par?",
            ["Porque un número par es 2k, y 2k · n = 2(k · n), que es múltiplo de 2.", "Porque todos los números enteros son pares.",
             "Porque al multiplicar siempre se obtiene un número mayor.", "Porque los números impares no se pueden multiplicar."],
            0, "Todo par se escribe 2k; al multiplicarlo por n el resultado conserva el factor 2.", Media, DificultadPregunta.Alta),

        // ===================== SOCIALES Y CIUDADANAS (Saber 11) =====================
        new(AreaEvaluada.SocialesCiudadanas, C.SocPensamientoSocial, null,
            "Según el artículo 1 de la Constitución Política de 1991, Colombia es",
            ["un Estado social de derecho, organizado como república unitaria y descentralizada.", "una república federal formada por estados independientes.",
             "una monarquía parlamentaria.", "un Estado confesional con una religión oficial."],
            0, "El artículo 1 define a Colombia como Estado social de derecho, república unitaria, descentralizada, con autonomía de sus entidades territoriales.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.SocialesCiudadanas, C.SocPensamientoSocial, null,
            "Una persona considera que se está violando su derecho fundamental a la salud y necesita una protección rápida. ¿Qué mecanismo debe usar?",
            ["La acción de tutela.", "El referendo.", "El cabildo abierto.", "La revocatoria del mandato."],
            0, "La tutela (art. 86) protege de forma inmediata los derechos fundamentales; los otros son mecanismos de participación.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.SocialesCiudadanas, C.SocPensamientoSocial, null,
            "¿Qué rama del poder público tiene como función principal hacer las leyes?",
            ["La rama legislativa (Congreso de la República).", "La rama ejecutiva (Presidencia).", "La rama judicial (jueces y cortes).",
             "Los organismos de control (Procuraduría y Contraloría)."],
            0, "El Congreso, rama legislativa, hace las leyes; el ejecutivo las ejecuta y el judicial las aplica en casos concretos.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.SocialesCiudadanas, C.SocPerspectivas, TextoMineria,
            "¿Qué interés defiende principalmente la comunidad indígena?",
            ["La protección de su territorio y del agua de la que depende.", "La generación de empleo en la zona.",
             "El aumento de los impuestos del municipio.", "La venta del oro en el exterior."],
            0, "La comunidad teme la contaminación del río: su interés es proteger el territorio y el agua.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.SocialesCiudadanas, C.SocPerspectivas, TextoMineria,
            "¿Qué solución tiene en cuenta las perspectivas de todos los actores?",
            ["Realizar la consulta previa con la comunidad, un estudio ambiental independiente y acuerdos sobre empleos y beneficios.",
             "Aprobar el proyecto porque genera empleos e impuestos.", "Prohibir cualquier actividad económica en la región.",
             "Dejar que la empresa decida sola cómo manejar el río."],
            0, "La consulta previa es un derecho de los pueblos étnicos; combinarla con estudios y acuerdos atiende todos los intereses.", Media, DificultadPregunta.Media),
        new(AreaEvaluada.SocialesCiudadanas, C.SocSistemico, null,
            "Si el precio internacional del café baja mucho durante varios años, ¿qué consecuencia es más probable en las regiones cafeteras?",
            ["Menos ingresos para las familias campesinas, que podrían cambiar de cultivo o migrar.", "Aumento inmediato de los salarios de los recolectores.",
             "Más turismo internacional en la zona.", "Ningún efecto, porque el café solo se consume en Colombia."],
            0, "Un precio bajo reduce los ingresos de los productores, lo que afecta la economía, el empleo y la migración en la región.", Media, DificultadPregunta.Media),
        new(AreaEvaluada.SocialesCiudadanas, C.SocPensamientoSocial, null,
            "¿Qué hecho de 1810 se asocia con el inicio del proceso de independencia en la Nueva Granada?",
            ["El grito de independencia del 20 de julio en Santafé.", "La Batalla de Boyacá.",
             "La expedición de la Constitución de 1991.", "La fundación de Cartagena de Indias."],
            0, "El 20 de julio de 1810 se instaló la Junta de Santafé; la Batalla de Boyacá ocurrió en 1819.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.SocialesCiudadanas, C.SocSistemico, null,
            "¿Cuál es una consecuencia de la deforestación de la Amazonía?",
            ["Pérdida de biodiversidad, cambios en las lluvias y aumento de gases de efecto invernadero.", "Aumento de la biodiversidad de la región.",
             "Mayor regulación del agua en los ríos.", "Disminución de la temperatura del planeta."],
            0, "La selva regula el agua y almacena carbono; talarla afecta especies, lluvias y clima.", Media, DificultadPregunta.Media),
        new(AreaEvaluada.SocialesCiudadanas, C.SocPerspectivas, null,
            "Un historiador estudia una guerra usando como única fuente el diario de un general de uno de los bandos. ¿Qué limitación tiene su estudio?",
            ["Solo muestra la perspectiva de uno de los actores del conflicto.", "Los diarios nunca sirven como fuente histórica.",
             "Los generales no participan en las guerras.", "Ninguna, porque una sola fuente es suficiente."],
            0, "Toda fuente tiene un punto de vista; contrastar varias fuentes evita sesgos.", Media, DificultadPregunta.Media),
        new(AreaEvaluada.SocialesCiudadanas, C.SocPensamientoSocial, null,
            "En el gobierno escolar, ¿quién promueve los derechos y deberes de los estudiantes y es elegido por el voto de todos ellos?",
            ["El personero estudiantil.", "El rector.", "El consejo académico.", "La asociación de padres de familia."],
            0, "El personero estudiantil, elegido por los estudiantes, promueve sus derechos y deberes (Decreto 1860 de 1994).", Media, DificultadPregunta.Baja),

        // ===================== CIENCIAS NATURALES (Saber 11) =====================
        new(AreaEvaluada.CienciasNaturales, C.CnUsoComprensivo, null,
            "¿En qué organelo de la célula vegetal se realiza la fotosíntesis?",
            ["Cloroplasto.", "Mitocondria.", "Núcleo.", "Ribosoma."],
            0, "Los cloroplastos contienen clorofila y transforman la energía de la luz en energía química.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.CienciasNaturales, C.CnExplicar, null,
            "En una sopa caliente se ponen una cuchara de metal y una de madera. ¿Por qué el mango de la de metal se calienta más rápido?",
            ["Porque el metal conduce mejor el calor que la madera.", "Porque la madera produce frío.",
             "Porque el metal pesa más que la madera.", "Porque la madera absorbe la sopa."],
            0, "Los metales son buenos conductores térmicos; la madera es aislante.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.CienciasNaturales, C.CnIndagar, null,
            "Una estudiante quiere saber si la luz influye en el crecimiento de las plantas de frijol. ¿Qué experimento es el más adecuado?",
            ["Dos grupos de plantas iguales, con la misma agua, tierra y temperatura: uno con luz y otro sin luz.",
             "Un grupo de plantas con luz y mucha agua, y otro sin luz y sin agua.", "Una sola planta con luz, midiéndola un día.",
             "Plantas de distintas especies, cada una en un lugar diferente."],
            0, "Solo debe cambiar la variable que se estudia (la luz); las demás se mantienen iguales para poder comparar.", Media, DificultadPregunta.Media),
        new(AreaEvaluada.CienciasNaturales, C.CnUsoComprensivo, null,
            "¿Cuál de las siguientes es una mezcla homogénea?",
            ["Agua con sal disuelta.", "Agua con aceite.", "Arena con agua.", "Una ensalada de frutas."],
            0, "En una mezcla homogénea no se distinguen sus componentes; la sal disuelta se distribuye uniformemente.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.CienciasNaturales, C.CnUsoComprensivo, null,
            "Un ciclista avanza a una velocidad constante de 20 m/s durante 5 s. ¿Qué distancia recorre?",
            ["100 m", "4 m", "25 m", "15 m"],
            0, "Distancia = velocidad × tiempo = 20 m/s × 5 s = 100 m.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.CienciasNaturales, C.CnExplicar, null,
            "Se cruzan dos plantas heterocigotas (Aa × Aa), donde A (flor morada) domina sobre a (flor blanca). ¿Qué proporción de la descendencia se espera?",
            ["3 moradas por cada 1 blanca.", "Todas moradas.", "1 morada por cada 1 blanca.", "Todas blancas."],
            0, "El cuadro de Punnett da AA, Aa, Aa, aa: tres con el alelo dominante (moradas) y una blanca.", Media, DificultadPregunta.Media),
        new(AreaEvaluada.CienciasNaturales, C.CnIndagar,
            "Gramos de azúcar que se disuelven en 100 g de agua: a 20 °C, 200 g; a 40 °C, 240 g; a 60 °C, 290 g.",
            "¿Qué conclusión se puede sacar de estos datos?",
            ["La cantidad de azúcar que se disuelve aumenta con la temperatura.", "El azúcar no se disuelve en agua fría.",
             "La temperatura no influye en la cantidad de azúcar disuelta.", "A mayor temperatura se disuelve menos azúcar."],
            0, "Los datos muestran que al subir la temperatura se disuelve más azúcar: la solubilidad aumenta.", Media, DificultadPregunta.Media),
        new(AreaEvaluada.CienciasNaturales, C.CnExplicar, null,
            "En la cadena alimenticia pasto → conejo → zorro, ¿qué pasaría a corto plazo si desaparecen los zorros?",
            ["Aumentaría la población de conejos y disminuiría el pasto.", "Disminuiría la población de conejos.",
             "Aumentaría el pasto y desaparecerían los conejos.", "No habría ningún cambio en el ecosistema."],
            0, "Sin depredador, los conejos aumentan y consumen más pasto.", Media, DificultadPregunta.Media),
        new(AreaEvaluada.CienciasNaturales, C.CnExplicar, null,
            "¿Por qué se recomienda no quemar basura a cielo abierto?",
            ["Porque libera gases y partículas contaminantes que dañan el aire y la salud.", "Porque la basura no se puede quemar.",
             "Porque el humo enfría el ambiente.", "Porque la ceniza es un buen abono para los cultivos."],
            0, "La quema produce gases tóxicos y material particulado que causan enfermedades respiratorias y contaminan el aire.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.CienciasNaturales, C.CnExplicar, null,
            "Al remar, el bote avanza hacia adelante. ¿Cuál es la explicación?",
            ["El remo empuja el agua hacia atrás y el agua empuja el bote hacia adelante (acción y reacción).",
             "El agua siempre se mueve hacia adelante.", "El remo hace que el bote pese menos.", "El viento empuja el remo."],
            0, "Tercera ley de Newton: a toda acción corresponde una reacción de igual magnitud y sentido contrario.", Media, DificultadPregunta.Media),

        // ===================== INGLÉS (Saber 11) =====================
        new(AreaEvaluada.Ingles, C.InAvisos, "Sign: «PLEASE KEEP OFF THE GRASS».",
            "What does the sign mean?",
            ["You should not walk on the grass.", "You can play on the grass.", "The grass is for sale.", "You must cut the grass."],
            0, "«Keep off» means «stay away from»: you should not walk on the grass.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.Ingles, C.InAvisos, "Sign: «NO FOOD OR DRINKS IN THE LAB».",
            "What does the sign mean?",
            ["You cannot eat or drink in the laboratory.", "The laboratory sells food and drinks.", "You must bring food to the laboratory.",
             "Drinks are free in the laboratory."],
            0, "The sign forbids eating and drinking in the lab.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.Ingles, C.InAvisos, null,
            "You use it to cut paper. What is it?",
            ["Scissors", "A ruler", "An eraser", "Glue"],
            0, "Scissors are used to cut paper.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.Ingles, C.InConversacion, null,
            "— How about going to the cinema tonight?\n— ______",
            ["That's a great idea!", "I'm fine, thank you.", "It's ten o'clock.", "Yes, I am."],
            0, "To accept an invitation we say «That's a great idea!».", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.Ingles, C.InConversacion, null,
            "— Can I borrow your pen?\n— ______",
            ["Sure, here you are.", "No, I'm not.", "It's blue.", "I'm fifteen."],
            0, "«Sure, here you are» is the natural answer when someone asks to borrow something.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.Ingles, C.InGramatica, null,
            "She ______ to school every day.",
            ["goes", "go", "going", "gone"],
            0, "Simple present, third person singular: she goes.", Media, DificultadPregunta.Baja),
        new(AreaEvaluada.Ingles, C.InGramatica, null,
            "Yesterday we ______ a football match on TV.",
            ["watched", "watch", "watches", "are watching"],
            0, "«Yesterday» requires the simple past: watched.", Media, DificultadPregunta.Media),
        new(AreaEvaluada.Ingles, C.InGramatica, null,
            "This book is ______ than that one.",
            ["more interesting", "most interesting", "interestinger", "the more interesting"],
            0, "Long adjectives form the comparative with «more … than».", Media, DificultadPregunta.Media),
        new(AreaEvaluada.Ingles, C.InLectura, TextoMaria,
            "Why does Maria like helping her grandmother?",
            ["Because she practices math and talks with tourists in English.", "Because she wants to sell fruit all her life.",
             "Because her grandmother pays her a lot of money.", "Because she doesn't like going to school."],
            0, "The text says: «She likes it because she practices math and talks with tourists in English».", Media, DificultadPregunta.Media),
        new(AreaEvaluada.Ingles, C.InLectura, TextoMaria,
            "What does Maria want to do next year?",
            ["Study tourism at university.", "Open her own fruit market.", "Move to another country.", "Teach English to tourists."],
            0, "The last sentence says she wants to study tourism at university.", Media, DificultadPregunta.Baja),

        // ===================== LENGUAJE (Saber 9) =====================
        new(AreaEvaluada.Lenguaje, C.LenComprension, TextoFabula,
            "¿Cuál es la enseñanza de la fábula?",
            ["Es importante prever y trabajar pensando en el futuro.", "Cantar es más importante que trabajar.",
             "Las hormigas y las cigarras no pueden ser amigas.", "El invierno es la estación más difícil del año."],
            0, "La fábula contrasta a quien se prepara (la hormiga) con quien no lo hace (la cigarra).", Noveno, DificultadPregunta.Baja),
        new(AreaEvaluada.Lenguaje, C.LenComprension, TextoFabula,
            "El texto anterior es una fábula porque",
            ["sus personajes son animales que actúan como personas y deja una enseñanza.", "explica cómo viven las hormigas en la naturaleza.",
             "da instrucciones para guardar alimentos.", "cuenta un hecho real ocurrido en el pasado."],
            0, "Las fábulas tienen animales personificados y una moraleja.", Noveno, DificultadPregunta.Baja),
        new(AreaEvaluada.Lenguaje, C.LenProduccion, null,
            "¿Qué palabra completa mejor la oración? «Quería ir al paseo, ______ estaba enfermo.»",
            ["pero", "porque", "además", "entonces"],
            0, "«Pero» expresa contraste entre querer ir y no poder por estar enfermo.", Noveno, DificultadPregunta.Baja),
        new(AreaEvaluada.Lenguaje, C.LenProduccion, null,
            "¿Cuál oración está escrita correctamente?",
            ["Mañana habrá una reunión de padres.", "Mañana abrá una reunión de padres.", "Mañana havrá una reunión de padres.",
             "Mañana habra una reunión de padres."],
            0, "«Habrá» (del verbo haber) se escribe con h, con b y con tilde.", Noveno, DificultadPregunta.Media),
        new(AreaEvaluada.Lenguaje, C.LenProduccion, null,
            "Un grupo de estudiantes escribe al rector para solicitar arreglar la cancha. ¿Qué inicio es el más adecuado?",
            ["Respetado señor rector: le escribimos para solicitar el arreglo de la cancha.", "¡Hola, profe! Arregle la cancha ya.",
             "Rector, la cancha está fea.", "Querido amigo: ¿qué más? Te cuento que la cancha está dañada."],
            0, "Una carta a una autoridad exige registro formal y una solicitud respetuosa.", Noveno, DificultadPregunta.Media),
        new(AreaEvaluada.Lenguaje, C.LenComprension, null,
            "En un texto argumentativo, ¿qué función cumple generalmente el último párrafo?",
            ["Presentar la conclusión que retoma la tesis.", "Presentar a los personajes.", "Describir el lugar de los hechos.",
             "Dar las instrucciones de uso."],
            0, "El texto argumentativo cierra con una conclusión que reafirma la tesis.", Noveno, DificultadPregunta.Media),

        // ===================== COMPETENCIAS CIUDADANAS (Saber 9) =====================
        new(AreaEvaluada.CompetenciasCiudadanas, C.CcConocimientos, null,
            "¿Qué derecho protege a las personas para que no sean discriminadas por su sexo, raza, origen o religión?",
            ["El derecho a la igualdad.", "El derecho a la propiedad.", "El derecho al trabajo.", "El derecho a la libre circulación."],
            0, "El artículo 13 de la Constitución establece la igualdad y prohíbe la discriminación.", Noveno, DificultadPregunta.Baja),
        new(AreaEvaluada.CompetenciasCiudadanas, C.CcMultiperspectivismo, null,
            "En el descanso, dos grupos discuten porque ambos quieren usar el único balón. ¿Cuál es la mejor solución?",
            ["Dialogar y acordar turnos o jugar juntos.", "Que el grupo más grande se quede con el balón.",
             "Esconder el balón para que nadie juegue.", "Resolverlo a empujones."],
            0, "Una solución pacífica y concertada tiene en cuenta los intereses de ambos grupos.", Noveno, DificultadPregunta.Baja),
        new(AreaEvaluada.CompetenciasCiudadanas, C.CcArgumentacion, null,
            "Alguien afirma: «Todos los jóvenes son irresponsables». ¿Qué problema tiene esta afirmación?",
            ["Es una generalización que no se apoya en evidencias.", "Es un dato comprobado por la ciencia.",
             "Es una pregunta y no una afirmación.", "No tiene ningún problema."],
            0, "Atribuir una característica a todo un grupo sin pruebas es una generalización y puede ser un prejuicio.", Noveno, DificultadPregunta.Media),
        new(AreaEvaluada.CompetenciasCiudadanas, C.CcSistemico, null,
            "Si las familias de un barrio dejan de separar los residuos, ¿qué consecuencia es más probable?",
            ["Más basura en los rellenos sanitarios, más contaminación y menos ingresos para los recicladores.", "Las calles quedan más limpias.",
             "Aumenta el reciclaje en la ciudad.", "Ninguna, porque separar residuos no tiene efectos."],
            0, "No separar afecta el ambiente, la vida útil de los rellenos y el trabajo de los recicladores.", Noveno, DificultadPregunta.Media),
        new(AreaEvaluada.CompetenciasCiudadanas, C.CcConocimientos, null,
            "¿Qué es el manual de convivencia de un colegio?",
            ["El documento que establece los derechos, deberes y normas de convivencia de la comunidad educativa.",
             "El horario de clases del año.", "La lista de útiles escolares.", "El reglamento de la tienda escolar."],
            0, "El manual de convivencia (Ley 115 de 1994) recoge los acuerdos de convivencia de la comunidad educativa.", Noveno, DificultadPregunta.Baja),
        new(AreaEvaluada.CompetenciasCiudadanas, C.CcMultiperspectivismo, null,
            "Una compañera nueva, que viene de otra región, recibe burlas por su forma de hablar. ¿Qué actitud es la más adecuada?",
            ["Reconocer la diversidad, no participar en las burlas y pedir respeto para ella.", "Reírse para no quedar mal con el grupo.",
             "Ignorar la situación porque no es asunto propio.", "Pedirle que cambie su forma de hablar."],
            0, "Respetar la diversidad cultural y actuar frente a la discriminación es parte de la convivencia.", Noveno, DificultadPregunta.Baja),
    ];

    /// <summary>
    /// Preguntas del simulacro de 20 preguntas (4 por área de Saber 11), en el orden del ICFES. Índices sobre <see cref="Preguntas"/>.
    /// </summary>
    private static readonly int[] PreguntasDelSimulacro =
    [
        0, 1, 3, 5,     // Lectura crítica
        10, 12, 14, 15, // Matemáticas
        21, 23, 25, 28, // Sociales y ciudadanas
        30, 32, 35, 37, // Ciencias naturales
        40, 43, 46, 48, // Inglés
    ];

    public static IEnumerable<PreguntaBanco> Faltantes(IReadOnlySet<string> enunciadosExistentes) =>
        Preguntas.Where(p => !enunciadosExistentes.Contains(Clave(p.Area, p.Contexto, p.Enunciado))).Select(Crear);

    public static string Clave(AreaEvaluada area, string? contexto, string enunciado) => $"{area}|{contexto}|{enunciado}";

    public static IReadOnlyList<P2> DelSimulacro => [.. PreguntasDelSimulacro.Select(i => new P2(Preguntas[i].Area, Preguntas[i].Contexto, Preguntas[i].Enunciado))];

    public sealed record P2(AreaEvaluada Area, string? Contexto, string Enunciado);

    public static int Total => Preguntas.Length;

    private static PreguntaBanco Crear(P p) =>
        PreguntaBanco.Crear(p.Area, p.Competencia, p.Contexto, p.Enunciado, null, BarajarOpciones(p, out var correcta), correcta, p.Explicacion,
            p.Grados, p.Dificultad);

    /// <summary>
    /// Las preguntas se escriben con la respuesta correcta de primera; aquí se reparte su posición (A, B, C o D) de forma fija
    /// según el enunciado, para que no quede siempre en la misma letra.
    /// </summary>
    private static string[] BarajarOpciones(P p, out int correcta)
    {
        var desplazamiento = (int)((uint)Estable(p.Enunciado) % (uint)p.Opciones.Length);
        var opciones = new string[p.Opciones.Length];
        for (var i = 0; i < p.Opciones.Length; i++)
        {
            opciones[(i + desplazamiento) % p.Opciones.Length] = p.Opciones[i];
        }

        correcta = (p.Correcta + desplazamiento) % p.Opciones.Length;
        return opciones;
    }

    /// <summary>Hash estable (FNV-1a): string.GetHashCode cambia en cada ejecución.</summary>
    private static int Estable(string texto)
    {
        unchecked
        {
            var hash = (int)2166136261;
            foreach (var c in texto)
            {
                hash = (hash ^ c) * 16777619;
            }

            return hash;
        }
    }
}
