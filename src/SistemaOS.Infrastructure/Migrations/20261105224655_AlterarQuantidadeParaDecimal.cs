using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlterarQuantidadeParaDecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "QuantidadeAtual",
                table: "Produtos",
                type: "decimal(18,3)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantidade",
                table: "MovimentosEstoque",
                type: "decimal(18,3)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "QuantidadeAtual",
                table: "Produtos",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)");

            migrationBuilder.AlterColumn<int>(
                name: "Quantidade",
                table: "MovimentosEstoque",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,3)");
        }
    }
}
