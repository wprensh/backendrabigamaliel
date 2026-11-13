START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261020130913_EleccionesCampus') THEN
    CREATE TABLE colegio.elecciones (
        id uuid NOT NULL,
        nombre character varying(120) NOT NULL,
        descripcion character varying(1000),
        inicio timestamp with time zone NOT NULL,
        fin timestamp with time zone NOT NULL,
        cargos text[] NOT NULL,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_elecciones PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261020130913_EleccionesCampus') THEN
    CREATE TABLE colegio.miembros_campus (
        id uuid NOT NULL,
        rol character varying(20) NOT NULL,
        documento character varying(20) NOT NULL,
        nombre_completo character varying(120) NOT NULL,
        grado integer,
        grupo character varying(5),
        activo boolean NOT NULL,
        hash_clave character varying(200),
        sello_seguridad uuid NOT NULL,
        intentos_fallidos integer NOT NULL,
        bloqueado_hasta timestamp with time zone,
        ultimo_acceso timestamp with time zone,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_miembros_campus PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261020130913_EleccionesCampus') THEN
    CREATE TABLE colegio.candidatos (
        id uuid NOT NULL,
        eleccion_id uuid NOT NULL,
        cargo character varying(30) NOT NULL,
        estudiante_id uuid NOT NULL,
        nombre_completo character varying(120) NOT NULL,
        grado integer NOT NULL,
        grupo character varying(5),
        ambito character varying(20) NOT NULL,
        numero integer NOT NULL,
        lema character varying(150),
        propuestas character varying(4000),
        foto_url character varying(500),
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_candidatos PRIMARY KEY (id),
        CONSTRAINT fk_candidatos_elecciones_eleccion_id FOREIGN KEY (eleccion_id) REFERENCES colegio.elecciones (id) ON DELETE CASCADE,
        CONSTRAINT fk_candidatos_miembros_campus_estudiante_id FOREIGN KEY (estudiante_id) REFERENCES colegio.miembros_campus (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261020130913_EleccionesCampus') THEN
    CREATE TABLE colegio.participaciones_voto (
        id uuid NOT NULL,
        eleccion_id uuid NOT NULL,
        estudiante_id uuid NOT NULL,
        grado integer NOT NULL,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_participaciones_voto PRIMARY KEY (id),
        CONSTRAINT fk_participaciones_voto_elecciones_eleccion_id FOREIGN KEY (eleccion_id) REFERENCES colegio.elecciones (id) ON DELETE RESTRICT,
        CONSTRAINT fk_participaciones_voto_miembros_campus_estudiante_id FOREIGN KEY (estudiante_id) REFERENCES colegio.miembros_campus (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261020130913_EleccionesCampus') THEN
    CREATE TABLE colegio.votos (
        id uuid NOT NULL,
        eleccion_id uuid NOT NULL,
        cargo character varying(30) NOT NULL,
        ambito character varying(20) NOT NULL,
        candidato_id uuid,
        CONSTRAINT pk_votos PRIMARY KEY (id),
        CONSTRAINT fk_votos_candidatos_candidato_id FOREIGN KEY (candidato_id) REFERENCES colegio.candidatos (id) ON DELETE RESTRICT,
        CONSTRAINT fk_votos_elecciones_eleccion_id FOREIGN KEY (eleccion_id) REFERENCES colegio.elecciones (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261020130913_EleccionesCampus') THEN
    CREATE INDEX ix_candidatos_estudiante_id ON colegio.candidatos (estudiante_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261020130913_EleccionesCampus') THEN
    CREATE UNIQUE INDEX ux_candidatos_eleccion_cargo_estudiante ON colegio.candidatos (eleccion_id, cargo, estudiante_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261020130913_EleccionesCampus') THEN
    CREATE UNIQUE INDEX ux_candidatos_eleccion_contienda_numero ON colegio.candidatos (eleccion_id, cargo, ambito, numero);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261020130913_EleccionesCampus') THEN
    CREATE INDEX ix_elecciones_inicio ON colegio.elecciones (inicio);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261020130913_EleccionesCampus') THEN
    CREATE INDEX ix_miembros_campus_rol_grado_grupo ON colegio.miembros_campus (rol, grado, grupo);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261020130913_EleccionesCampus') THEN
    CREATE UNIQUE INDEX ux_miembros_campus_documento ON colegio.miembros_campus (documento);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261020130913_EleccionesCampus') THEN
    CREATE INDEX ix_participaciones_voto_estudiante_id ON colegio.participaciones_voto (estudiante_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261020130913_EleccionesCampus') THEN
    CREATE UNIQUE INDEX ux_participaciones_voto_eleccion_estudiante ON colegio.participaciones_voto (eleccion_id, estudiante_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261020130913_EleccionesCampus') THEN
    CREATE INDEX ix_votos_candidato_id ON colegio.votos (candidato_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261020130913_EleccionesCampus') THEN
    CREATE INDEX ix_votos_eleccion_contienda ON colegio.votos (eleccion_id, cargo, ambito);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261020130913_EleccionesCampus') THEN
    INSERT INTO colegio.__ef_migrations_history (migration_id, product_version)
    VALUES ('20261020130913_EleccionesCampus', '9.0.9');
    END IF;
END $EF$;
COMMIT;

