START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261019181417_Usuarios') THEN
    CREATE TABLE colegio.usuarios (
        id uuid NOT NULL,
        nombre_usuario character varying(50) NOT NULL,
        nombre_completo character varying(120) NOT NULL,
        correo character varying(150),
        hash_clave character varying(200) NOT NULL,
        es_super_administrador boolean NOT NULL,
        activo boolean NOT NULL,
        sello_seguridad uuid NOT NULL,
        intentos_fallidos integer NOT NULL,
        bloqueado_hasta timestamp with time zone,
        ultimo_acceso timestamp with time zone,
        permisos text[] NOT NULL,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_usuarios PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261019181417_Usuarios') THEN
    CREATE UNIQUE INDEX ux_usuarios_nombre_usuario ON colegio.usuarios (nombre_usuario);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261019181417_Usuarios') THEN
    INSERT INTO colegio.__ef_migrations_history (migration_id, product_version)
    VALUES ('20261019181417_Usuarios', '9.0.9');
    END IF;
END $EF$;
COMMIT;

