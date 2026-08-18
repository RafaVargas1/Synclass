using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synclass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CriaAulaECancelamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrazoCancelamentoMinutos",
                table: "ConfiguracoesProfessor",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Aulas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HorarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Data = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aulas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Aulas_Horarios_HorarioId",
                        column: x => x.HorarioId,
                        principalTable: "Horarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CancelamentosAula",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AulaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatriculaId = table.Column<Guid>(type: "uuid", nullable: false),
                    CanceladoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancelamentosAula", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CancelamentosAula_Aulas_AulaId",
                        column: x => x.AulaId,
                        principalTable: "Aulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CancelamentosAula_Matriculas_MatriculaId",
                        column: x => x.MatriculaId,
                        principalTable: "Matriculas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Aulas_HorarioId_Data",
                table: "Aulas",
                columns: new[] { "HorarioId", "Data" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CancelamentosAula_AulaId_MatriculaId",
                table: "CancelamentosAula",
                columns: new[] { "AulaId", "MatriculaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CancelamentosAula_MatriculaId",
                table: "CancelamentosAula",
                column: "MatriculaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CancelamentosAula");

            migrationBuilder.DropTable(
                name: "Aulas");

            migrationBuilder.DropColumn(
                name: "PrazoCancelamentoMinutos",
                table: "ConfiguracoesProfessor");
        }
    }
}
