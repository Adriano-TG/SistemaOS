using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarWhatsApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WhatsApp",
                table: "Clientes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WhatsApp",
                table: "Clientes");
        }
    }
}
