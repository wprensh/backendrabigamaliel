using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RabiGamaliel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Certificaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "certificados",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    nombre_beneficiario = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    tipo_documento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    numero_documento = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    grado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    anio_lectivo = table.Column<int>(type: "integer", nullable: true),
                    contenido = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    fecha_expedicion = table.Column<DateOnly>(type: "date", nullable: false),
                    firmante_nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    firmante_cargo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    anulado = table.Column<bool>(type: "boolean", nullable: false),
                    motivo_anulacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_certificados", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_certificados_creado",
                schema: "colegio",
                table: "certificados",
                column: "creado_en",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_certificados_numero_documento",
                schema: "colegio",
                table: "certificados",
                column: "numero_documento");

            migrationBuilder.CreateIndex(
                name: "ux_certificados_codigo",
                schema: "colegio",
                table: "certificados",
                column: "codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "certificados",
                schema: "colegio");
        }
    }
}
