using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synclass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CriaRegistroFrequencia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosFrequencia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AulaId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatriculaId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatusProfessor = table.Column<int>(type: "integer", nullable: true),
                    ConfirmadoPeloAluno = table.Column<bool>(type: "boolean", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosFrequencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosFrequencia_Aulas_AulaId",
                        column: x => x.AulaId,
                        principalTable: "Aulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrosFrequencia_Matriculas_MatriculaId",
                        column: x => x.MatriculaId,
                        principalTable: "Matriculas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosFrequencia_AulaId_MatriculaId",
                table: "RegistrosFrequencia",
                columns: new[] { "AulaId", "MatriculaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosFrequencia_MatriculaId",
                table: "RegistrosFrequencia",
                column: "MatriculaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosFrequencia");
        }
    }
}
