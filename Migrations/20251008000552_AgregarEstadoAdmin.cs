using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Audicob.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEstadoAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EstadoAdmin",
                table: "Clientes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaDecisionAdmin",
                table: "Clientes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoAdmin",
                table: "Clientes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstadoAdmin",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "FechaDecisionAdmin",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "MotivoAdmin",
                table: "Clientes");
        }
    }
}
