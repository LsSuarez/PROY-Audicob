using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audicob.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRelacionAsesorClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AsignacionesAsesores_Clientes_ClienteId",
                table: "AsignacionesAsesores");

            migrationBuilder.DropIndex(
                name: "IX_AsignacionesAsesores_ClienteId",
                table: "AsignacionesAsesores");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "AsignacionesAsesores");

            migrationBuilder.AddColumn<int>(
                name: "AsignacionAsesorId",
                table: "Clientes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_AsignacionAsesorId",
                table: "Clientes",
                column: "AsignacionAsesorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_AsignacionesAsesores_AsignacionAsesorId",
                table: "Clientes",
                column: "AsignacionAsesorId",
                principalTable: "AsignacionesAsesores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_AsignacionesAsesores_AsignacionAsesorId",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_AsignacionAsesorId",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "AsignacionAsesorId",
                table: "Clientes");

            migrationBuilder.AddColumn<int>(
                name: "ClienteId",
                table: "AsignacionesAsesores",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesAsesores_ClienteId",
                table: "AsignacionesAsesores",
                column: "ClienteId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AsignacionesAsesores_Clientes_ClienteId",
                table: "AsignacionesAsesores",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
