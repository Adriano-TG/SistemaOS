using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarConfiguracoesImpressao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrientacaoIndex",
                table: "DadosEmpresa",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TamanhoPapelIndex",
                table: "DadosEmpresa",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TipoOsIndex",
                table: "DadosEmpresa",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrientacaoIndex",
                table: "DadosEmpresa");

            migrationBuilder.DropColumn(
                name: "TamanhoPapelIndex",
                table: "DadosEmpresa");

            migrationBuilder.DropColumn(
                name: "TipoOsIndex",
                table: "DadosEmpresa");
        }
    }
}
