START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108092017_Horarios') THEN
    CREATE TABLE colegio.bloques_horario (
        id uuid NOT NULL,
        asignacion_id uuid NOT NULL,
        dia integer NOT NULL,
        inicio time without time zone NOT NULL,
        fin time without time zone NOT NULL,
        creado_en timestamp with time zone NOT NULL,
        modificado_en timestamp with time zone,
        CONSTRAINT pk_bloques_horario PRIMARY KEY (id),
        CONSTRAINT ck_bloques_horario_franja CHECK (dia BETWEEN 1 AND 6 AND inicio < fin),
        CONSTRAINT fk_bloques_horario_asignaciones_docentes_asignacion_id FOREIGN KEY (asignacion_id) REFERENCES colegio.asignaciones_docentes (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108092017_Horarios') THEN
    CREATE INDEX ix_bloques_horario_asignacion ON colegio.bloques_horario (asignacion_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM colegio.__ef_migrations_history WHERE "migration_id" = '20261108092017_Horarios') THEN
    INSERT INTO colegio.__ef_migrations_history (migration_id, product_version)
    VALUES ('20261108092017_Horarios', '9.0.9');
    END IF;
END $EF$;
COMMIT;

