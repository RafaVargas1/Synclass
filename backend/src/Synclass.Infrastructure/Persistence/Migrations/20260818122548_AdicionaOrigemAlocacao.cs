using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synclass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaOrigemAlocacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrigemAlocacao",
                table: "AlocacoesHorario",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrigemAlocacao",
                table: "AlocacoesHorario");
        }
    }
}
