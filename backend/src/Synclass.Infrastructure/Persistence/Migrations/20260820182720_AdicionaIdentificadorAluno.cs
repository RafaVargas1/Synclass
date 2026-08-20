using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synclass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaIdentificadorAluno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentificadorAluno",
                table: "Usuarios",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentificadorAluno",
                table: "Matriculas",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdentificadorAluno",
                table: "Usuarios",
                column: "IdentificadorAluno",
                unique: true,
                filter: "\"IdentificadorAluno\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Matriculas_IdentificadorAluno",
                table: "Matriculas",
                column: "IdentificadorAluno",
                unique: true,
                filter: "\"IdentificadorAluno\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_IdentificadorAluno",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Matriculas_IdentificadorAluno",
                table: "Matriculas");

            migrationBuilder.DropColumn(
                name: "IdentificadorAluno",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "IdentificadorAluno",
                table: "Matriculas");
        }
    }
}
