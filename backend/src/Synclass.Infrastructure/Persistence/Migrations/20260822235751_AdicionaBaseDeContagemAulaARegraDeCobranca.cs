using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synclass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaBaseDeContagemAulaARegraDeCobranca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BaseDeContagemAula",
                table: "RegrasDeCobranca",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RegraValorPorAula_BaseDeContagemAula",
                table: "RegrasDeCobranca",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseDeContagemAula",
                table: "RegrasDeCobranca");

            migrationBuilder.DropColumn(
                name: "RegraValorPorAula_BaseDeContagemAula",
                table: "RegrasDeCobranca");
        }
    }
}
