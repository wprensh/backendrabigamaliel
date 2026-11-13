START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    ALTER TABLE colegio.miembros_campus ADD codigo_carnet character varying(10) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    ALTER TABLE colegio.miembros_campus ADD correo character varying(150);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    ALTER TABLE colegio.miembros_campus ADD telefono character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    UPDATE colegio.miembros_campus SET codigo_carnet = upper(substr(md5(random()::text || id::text), 1, 10)) WHERE codigo_carnet = '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.anotaciones (
        id uuid NOT NULL,
        estudiante_id uuid NOT NULL,
        grado integer,
        grupo character varying(5),
        tipo character varying(30) NOT NULL,
        fecha date NOT NULL,
        descripcion character varying(3000) NOT NULL,
        compromisos character varying(1500),
        autor_nombre character varying(120) NOT NULL,
        autor_miembro_id uuid,
        estado character varying(20) NOT NULL,
        enterado_en timestamp with time zone,
        enterado_por character varying(120),
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_anotaciones PRIMARY KEY (id),
        CONSTRAINT fk_anotaciones_miembros_campus_estudiante_id FOREIGN KEY (estudiante_id) REFERENCES colegio.miembros_campus (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.asignaturas (
        id uuid NOT NULL,
        nombre character varying(80) NOT NULL,
        area character varying(80) NOT NULL,
        orden integer NOT NULL,
        activa boolean NOT NULL,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_asignaturas PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.avisos (
        id uuid NOT NULL,
        destinatario_id uuid NOT NULL,
        tipo character varying(20) NOT NULL,
        titulo character varying(150) NOT NULL,
        mensaje character varying(1000) NOT NULL,
        enlace character varying(200),
        estudiante_id uuid,
        leido_en timestamp with time zone,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_avisos PRIMARY KEY (id),
        CONSTRAINT fk_avisos_miembros_campus_destinatario_id FOREIGN KEY (destinatario_id) REFERENCES colegio.miembros_campus (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.circulares (
        id uuid NOT NULL,
        anio integer NOT NULL,
        numero integer NOT NULL,
        titulo character varying(150) NOT NULL,
        contenido character varying(10000) NOT NULL,
        publico character varying(20) NOT NULL,
        requiere_confirmacion boolean NOT NULL,
        enlace_adjunto character varying(500),
        publicada_en timestamp with time zone,
        grados integer[] NOT NULL,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_circulares PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.eventos_calendario (
        id uuid NOT NULL,
        titulo character varying(150) NOT NULL,
        descripcion character varying(1000),
        inicio date NOT NULL,
        fin date NOT NULL,
        hora character varying(30),
        lugar character varying(120),
        tipo character varying(20) NOT NULL,
        publico boolean NOT NULL,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_eventos_calendario PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.periodos_academicos (
        id uuid NOT NULL,
        anio integer NOT NULL,
        numero integer NOT NULL,
        nombre character varying(60) NOT NULL,
        inicio date NOT NULL,
        fin date NOT NULL,
        porcentaje integer NOT NULL,
        notas_abiertas boolean NOT NULL,
        publicado boolean NOT NULL,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_periodos_academicos PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.simulacros (
        id uuid NOT NULL,
        titulo character varying(150) NOT NULL,
        instrucciones character varying(3000),
        tipo character varying(20) NOT NULL,
        inicio timestamp with time zone NOT NULL,
        fin timestamp with time zone NOT NULL,
        duracion_minutos integer NOT NULL,
        mostrar_respuestas boolean NOT NULL,
        grados integer[] NOT NULL,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_simulacros PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.solicitudes_admision (
        id uuid NOT NULL,
        radicado character varying(10) NOT NULL,
        anio_lectivo integer NOT NULL,
        grado_solicitado integer NOT NULL,
        aspirante_tipo_documento character varying(30) NOT NULL,
        aspirante_documento character varying(20) NOT NULL,
        aspirante_nombres character varying(80) NOT NULL,
        aspirante_apellidos character varying(80) NOT NULL,
        aspirante_fecha_nacimiento date NOT NULL,
        colegio_procedencia character varying(150),
        direccion character varying(150),
        eps character varying(150),
        acudiente_nombre character varying(120) NOT NULL,
        acudiente_documento character varying(20) NOT NULL,
        acudiente_parentesco character varying(30),
        acudiente_telefono character varying(20) NOT NULL,
        acudiente_correo character varying(150),
        observaciones character varying(1500),
        estado character varying(30) NOT NULL,
        mensaje_familia character varying(1500),
        nota_interna character varying(1500),
        estado_cambiado_en timestamp with time zone,
        estudiante_id uuid,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_solicitudes_admision PRIMARY KEY (id),
        CONSTRAINT fk_solicitudes_admision_miembros_campus_estudiante_id FOREIGN KEY (estudiante_id) REFERENCES colegio.miembros_campus (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.vinculos_acudiente (
        id uuid NOT NULL,
        acudiente_id uuid NOT NULL,
        estudiante_id uuid NOT NULL,
        parentesco character varying(30),
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_vinculos_acudiente PRIMARY KEY (id),
        CONSTRAINT fk_vinculos_acudiente_miembros_campus_acudiente_id FOREIGN KEY (acudiente_id) REFERENCES colegio.miembros_campus (id) ON DELETE CASCADE,
        CONSTRAINT fk_vinculos_acudiente_miembros_campus_estudiante_id FOREIGN KEY (estudiante_id) REFERENCES colegio.miembros_campus (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.seguimientos_anotacion (
        id uuid NOT NULL,
        anotacion_id uuid NOT NULL,
        texto character varying(2000) NOT NULL,
        autor_nombre character varying(120) NOT NULL,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_seguimientos_anotacion PRIMARY KEY (id),
        CONSTRAINT fk_seguimientos_anotacion_anotaciones_anotacion_id FOREIGN KEY (anotacion_id) REFERENCES colegio.anotaciones (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.asignaciones_docentes (
        id uuid NOT NULL,
        anio integer NOT NULL,
        docente_id uuid NOT NULL,
        asignatura_id uuid NOT NULL,
        grado integer NOT NULL,
        grupo character varying(5),
        intensidad_horaria integer,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_asignaciones_docentes PRIMARY KEY (id),
        CONSTRAINT fk_asignaciones_docentes_asignaturas_asignatura_id FOREIGN KEY (asignatura_id) REFERENCES colegio.asignaturas (id) ON DELETE RESTRICT,
        CONSTRAINT fk_asignaciones_docentes_miembros_campus_docente_id FOREIGN KEY (docente_id) REFERENCES colegio.miembros_campus (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.lecturas_circular (
        id uuid NOT NULL,
        circular_id uuid NOT NULL,
        miembro_id uuid NOT NULL,
        leida_en timestamp with time zone NOT NULL,
        confirmada_en timestamp with time zone,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_lecturas_circular PRIMARY KEY (id),
        CONSTRAINT fk_lecturas_circular_circulares_circular_id FOREIGN KEY (circular_id) REFERENCES colegio.circulares (id) ON DELETE CASCADE,
        CONSTRAINT fk_lecturas_circular_miembros_campus_miembro_id FOREIGN KEY (miembro_id) REFERENCES colegio.miembros_campus (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.calificaciones (
        id uuid NOT NULL,
        estudiante_id uuid NOT NULL,
        asignatura_id uuid NOT NULL,
        periodo_id uuid NOT NULL,
        nota numeric(3,1),
        fallas integer,
        observacion character varying(400),
        registrada_por character varying(120) NOT NULL,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_calificaciones PRIMARY KEY (id),
        CONSTRAINT fk_calificaciones_asignaturas_asignatura_id FOREIGN KEY (asignatura_id) REFERENCES colegio.asignaturas (id) ON DELETE RESTRICT,
        CONSTRAINT fk_calificaciones_miembros_campus_estudiante_id FOREIGN KEY (estudiante_id) REFERENCES colegio.miembros_campus (id) ON DELETE RESTRICT,
        CONSTRAINT fk_calificaciones_periodos_academicos_periodo_id FOREIGN KEY (periodo_id) REFERENCES colegio.periodos_academicos (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.intentos_simulacro (
        id uuid NOT NULL,
        simulacro_id uuid NOT NULL,
        estudiante_id uuid NOT NULL,
        grado integer NOT NULL,
        grupo character varying(5),
        iniciado_en timestamp with time zone NOT NULL,
        limite_en timestamp with time zone NOT NULL,
        entregado_en timestamp with time zone,
        aciertos integer NOT NULL,
        total_preguntas integer NOT NULL,
        respuestas jsonb NOT NULL,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_intentos_simulacro PRIMARY KEY (id),
        CONSTRAINT fk_intentos_simulacro_miembros_campus_estudiante_id FOREIGN KEY (estudiante_id) REFERENCES colegio.miembros_campus (id) ON DELETE RESTRICT,
        CONSTRAINT fk_intentos_simulacro_simulacros_simulacro_id FOREIGN KEY (simulacro_id) REFERENCES colegio.simulacros (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.preguntas_simulacro (
        id uuid NOT NULL,
        simulacro_id uuid NOT NULL,
        orden integer NOT NULL,
        area character varying(30) NOT NULL,
        competencia character varying(120),
        contexto character varying(6000),
        enunciado character varying(3000) NOT NULL,
        imagen_url character varying(500),
        correcta integer NOT NULL,
        explicacion character varying(2000),
        opciones text[] NOT NULL,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_preguntas_simulacro PRIMARY KEY (id),
        CONSTRAINT fk_preguntas_simulacro_simulacros_simulacro_id FOREIGN KEY (simulacro_id) REFERENCES colegio.simulacros (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.documentos_admision (
        id uuid NOT NULL,
        solicitud_id uuid NOT NULL,
        tipo character varying(30) NOT NULL,
        nombre_archivo character varying(150) NOT NULL,
        tipo_contenido character varying(50) NOT NULL,
        tamano integer NOT NULL,
        contenido bytea NOT NULL,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_documentos_admision PRIMARY KEY (id),
        CONSTRAINT fk_documentos_admision_solicitudes_admision_solicitud_id FOREIGN KEY (solicitud_id) REFERENCES colegio.solicitudes_admision (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.tomas_asistencia (
        id uuid NOT NULL,
        asignacion_id uuid NOT NULL,
        asignatura_id uuid NOT NULL,
        docente_id uuid NOT NULL,
        fecha date NOT NULL,
        grado integer NOT NULL,
        grupo character varying(5),
        total_estudiantes integer NOT NULL,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_tomas_asistencia PRIMARY KEY (id),
        CONSTRAINT fk_tomas_asistencia_asignaciones_docentes_asignacion_id FOREIGN KEY (asignacion_id) REFERENCES colegio.asignaciones_docentes (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE TABLE colegio.novedades_asistencia (
        id uuid NOT NULL,
        toma_id uuid NOT NULL,
        estudiante_id uuid NOT NULL,
        estado character varying(20) NOT NULL,
        observacion character varying(200),
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_novedades_asistencia PRIMARY KEY (id),
        CONSTRAINT fk_novedades_asistencia_miembros_campus_estudiante_id FOREIGN KEY (estudiante_id) REFERENCES colegio.miembros_campus (id) ON DELETE RESTRICT,
        CONSTRAINT fk_novedades_asistencia_tomas_asistencia_toma_id FOREIGN KEY (toma_id) REFERENCES colegio.tomas_asistencia (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE UNIQUE INDEX ux_miembros_campus_codigo_carnet ON colegio.miembros_campus (codigo_carnet);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_anotaciones_estado_tipo ON colegio.anotaciones (estado, tipo);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_anotaciones_estudiante ON colegio.anotaciones (estudiante_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_anotaciones_fecha ON colegio.anotaciones (fecha);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_asignaciones_docentes_asignatura_id ON colegio.asignaciones_docentes (asignatura_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_asignaciones_docentes_docente ON colegio.asignaciones_docentes (anio, docente_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_asignaciones_docentes_docente_id ON colegio.asignaciones_docentes (docente_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE UNIQUE INDEX ux_asignaciones_docentes_curso ON colegio.asignaciones_docentes (anio, asignatura_id, grado, grupo) NULLS NOT DISTINCT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE UNIQUE INDEX ux_asignaturas_nombre ON colegio.asignaturas (nombre);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_avisos_destinatario_creado ON colegio.avisos (destinatario_id, creado_en);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_calificaciones_asignatura_id ON colegio.calificaciones (asignatura_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_calificaciones_periodo ON colegio.calificaciones (periodo_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE UNIQUE INDEX ux_calificaciones_estudiante_asignatura_periodo ON colegio.calificaciones (estudiante_id, asignatura_id, periodo_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE UNIQUE INDEX ux_circulares_anio_numero ON colegio.circulares (anio, numero);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_documentos_admision_solicitud ON colegio.documentos_admision (solicitud_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_eventos_calendario_fechas ON colegio.eventos_calendario (inicio, fin);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_intentos_simulacro_estudiante ON colegio.intentos_simulacro (estudiante_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE UNIQUE INDEX ux_intentos_simulacro_simulacro_estudiante ON colegio.intentos_simulacro (simulacro_id, estudiante_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_lecturas_circular_miembro ON colegio.lecturas_circular (miembro_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE UNIQUE INDEX ux_lecturas_circular_circular_miembro ON colegio.lecturas_circular (circular_id, miembro_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_novedades_asistencia_estudiante ON colegio.novedades_asistencia (estudiante_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE UNIQUE INDEX ux_novedades_asistencia_toma_estudiante ON colegio.novedades_asistencia (toma_id, estudiante_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE UNIQUE INDEX ux_periodos_academicos_anio_numero ON colegio.periodos_academicos (anio, numero);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_preguntas_simulacro_orden ON colegio.preguntas_simulacro (simulacro_id, orden);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_seguimientos_anotacion_anotacion_id ON colegio.seguimientos_anotacion (anotacion_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_simulacros_inicio ON colegio.simulacros (inicio);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_solicitudes_admision_anio_estado ON colegio.solicitudes_admision (anio_lectivo, estado);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_solicitudes_admision_aspirante ON colegio.solicitudes_admision (aspirante_documento, anio_lectivo);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_solicitudes_admision_estudiante_id ON colegio.solicitudes_admision (estudiante_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE UNIQUE INDEX ux_solicitudes_admision_radicado ON colegio.solicitudes_admision (radicado);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_tomas_asistencia_fecha_grado ON colegio.tomas_asistencia (fecha, grado);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE UNIQUE INDEX ux_tomas_asistencia_asignacion_fecha ON colegio.tomas_asistencia (asignacion_id, fecha);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE INDEX ix_vinculos_acudiente_estudiante ON colegio.vinculos_acudiente (estudiante_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    CREATE UNIQUE INDEX ux_vinculos_acudiente_acudiente_estudiante ON colegio.vinculos_acudiente (acudiente_id, estudiante_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108035721_PlataformaAcademica') THEN
    INSERT INTO colegio.__ef_migrations_history (migration_id, product_version)
    VALUES ('20261108035721_PlataformaAcademica', '9.0.9');
    END IF;
END $EF$;
COMMIT;

