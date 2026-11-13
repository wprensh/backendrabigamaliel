using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RabiGamaliel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlataformaAcademica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "codigo_carnet",
                schema: "colegio",
                table: "miembros_campus",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "correo",
                schema: "colegio",
                table: "miembros_campus",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "telefono",
                schema: "colegio",
                table: "miembros_campus",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Los miembros existentes reciben un código de carné único antes de crear el índice.
            migrationBuilder.Sql(
                "UPDATE colegio.miembros_campus SET codigo_carnet = upper(substr(md5(random()::text || id::text), 1, 10)) WHERE codigo_carnet = '';");

            migrationBuilder.CreateTable(
                name: "anotaciones",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    estudiante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grado = table.Column<int>(type: "integer", nullable: true),
                    grupo = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    compromisos = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    autor_nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    autor_miembro_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    enterado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    enterado_por = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_anotaciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_anotaciones_miembros_campus_estudiante_id",
                        column: x => x.estudiante_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "asignaturas",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    area = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asignaturas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "avisos",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    destinatario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    mensaje = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    enlace = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    estudiante_id = table.Column<Guid>(type: "uuid", nullable: true),
                    leido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_avisos", x => x.id);
                    table.ForeignKey(
                        name: "fk_avisos_miembros_campus_destinatario_id",
                        column: x => x.destinatario_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "circulares",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    contenido = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                    publico = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    requiere_confirmacion = table.Column<bool>(type: "boolean", nullable: false),
                    enlace_adjunto = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    publicada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    grados = table.Column<List<int>>(type: "integer[]", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_circulares", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "eventos_calendario",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fin = table.Column<DateOnly>(type: "date", nullable: false),
                    hora = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    lugar = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    publico = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_eventos_calendario", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "periodos_academicos",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    fin = table.Column<DateOnly>(type: "date", nullable: false),
                    porcentaje = table.Column<int>(type: "integer", nullable: false),
                    notas_abiertas = table.Column<bool>(type: "boolean", nullable: false),
                    publicado = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_periodos_academicos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "simulacros",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    instrucciones = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fin = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    duracion_minutos = table.Column<int>(type: "integer", nullable: false),
                    mostrar_respuestas = table.Column<bool>(type: "boolean", nullable: false),
                    grados = table.Column<List<int>>(type: "integer[]", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_simulacros", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "solicitudes_admision",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    radicado = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    anio_lectivo = table.Column<int>(type: "integer", nullable: false),
                    grado_solicitado = table.Column<int>(type: "integer", nullable: false),
                    aspirante_tipo_documento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    aspirante_documento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    aspirante_nombres = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    aspirante_apellidos = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    aspirante_fecha_nacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    colegio_procedencia = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    direccion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    eps = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    acudiente_nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    acudiente_documento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    acudiente_parentesco = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    acudiente_telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    acudiente_correo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    observaciones = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    mensaje_familia = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    nota_interna = table.Column<string>(type: "character varying(1500)", maxLength: 1500, nullable: true),
                    estado_cambiado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    estudiante_id = table.Column<Guid>(type: "uuid", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_solicitudes_admision", x => x.id);
                    table.ForeignKey(
                        name: "fk_solicitudes_admision_miembros_campus_estudiante_id",
                        column: x => x.estudiante_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "vinculos_acudiente",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    acudiente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estudiante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parentesco = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vinculos_acudiente", x => x.id);
                    table.ForeignKey(
                        name: "fk_vinculos_acudiente_miembros_campus_acudiente_id",
                        column: x => x.acudiente_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_vinculos_acudiente_miembros_campus_estudiante_id",
                        column: x => x.estudiante_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seguimientos_anotacion",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    anotacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    texto = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    autor_nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seguimientos_anotacion", x => x.id);
                    table.ForeignKey(
                        name: "fk_seguimientos_anotacion_anotaciones_anotacion_id",
                        column: x => x.anotacion_id,
                        principalSchema: "colegio",
                        principalTable: "anotaciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asignaciones_docentes",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: false),
                    docente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asignatura_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grado = table.Column<int>(type: "integer", nullable: false),
                    grupo = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    intensidad_horaria = table.Column<int>(type: "integer", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asignaciones_docentes", x => x.id);
                    table.ForeignKey(
                        name: "fk_asignaciones_docentes_asignaturas_asignatura_id",
                        column: x => x.asignatura_id,
                        principalSchema: "colegio",
                        principalTable: "asignaturas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_asignaciones_docentes_miembros_campus_docente_id",
                        column: x => x.docente_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lecturas_circular",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    circular_id = table.Column<Guid>(type: "uuid", nullable: false),
                    miembro_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leida_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    confirmada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_lecturas_circular", x => x.id);
                    table.ForeignKey(
                        name: "fk_lecturas_circular_circulares_circular_id",
                        column: x => x.circular_id,
                        principalSchema: "colegio",
                        principalTable: "circulares",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_lecturas_circular_miembros_campus_miembro_id",
                        column: x => x.miembro_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "calificaciones",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    estudiante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asignatura_id = table.Column<Guid>(type: "uuid", nullable: false),
                    periodo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nota = table.Column<decimal>(type: "numeric(3,1)", precision: 3, scale: 1, nullable: true),
                    fallas = table.Column<int>(type: "integer", nullable: true),
                    observacion = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    registrada_por = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calificaciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_calificaciones_asignaturas_asignatura_id",
                        column: x => x.asignatura_id,
                        principalSchema: "colegio",
                        principalTable: "asignaturas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_calificaciones_miembros_campus_estudiante_id",
                        column: x => x.estudiante_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_calificaciones_periodos_academicos_periodo_id",
                        column: x => x.periodo_id,
                        principalSchema: "colegio",
                        principalTable: "periodos_academicos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "intentos_simulacro",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    simulacro_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estudiante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grado = table.Column<int>(type: "integer", nullable: false),
                    grupo = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    iniciado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    limite_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    entregado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    aciertos = table.Column<int>(type: "integer", nullable: false),
                    total_preguntas = table.Column<int>(type: "integer", nullable: false),
                    respuestas = table.Column<string>(type: "jsonb", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_intentos_simulacro", x => x.id);
                    table.ForeignKey(
                        name: "fk_intentos_simulacro_miembros_campus_estudiante_id",
                        column: x => x.estudiante_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_intentos_simulacro_simulacros_simulacro_id",
                        column: x => x.simulacro_id,
                        principalSchema: "colegio",
                        principalTable: "simulacros",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "preguntas_simulacro",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    simulacro_id = table.Column<Guid>(type: "uuid", nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    area = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    competencia = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    contexto = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: true),
                    enunciado = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    imagen_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    correcta = table.Column<int>(type: "integer", nullable: false),
                    explicacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    opciones = table.Column<List<string>>(type: "text[]", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_preguntas_simulacro", x => x.id);
                    table.ForeignKey(
                        name: "fk_preguntas_simulacro_simulacros_simulacro_id",
                        column: x => x.simulacro_id,
                        principalSchema: "colegio",
                        principalTable: "simulacros",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "documentos_admision",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    solicitud_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    nombre_archivo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo_contenido = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tamano = table.Column<int>(type: "integer", nullable: false),
                    contenido = table.Column<byte[]>(type: "bytea", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documentos_admision", x => x.id);
                    table.ForeignKey(
                        name: "fk_documentos_admision_solicitudes_admision_solicitud_id",
                        column: x => x.solicitud_id,
                        principalSchema: "colegio",
                        principalTable: "solicitudes_admision",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tomas_asistencia",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asignacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asignatura_id = table.Column<Guid>(type: "uuid", nullable: false),
                    docente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    grado = table.Column<int>(type: "integer", nullable: false),
                    grupo = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    total_estudiantes = table.Column<int>(type: "integer", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tomas_asistencia", x => x.id);
                    table.ForeignKey(
                        name: "fk_tomas_asistencia_asignaciones_docentes_asignacion_id",
                        column: x => x.asignacion_id,
                        principalSchema: "colegio",
                        principalTable: "asignaciones_docentes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "novedades_asistencia",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    toma_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estudiante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    observacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_novedades_asistencia", x => x.id);
                    table.ForeignKey(
                        name: "fk_novedades_asistencia_miembros_campus_estudiante_id",
                        column: x => x.estudiante_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_novedades_asistencia_tomas_asistencia_toma_id",
                        column: x => x.toma_id,
                        principalSchema: "colegio",
                        principalTable: "tomas_asistencia",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_miembros_campus_codigo_carnet",
                schema: "colegio",
                table: "miembros_campus",
                column: "codigo_carnet",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_anotaciones_estado_tipo",
                schema: "colegio",
                table: "anotaciones",
                columns: new[] { "estado", "tipo" });

            migrationBuilder.CreateIndex(
                name: "ix_anotaciones_estudiante",
                schema: "colegio",
                table: "anotaciones",
                column: "estudiante_id");

            migrationBuilder.CreateIndex(
                name: "ix_anotaciones_fecha",
                schema: "colegio",
                table: "anotaciones",
                column: "fecha");

            migrationBuilder.CreateIndex(
                name: "ix_asignaciones_docentes_asignatura_id",
                schema: "colegio",
                table: "asignaciones_docentes",
                column: "asignatura_id");

            migrationBuilder.CreateIndex(
                name: "ix_asignaciones_docentes_docente",
                schema: "colegio",
                table: "asignaciones_docentes",
                columns: new[] { "anio", "docente_id" });

            migrationBuilder.CreateIndex(
                name: "ix_asignaciones_docentes_docente_id",
                schema: "colegio",
                table: "asignaciones_docentes",
                column: "docente_id");

            migrationBuilder.CreateIndex(
                name: "ux_asignaciones_docentes_curso",
                schema: "colegio",
                table: "asignaciones_docentes",
                columns: new[] { "anio", "asignatura_id", "grado", "grupo" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ux_asignaturas_nombre",
                schema: "colegio",
                table: "asignaturas",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_avisos_destinatario_creado",
                schema: "colegio",
                table: "avisos",
                columns: new[] { "destinatario_id", "creado_en" });

            migrationBuilder.CreateIndex(
                name: "ix_calificaciones_asignatura_id",
                schema: "colegio",
                table: "calificaciones",
                column: "asignatura_id");

            migrationBuilder.CreateIndex(
                name: "ix_calificaciones_periodo",
                schema: "colegio",
                table: "calificaciones",
                column: "periodo_id");

            migrationBuilder.CreateIndex(
                name: "ux_calificaciones_estudiante_asignatura_periodo",
                schema: "colegio",
                table: "calificaciones",
                columns: new[] { "estudiante_id", "asignatura_id", "periodo_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_circulares_anio_numero",
                schema: "colegio",
                table: "circulares",
                columns: new[] { "anio", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_documentos_admision_solicitud",
                schema: "colegio",
                table: "documentos_admision",
                column: "solicitud_id");

            migrationBuilder.CreateIndex(
                name: "ix_eventos_calendario_fechas",
                schema: "colegio",
                table: "eventos_calendario",
                columns: new[] { "inicio", "fin" });

            migrationBuilder.CreateIndex(
                name: "ix_intentos_simulacro_estudiante",
                schema: "colegio",
                table: "intentos_simulacro",
                column: "estudiante_id");

            migrationBuilder.CreateIndex(
                name: "ux_intentos_simulacro_simulacro_estudiante",
                schema: "colegio",
                table: "intentos_simulacro",
                columns: new[] { "simulacro_id", "estudiante_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_lecturas_circular_miembro",
                schema: "colegio",
                table: "lecturas_circular",
                column: "miembro_id");

            migrationBuilder.CreateIndex(
                name: "ux_lecturas_circular_circular_miembro",
                schema: "colegio",
                table: "lecturas_circular",
                columns: new[] { "circular_id", "miembro_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_novedades_asistencia_estudiante",
                schema: "colegio",
                table: "novedades_asistencia",
                column: "estudiante_id");

            migrationBuilder.CreateIndex(
                name: "ux_novedades_asistencia_toma_estudiante",
                schema: "colegio",
                table: "novedades_asistencia",
                columns: new[] { "toma_id", "estudiante_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_periodos_academicos_anio_numero",
                schema: "colegio",
                table: "periodos_academicos",
                columns: new[] { "anio", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_preguntas_simulacro_orden",
                schema: "colegio",
                table: "preguntas_simulacro",
                columns: new[] { "simulacro_id", "orden" });

            migrationBuilder.CreateIndex(
                name: "ix_seguimientos_anotacion_anotacion_id",
                schema: "colegio",
                table: "seguimientos_anotacion",
                column: "anotacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_simulacros_inicio",
                schema: "colegio",
                table: "simulacros",
                column: "inicio");

            migrationBuilder.CreateIndex(
                name: "ix_solicitudes_admision_anio_estado",
                schema: "colegio",
                table: "solicitudes_admision",
                columns: new[] { "anio_lectivo", "estado" });

            migrationBuilder.CreateIndex(
                name: "ix_solicitudes_admision_aspirante",
                schema: "colegio",
                table: "solicitudes_admision",
                columns: new[] { "aspirante_documento", "anio_lectivo" });

            migrationBuilder.CreateIndex(
                name: "ix_solicitudes_admision_estudiante_id",
                schema: "colegio",
                table: "solicitudes_admision",
                column: "estudiante_id");

            migrationBuilder.CreateIndex(
                name: "ux_solicitudes_admision_radicado",
                schema: "colegio",
                table: "solicitudes_admision",
                column: "radicado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tomas_asistencia_fecha_grado",
                schema: "colegio",
                table: "tomas_asistencia",
                columns: new[] { "fecha", "grado" });

            migrationBuilder.CreateIndex(
                name: "ux_tomas_asistencia_asignacion_fecha",
                schema: "colegio",
                table: "tomas_asistencia",
                columns: new[] { "asignacion_id", "fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_vinculos_acudiente_estudiante",
                schema: "colegio",
                table: "vinculos_acudiente",
                column: "estudiante_id");

            migrationBuilder.CreateIndex(
                name: "ux_vinculos_acudiente_acudiente_estudiante",
                schema: "colegio",
                table: "vinculos_acudiente",
                columns: new[] { "acudiente_id", "estudiante_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "avisos",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "calificaciones",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "documentos_admision",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "eventos_calendario",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "intentos_simulacro",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "lecturas_circular",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "novedades_asistencia",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "preguntas_simulacro",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "seguimientos_anotacion",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "vinculos_acudiente",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "periodos_academicos",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "solicitudes_admision",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "circulares",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "tomas_asistencia",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "simulacros",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "anotaciones",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "asignaciones_docentes",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "asignaturas",
                schema: "colegio");

            migrationBuilder.DropIndex(
                name: "ux_miembros_campus_codigo_carnet",
                schema: "colegio",
                table: "miembros_campus");

            migrationBuilder.DropColumn(
                name: "codigo_carnet",
                schema: "colegio",
                table: "miembros_campus");

            migrationBuilder.DropColumn(
                name: "correo",
                schema: "colegio",
                table: "miembros_campus");

            migrationBuilder.DropColumn(
                name: "telefono",
                schema: "colegio",
                table: "miembros_campus");
        }
    }
}
