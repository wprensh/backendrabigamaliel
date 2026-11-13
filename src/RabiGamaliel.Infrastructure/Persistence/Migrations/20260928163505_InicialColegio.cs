using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RabiGamaliel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InicialColegio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "colegio");

            migrationBuilder.CreateTable(
                name: "noticias",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    resumen = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    contenido = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    categoria = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    imagen_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    fecha_publicacion = table.Column<DateOnly>(type: "date", nullable: false),
                    publicada = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_noticias", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "solicitudes_pqrs",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    radicado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre_completo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    correo = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    mensaje = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    respuesta = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_solicitudes_pqrs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_noticias_publicada_fecha",
                schema: "colegio",
                table: "noticias",
                columns: new[] { "publicada", "fecha_publicacion" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_noticias_titulo",
                schema: "colegio",
                table: "noticias",
                column: "titulo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pqrs_estado_creado",
                schema: "colegio",
                table: "solicitudes_pqrs",
                columns: new[] { "estado", "creado_en" });

            migrationBuilder.CreateIndex(
                name: "ux_pqrs_radicado",
                schema: "colegio",
                table: "solicitudes_pqrs",
                column: "radicado",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "noticias",
                schema: "colegio");

            migrationBuilder.DropTable(
                name: "solicitudes_pqrs",
                schema: "colegio");
        }
    }
}
