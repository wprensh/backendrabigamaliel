using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RabiGamaliel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BancoPreguntas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "pregunta_banco_id",
                schema: "colegio",
                table: "preguntas_simulacro",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "preguntas_banco",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    area = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    competencia = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    contexto = table.Column<string>(type: "character varying(6000)", maxLength: 6000, nullable: true),
                    enunciado = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    imagen_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    correcta = table.Column<int>(type: "integer", nullable: false),
                    explicacion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    dificultad = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    grados = table.Column<List<int>>(type: "integer[]", nullable: false),
                    opciones = table.Column<List<string>>(type: "text[]", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_preguntas_banco", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_preguntas_simulacro_pregunta_banco",
                schema: "colegio",
                table: "preguntas_simulacro",
                column: "pregunta_banco_id");

            migrationBuilder.CreateIndex(
                name: "ix_preguntas_banco_area",
                schema: "colegio",
                table: "preguntas_banco",
                column: "area");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "preguntas_banco",
                schema: "colegio");

            migrationBuilder.DropIndex(
                name: "ix_preguntas_simulacro_pregunta_banco",
                schema: "colegio",
                table: "preguntas_simulacro");

            migrationBuilder.DropColumn(
                name: "pregunta_banco_id",
                schema: "colegio",
                table: "preguntas_simulacro");
        }
    }
}
