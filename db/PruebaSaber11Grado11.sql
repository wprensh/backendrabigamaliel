-- Prueba Saber 11 para grado 11: 20 preguntas tomadas al azar del banco, 4 por área, en el orden del ICFES.
-- Idempotente: si el simulacro ya existe no se crea otra vez, y solo se llenan las preguntas si está vacío.
-- Requiere BancoSaber11.sql (o preguntas propias en el banco para grado 11).
INSERT INTO colegio.simulacros (id, titulo, instrucciones, tipo, grados, inicio, fin, duracion_minutos, mostrar_respuestas, creado_en)
SELECT '7c02c541-642a-5067-a3d9-c69c3ac90c3f', 'Prueba Saber 11 - Grado 11',
       'Responda las 20 preguntas (4 por área). Cada pregunta tiene una sola respuesta correcta. Sus respuestas se guardan solas; pulse «Entregar» al terminar. Tiene 45 minutos.',
       'Saber11', ARRAY[11], '2026-11-17 13:00:00-05', '2026-11-17 18:30:00-05', 45, true, now()
WHERE NOT EXISTS (SELECT 1 FROM colegio.simulacros WHERE id = '7c02c541-642a-5067-a3d9-c69c3ac90c3f');

WITH elegidas AS (
    SELECT b.*, row_number() OVER (PARTITION BY b.area ORDER BY random()) AS n,
           array_position(ARRAY['LecturaCritica', 'Matematicas', 'SocialesCiudadanas', 'CienciasNaturales', 'Ingles'], b.area::text) AS pos_area
    FROM colegio.preguntas_banco b
    WHERE b.area IN ('LecturaCritica', 'Matematicas', 'SocialesCiudadanas', 'CienciasNaturales', 'Ingles')
      AND (cardinality(b.grados) = 0 OR 11 = ANY (b.grados))
)
INSERT INTO colegio.preguntas_simulacro
    (id, simulacro_id, orden, area, competencia, contexto, enunciado, imagen_url, opciones, correcta, explicacion, pregunta_banco_id, creado_en)
SELECT gen_random_uuid(), '7c02c541-642a-5067-a3d9-c69c3ac90c3f', row_number() OVER (ORDER BY e.pos_area, e.n), e.area, e.competencia, e.contexto, e.enunciado,
       e.imagen_url, e.opciones, e.correcta, e.explicacion, e.id, now()
FROM elegidas e
WHERE e.n <= 4
  AND NOT EXISTS (SELECT 1 FROM colegio.preguntas_simulacro p WHERE p.simulacro_id = '7c02c541-642a-5067-a3d9-c69c3ac90c3f');
