using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Audicob.Migrations
{
    /// <inheritdoc />
    public partial class AddHistorialEstadoMora : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EstadoMora",
                table: "Clientes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "FiltrosGuardados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ConfiguracionJson = table.Column<string>(type: "text", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EsPredeterminado = table.Column<bool>(type: "boolean", nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiltrosGuardados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiltrosGuardados_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HistorialEstadosMora",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    EstadoAnterior = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NuevoEstado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UsuarioId = table.Column<string>(type: "text", nullable: false),
                    FechaCambio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    MotivoCambio = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DireccionIP = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialEstadosMora", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialEstadosMora_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HistorialEstadosMora_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FiltrosGuardados_UsuarioId",
                table: "FiltrosGuardados",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadosMora_Cliente_Fecha",
                table: "HistorialEstadosMora",
                columns: new[] { "ClienteId", "FechaCambio" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadosMora_ClienteId",
                table: "HistorialEstadosMora",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadosMora_FechaCambio",
                table: "HistorialEstadosMora",
                column: "FechaCambio");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadosMora_UsuarioId",
                table: "HistorialEstadosMora",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FiltrosGuardados");

            migrationBuilder.DropTable(
                name: "HistorialEstadosMora");

            migrationBuilder.DropColumn(
                name: "EstadoMora",
                table: "Clientes");
        }
    }
}
