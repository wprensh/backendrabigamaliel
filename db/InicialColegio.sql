DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'colegio') THEN
        CREATE SCHEMA colegio;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS colegio.__ef_migrations_history (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20260928163505_InicialColegio') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'colegio') THEN
            CREATE SCHEMA colegio;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20260928163505_InicialColegio') THEN
    CREATE TABLE colegio.noticias (
        id uuid NOT NULL,
        titulo character varying(150) NOT NULL,
        resumen character varying(300) NOT NULL,
        contenido character varying(8000) NOT NULL,
        categoria character varying(30) NOT NULL,
        imagen_url character varying(500),
        fecha_publicacion date NOT NULL,
        publicada boolean NOT NULL,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_noticias PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20260928163505_InicialColegio') THEN
    CREATE TABLE colegio.solicitudes_pqrs (
        id uuid NOT NULL,
        radicado character varying(20) NOT NULL,
        tipo character varying(20) NOT NULL,
        nombre_completo character varying(120) NOT NULL,
        correo character varying(180) NOT NULL,
        mensaje character varying(2000) NOT NULL,
        estado character varying(20) NOT NULL,
        respuesta character varying(2000),
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_solicitudes_pqrs PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20260928163505_InicialColegio') THEN
    CREATE INDEX ix_noticias_publicada_fecha ON colegio.noticias (publicada, fecha_publicacion DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20260928163505_InicialColegio') THEN
    CREATE UNIQUE INDEX ux_noticias_titulo ON colegio.noticias (titulo);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20260928163505_InicialColegio') THEN
    CREATE INDEX ix_pqrs_estado_creado ON colegio.solicitudes_pqrs (estado, creado_en);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20260928163505_InicialColegio') THEN
    CREATE UNIQUE INDEX ux_pqrs_radicado ON colegio.solicitudes_pqrs (radicado);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20260928163505_InicialColegio') THEN
    INSERT INTO colegio.__ef_migrations_history (migration_id, product_version)
    VALUES ('20260928163505_InicialColegio', '9.0.9');
    END IF;
END $EF$;
COMMIT;

