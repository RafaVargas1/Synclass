using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synclass.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MigraTipoMarcacaoHorarioExistente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migration de dados (issue #75), não de schema — a coluna
            // TipoMarcacao já existe desde a Task #73. Deriva o valor de
            // cada Horario existente a partir de
            // ConfiguracoesProfessor.ModeloAgendamento +
            // AlocacoesHorario.OrigemAlocacao, replicando o comportamento
            // observável que cada horário já tinha sob o modelo antigo —
            // ver a tabela de derivação em
            // docs/specs/75-migra-tipo-marcacao-horario/implementation.md.
            // Idempotente: recalcula sempre a partir de cp/a, não depende do
            // valor anterior da própria coluna, então rodar de novo produz o
            // mesmo resultado.
            //
            // Subquery correlacionada em vez de "UPDATE ... FROM" com alias
            // no alvo (proposto em implementation.md): o SQLite usado no
            // teste de integração (ver
            // MigraTipoMarcacaoHorarioExistenteTests) não aceita alias
            // diretamente após o nome da tabela em UPDATE — forma abaixo é
            // portável entre Postgres (produção) e SQLite (teste).
            migrationBuilder.Sql(
                """
                UPDATE "Horarios"
                SET "TipoMarcacao" = (
                    SELECT CASE
                        WHEN cp."ModeloAgendamento" = 0 THEN 0
                        WHEN cp."ModeloAgendamento" = 1 THEN 1
                        WHEN cp."ModeloAgendamento" = 2 THEN
                            CASE WHEN EXISTS (
                                SELECT 1 FROM "AlocacoesHorario" a
                                WHERE a."HorarioId" = "Horarios"."Id" AND a."OrigemAlocacao" = 0
                            ) THEN 1 ELSE 0 END
                        ELSE 0
                    END
                    FROM "ConfiguracoesProfessor" cp
                    WHERE cp."ProfessorId" = "Horarios"."ProfessorId"
                )
                WHERE EXISTS (
                    SELECT 1 FROM "ConfiguracoesProfessor" cp WHERE cp."ProfessorId" = "Horarios"."ProfessorId"
                );
                """);

            // Horário "órfão" (sem ConfiguracaoProfessor correspondente) —
            // cenário defensivo, inalcançável no fluxo normal (ver
            // implementation.md#edge-points): default único Livre.
            migrationBuilder.Sql(
                """
                UPDATE "Horarios"
                SET "TipoMarcacao" = 0
                WHERE NOT EXISTS (
                    SELECT 1 FROM "ConfiguracoesProfessor" cp WHERE cp."ProfessorId" = "Horarios"."ProfessorId"
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op intencional: só faz sentido reverter schema, não dado —
            // a Task #73 (coluna TipoMarcacao) já cobre a reversão de
            // schema. Reverter esta migration não teria para onde voltar o
            // TipoMarcacao (o valor anterior de ModeloAgendamento continua
            // intacto em ConfiguracoesProfessor, não foi removido por esta
            // Task).
        }
    }
}
