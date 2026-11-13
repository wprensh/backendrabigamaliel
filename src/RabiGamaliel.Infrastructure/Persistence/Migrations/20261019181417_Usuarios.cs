using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RabiGamaliel.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Usuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "usuarios",
                schema: "colegio",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre_usuario = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    nombre_completo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    correo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    hash_clave = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    es_super_administrador = table.Column<bool>(type: "boolean", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    sello_seguridad = table.Column<Guid>(type: "uuid", nullable: false),
                    intentos_fallidos = table.Column<int>(type: "integer", nullable: false),
                    bloqueado_hasta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ultimo_acceso = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    permisos = table.Column<List<string>>(type: "text[]", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ux_usuarios_nombre_usuario",
                schema: "colegio",
                table: "usuarios",
                column: "nombre_usuario",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usuarios",
                schema: "colegio");
        }
    }
}
