using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synclass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdicionaTipoMarcacaoHorario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sem defaultValue no banco (issue #73 — Regra de Negócio): a
            // política de marcação precisa ser explícita em toda criação
            // nova, não herdar 0 silenciosamente. Só funciona porque não há
            // linhas existentes em Horarios em produção ainda (ver
            // implementation.md#modelo-de-dados); se um dia isso deixar de
            // ser verdade, esta migration passa a exigir backfill antes.
            migrationBuilder.AddColumn<int>(
                name: "TipoMarcacao",
                table: "Horarios",
                type: "integer",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoMarcacao",
                table: "Horarios");
        }
    }
}
