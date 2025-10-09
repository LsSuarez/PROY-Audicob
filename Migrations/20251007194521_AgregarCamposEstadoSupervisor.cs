using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audicob.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCamposEstadoSupervisor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Estado",
                table: "Clientes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UsuarioSupervisor",
                table: "Clientes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Estado",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "UsuarioSupervisor",
                table: "Clientes");
        }
    }
}
