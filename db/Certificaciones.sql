START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261019151312_Certificaciones') THEN
    CREATE TABLE colegio.certificados (
        id uuid NOT NULL,
        codigo character varying(20) NOT NULL,
        tipo character varying(30) NOT NULL,
        titulo character varying(150) NOT NULL,
        nombre_beneficiario character varying(150) NOT NULL,
        tipo_documento character varying(30) NOT NULL,
        numero_documento character varying(20) NOT NULL,
        grado character varying(50),
        anio_lectivo integer,
        contenido character varying(5000) NOT NULL,
        fecha_expedicion date NOT NULL,
        firmante_nombre character varying(120) NOT NULL,
        firmante_cargo character varying(80) NOT NULL,
        anulado boolean NOT NULL,
        motivo_anulacion character varying(300),
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_certificados PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261019151312_Certificaciones') THEN
    CREATE INDEX ix_certificados_creado ON colegio.certificados (creado_en DESC);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261019151312_Certificaciones') THEN
    CREATE INDEX ix_certificados_numero_documento ON colegio.certificados (numero_documento);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261019151312_Certificaciones') THEN
    CREATE UNIQUE INDEX ux_certificados_codigo ON colegio.certificados (codigo);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261019151312_Certificaciones') THEN
    INSERT INTO colegio.__ef_migrations_history (migration_id, product_version)
    VALUES ('20261019151312_Certificaciones', '9.0.9');
    END IF;
END $EF$;
COMMIT;

