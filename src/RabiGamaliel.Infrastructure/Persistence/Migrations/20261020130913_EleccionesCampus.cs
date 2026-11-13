using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RabiGamaliel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EleccionesCampus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "elecciones",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fin = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cargos = table.Column<List<string>>(type: "text[]", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_elecciones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "miembros_campus",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    documento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre_completo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    grado = table.Column<int>(type: "integer", nullable: true),
                    grupo = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    hash_clave = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sello_seguridad = table.Column<Guid>(type: "uuid", nullable: false),
                    intentos_fallidos = table.Column<int>(type: "integer", nullable: false),
                    bloqueado_hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ultimo_acceso = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_miembros_campus", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "candidatos",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    eleccion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cargo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    estudiante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre_completo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    grado = table.Column<int>(type: "integer", nullable: false),
                    grupo = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    ambito = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    lema = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    propuestas = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    foto_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidatos", x => x.id);
                    table.ForeignKey(
                        name: "fk_candidatos_elecciones_eleccion_id",
                        column: x => x.eleccion_id,
                        principalSchema: "colegio",
                        principalTable: "elecciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_candidatos_miembros_campus_estudiante_id",
                        column: x => x.estudiante_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "participaciones_voto",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    eleccion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estudiante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    grado = table.Column<int>(type: "integer", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_participaciones_voto", x => x.id);
                    table.ForeignKey(
                        name: "fk_participaciones_voto_elecciones_eleccion_id",
                        column: x => x.eleccion_id,
                        principalSchema: "colegio",
                        principalTable: "elecciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_participaciones_voto_miembros_campus_estudiante_id",
                        column: x => x.estudiante_id,
                        principalSchema: "colegio",
                        principalTable: "miembros_campus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "votos",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    eleccion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cargo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ambito = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    candidato_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_votos", x => x.id);
                    table.ForeignKey(
                        name: "fk_votos_candidatos_candidato_id",
                        column: x => x.candidato_id,
                        principalSchema: "colegio",
                        principalTable: "candidatos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_votos_elecciones_eleccion_id",
                        column: x => x.eleccion_id,
                        principalSchema: "colegio",
                        principalTable: "elecciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_candidatos_estudiante_id",
                schema: "colegio",
                table: "candidatos",
                column: "estudiante_id");

            migrationBuilder.CreateIndex(
                name: "ux_candidatos_eleccion_cargo_estudiante",
                schema: "colegio",
                table: "candidatos",
                columns: new[] { "eleccion_id", "cargo", "estudiante_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_candidatos_eleccion_contienda_numero",
                schema: "colegio",
                table: "candidatos",
                columns: new[] { "eleccion_id", "cargo", "ambito", "numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_elecciones_inicio",
                schema: "colegio",
                table: "elecciones",
                column: "inicio");

            migrationBuilder.CreateIndex(
                name: "ix_miembros_campus_rol_grado_grupo",
                schema: "colegio",
                table: "miembros_campus",
                columns: new[] { "rol", "grado", "grupo" });

            migrationBuilder.CreateIndex(
                name: "ux_miembros_campus_documento",
                schema: "colegio",
                table: "miembros_campus",
                column: "documento",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_participaciones_voto_estudiante_id",
                schema: "colegio",
                table: "participaciones_voto",
                column: "estudiante_id");

            migrationBuilder.CreateIndex(
                name: "ux_participaciones_voto_eleccion_estudiante",
                schema: "colegio",
                table: "participaciones_voto",
                columns: new[] { "eleccion_id", "estudiante_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_votos_candidato_id",
                schema: "colegio",
                table: "votos",
                column: "candidato_id");

            migrationBuilder.CreateIndex(
                name: "ix_votos_eleccion_contienda",
                schema: "colegio",
                table: "votos",
                columns: new[] { "eleccion_id", "cargo", "ambito" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "participaciones_voto",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "votos",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "candidatos",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "elecciones",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "miembros_campus",
                schema: "colegio");
        }
    }
}
