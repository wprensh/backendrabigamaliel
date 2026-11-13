using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RabiGamaliel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SieteMejoras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "actas_comision",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    periodo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    grado = table.Column<int>(type: "integer", nullable: false),
                    grupo = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    integrantes = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    observaciones = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_actas_comision", x => x.id);
                    table.ForeignKey(
                        name: "fk_actas_comision_periodos_academicos_periodo_id",
                        column: x => x.periodo_id,
                        principalSchema: "colegio",
                        principalTable: "periodos_academicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "archivos_aula",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    subido_por_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre_archivo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo_contenido = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tamano = table.Column<int>(type: "integer", nullable: false),
                    contenido = table.Column<byte[]>(type: "bytea", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_archivos_aula", x => x.id);
                    table.ForeignKey(
                        name: "fk_archivos_aula_miembros_campus_subido_por_id",
                        column: x => x.subido_por_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conversaciones",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    docente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    familia_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estudiante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asunto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ultimo_mensaje_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    leida_docente_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    leida_familia_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cerrada = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversaciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_conversaciones_miembros_campus_docente_id",
                        column: x => x.docente_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_conversaciones_miembros_campus_estudiante_id",
                        column: x => x.estudiante_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_conversaciones_miembros_campus_familia_id",
                        column: x => x.familia_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "planeaciones_clase",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asignacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    docente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    semana = table.Column<DateOnly>(type: "date", nullable: false),
                    tema = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    aprendizajes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    actividades = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    recursos = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    evaluacion = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    enviada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    revisada_por = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    revisada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_planeaciones_clase", x => x.id);
                    table.ForeignKey(
                        name: "fk_planeaciones_clase_asignaciones_docentes_asignacion_id",
                        column: x => x.asignacion_id,
                        principalSchema: "colegio",
                        principalTable: "asignaciones_docentes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_planeaciones_clase_miembros_campus_docente_id",
                        column: x => x.docente_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_planeaciones_clase_periodos_academicos_periodo_id",
                        column: x => x.periodo_id,
                        principalSchema: "colegio",
                        principalTable: "periodos_academicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "planes_estudio",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asignatura_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grado = table.Column<int>(type: "integer", nullable: false),
                    periodo = table.Column<int>(type: "integer", nullable: false),
                    estandares = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: true),
                    dba = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: true),
                    competencias = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: true),
                    contenidos = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: true),
                    indicadores = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: true),
                    actualizado_por = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_planes_estudio", x => x.id);
                    table.ForeignKey(
                        name: "fk_planes_estudio_asignaturas_asignatura_id",
                        column: x => x.asignatura_id,
                        principalSchema: "colegio",
                        principalTable: "asignaturas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "recuperaciones",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    estudiante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asignatura_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    docente_id = table.Column<Guid>(type: "uuid", nullable: true),
                    nota_original = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: false),
                    plan = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    fecha_limite = table.Column<DateOnly>(type: "date", nullable: true),
                    nota_recuperacion = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: true),
                    nota_definitiva = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: true),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    registrada_por = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recuperaciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_recuperaciones_asignaturas_asignatura_id",
                        column: x => x.asignatura_id,
                        principalSchema: "colegio",
                        principalTable: "asignaturas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_recuperaciones_miembros_campus_estudiante_id",
                        column: x => x.estudiante_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_recuperaciones_periodos_academicos_periodo_id",
                        column: x => x.periodo_id,
                        principalSchema: "colegio",
                        principalTable: "periodos_academicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "suscripciones_push",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    miembro_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endpoint = table.Column<string>(type: "character varying(800)", maxLength: 800, nullable: false),
                    p256dh = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    auth = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suscripciones_push", x => x.id);
                    table.ForeignKey(
                        name: "fk_suscripciones_push_miembros_campus_miembro_id",
                        column: x => x.miembro_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "decisiones_comision",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    acta_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estudiante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre_estudiante = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    situacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    decision = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    observacion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_decisiones_comision", x => x.id);
                    table.ForeignKey(
                        name: "fk_decisiones_comision_actas_comision_acta_id",
                        column: x => x.acta_id,
                        principalSchema: "colegio",
                        principalTable: "actas_comision",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_decisiones_comision_miembros_campus_estudiante_id",
                        column: x => x.estudiante_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "actividades_aula",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asignacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    docente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    enlace = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    archivo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fecha_entrega = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    calificable = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_actividades_aula", x => x.id);
                    table.ForeignKey(
                        name: "fk_actividades_aula_archivos_aula_archivo_id",
                        column: x => x.archivo_id,
                        principalSchema: "colegio",
                        principalTable: "archivos_aula",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_actividades_aula_asignaciones_docentes_asignacion_id",
                        column: x => x.asignacion_id,
                        principalSchema: "colegio",
                        principalTable: "asignaciones_docentes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_actividades_aula_miembros_campus_docente_id",
                        column: x => x.docente_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_actividades_aula_periodos_academicos_periodo_id",
                        column: x => x.periodo_id,
                        principalSchema: "colegio",
                        principalTable: "periodos_academicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mensajes",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    autor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    texto = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mensajes", x => x.id);
                    table.ForeignKey(
                        name: "fk_mensajes_conversaciones_conversacion_id",
                        column: x => x.conversacion_id,
                        principalSchema: "colegio",
                        principalTable: "conversaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mensajes_miembros_campus_autor_id",
                        column: x => x.autor_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "entregas_aula",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actividad_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estudiante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    texto = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    enlace = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    archivo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entregada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    nota = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: true),
                    comentario = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    calificada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entregas_aula", x => x.id);
                    table.ForeignKey(
                        name: "fk_entregas_aula_actividades_aula_actividad_id",
                        column: x => x.actividad_id,
                        principalSchema: "colegio",
                        principalTable: "actividades_aula",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_entregas_aula_archivos_aula_archivo_id",
                        column: x => x.archivo_id,
                        principalSchema: "colegio",
                        principalTable: "archivos_aula",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_entregas_aula_miembros_campus_estudiante_id",
                        column: x => x.estudiante_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_actas_comision_periodo_id",
                schema: "colegio",
                table: "actas_comision",
                column: "periodo_id");

            migrationBuilder.CreateIndex(
                name: "ux_actas_comision_anio_numero",
                schema: "colegio",
                table: "actas_comision",
                columns: new[] { "anio", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_actividades_aula_archivo_id",
                schema: "colegio",
                table: "actividades_aula",
                column: "archivo_id");

            migrationBuilder.CreateIndex(
                name: "ix_actividades_aula_asignacion",
                schema: "colegio",
                table: "actividades_aula",
                column: "asignacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_actividades_aula_docente_id",
                schema: "colegio",
                table: "actividades_aula",
                column: "docente_id");

            migrationBuilder.CreateIndex(
                name: "ix_actividades_aula_periodo_id",
                schema: "colegio",
                table: "actividades_aula",
                column: "periodo_id");

            migrationBuilder.CreateIndex(
                name: "ix_archivos_aula_subido_por_id",
                schema: "colegio",
                table: "archivos_aula",
                column: "subido_por_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversaciones_docente",
                schema: "colegio",
                table: "conversaciones",
                column: "docente_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversaciones_estudiante_id",
                schema: "colegio",
                table: "conversaciones",
                column: "estudiante_id");

            migrationBuilder.CreateIndex(
                name: "ix_conversaciones_familia",
                schema: "colegio",
                table: "conversaciones",
                column: "familia_id");

            migrationBuilder.CreateIndex(
                name: "ix_decisiones_comision_acta",
                schema: "colegio",
                table: "decisiones_comision",
                column: "acta_id");

            migrationBuilder.CreateIndex(
                name: "ix_decisiones_comision_estudiante_id",
                schema: "colegio",
                table: "decisiones_comision",
                column: "estudiante_id");

            migrationBuilder.CreateIndex(
                name: "ix_entregas_aula_archivo_id",
                schema: "colegio",
                table: "entregas_aula",
                column: "archivo_id");

            migrationBuilder.CreateIndex(
                name: "ix_entregas_aula_estudiante",
                schema: "colegio",
                table: "entregas_aula",
                column: "estudiante_id");

            migrationBuilder.CreateIndex(
                name: "ux_entregas_aula_actividad_estudiante",
                schema: "colegio",
                table: "entregas_aula",
                columns: new[] { "actividad_id", "estudiante_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mensajes_autor_id",
                schema: "colegio",
                table: "mensajes",
                column: "autor_id");

            migrationBuilder.CreateIndex(
                name: "ix_mensajes_conversacion_fecha",
                schema: "colegio",
                table: "mensajes",
                columns: new[] { "conversacion_id", "creado_en" });

            migrationBuilder.CreateIndex(
                name: "ix_planeaciones_clase_asignacion_id",
                schema: "colegio",
                table: "planeaciones_clase",
                column: "asignacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_planeaciones_clase_docente_semana",
                schema: "colegio",
                table: "planeaciones_clase",
                columns: new[] { "docente_id", "semana" });

            migrationBuilder.CreateIndex(
                name: "ix_planeaciones_clase_estado",
                schema: "colegio",
                table: "planeaciones_clase",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "ix_planeaciones_clase_periodo_id",
                schema: "colegio",
                table: "planeaciones_clase",
                column: "periodo_id");

            migrationBuilder.CreateIndex(
                name: "ux_planes_estudio_asignatura_grado_periodo",
                schema: "colegio",
                table: "planes_estudio",
                columns: new[] { "asignatura_id", "grado", "periodo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recuperaciones_asignatura_id",
                schema: "colegio",
                table: "recuperaciones",
                column: "asignatura_id");

            migrationBuilder.CreateIndex(
                name: "ix_recuperaciones_periodo",
                schema: "colegio",
                table: "recuperaciones",
                column: "periodo_id");

            migrationBuilder.CreateIndex(
                name: "ux_recuperaciones_estudiante_asignatura_periodo",
                schema: "colegio",
                table: "recuperaciones",
                columns: new[] { "estudiante_id", "asignatura_id", "periodo_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_suscripciones_push_miembro",
                schema: "colegio",
                table: "suscripciones_push",
                column: "miembro_id");

            migrationBuilder.CreateIndex(
                name: "ux_suscripciones_push_endpoint",
                schema: "colegio",
                table: "suscripciones_push",
                column: "endpoint",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "decisiones_comision",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "entregas_aula",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "mensajes",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "planeaciones_clase",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "planes_estudio",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "recuperaciones",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "suscripciones_push",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "actas_comision",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "actividades_aula",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "conversaciones",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "archivos_aula",
                schema: "colegio");
        }
    }
}
