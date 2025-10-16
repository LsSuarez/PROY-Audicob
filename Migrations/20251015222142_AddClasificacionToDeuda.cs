using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audicob.Migrations
{
    /// <inheritdoc />
    public partial class AddClasificacionToDeuda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Clasificacion",
                table: "Deudas",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Clasificacion",
                table: "Deudas");
        }
    }
}
