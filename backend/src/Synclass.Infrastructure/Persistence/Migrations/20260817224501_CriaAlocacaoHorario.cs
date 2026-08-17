using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synclass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CriaAlocacaoHorario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlocacoesHorario",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HorarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatriculaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlocacoesHorario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlocacoesHorario_Horarios_HorarioId",
                        column: x => x.HorarioId,
                        principalTable: "Horarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlocacoesHorario_Matriculas_MatriculaId",
                        column: x => x.MatriculaId,
                        principalTable: "Matriculas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlocacoesHorario_HorarioId_MatriculaId",
                table: "AlocacoesHorario",
                columns: new[] { "HorarioId", "MatriculaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AlocacoesHorario_MatriculaId",
                table: "AlocacoesHorario",
                column: "MatriculaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlocacoesHorario");
        }
    }
}
