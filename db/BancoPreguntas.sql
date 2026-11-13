START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108111956_BancoPreguntas') THEN
    ALTER TABLE colegio.preguntas_simulacro ADD pregunta_banco_id uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108111956_BancoPreguntas') THEN
    CREATE TABLE colegio.preguntas_banco (
        id uuid NOT NULL,
        area character varying(30) NOT NULL,
        competencia character varying(120),
        contexto character varying(6000),
        enunciado character varying(3000) NOT NULL,
        imagen_url character varying(500),
        correcta integer NOT NULL,
        explicacion character varying(2000),
        dificultad character varying(10) NOT NULL,
        grados integer[] NOT NULL,
        opciones text[] NOT NULL,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_preguntas_banco PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108111956_BancoPreguntas') THEN
    CREATE INDEX ix_preguntas_simulacro_pregunta_banco ON colegio.preguntas_simulacro (pregunta_banco_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108111956_BancoPreguntas') THEN
    CREATE INDEX ix_preguntas_banco_area ON colegio.preguntas_banco (area);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108111956_BancoPreguntas') THEN
    INSERT INTO colegio.__ef_migrations_history (migration_id, product_version)
    VALUES ('20261108111956_BancoPreguntas', '9.0.9');
    END IF;
END $EF$;
COMMIT;

