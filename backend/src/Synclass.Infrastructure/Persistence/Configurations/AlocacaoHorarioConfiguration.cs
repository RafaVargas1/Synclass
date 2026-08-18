using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synclass.Domain.Alocacoes;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;

namespace Synclass.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="AlocacaoHorario"/> para a tabela
/// <c>AlocacoesHorario</c>. Índice único em (HorarioId, MatriculaId) é o
/// guard rail final contra a corrida concorrente que duplicaria uma
/// alocação — a checagem prévia em <c>AlocacaoHorarioService</c> cobre o
/// caminho feliz, este índice cobre a janela de corrida (ver
/// docs/specs/8-aluno-horario/implementation.md#edge-points).
/// </summary>
public sealed class AlocacaoHorarioConfiguration : IEntityTypeConfiguration<AlocacaoHorario>
{
    public void Configure(EntityTypeBuilder<AlocacaoHorario> builder)
    {
        builder.ToTable("AlocacoesHorario");
        builder.HasKey(a => a.Id);
        // Id gerado pela aplicação (Guid.NewGuid() em AlocacaoHorario.Criar),
        // não pelo banco — mesmo ajuste e justificativa de HorarioConfiguration.
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.HorarioId).IsRequired();
        builder.Property(a => a.MatriculaId).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        // Remover o Horario (template recorrente) remove suas alocações —
        // na prática nunca dispara via fluxo normal, já que
        // HorarioService.RemoverAsync bloqueia remoção com Alunos alocados
        // (issue #6), mas é a semântica correta para outros caminhos futuros.
        builder.HasOne<Horario>().WithMany().HasForeignKey(a => a.HorarioId).OnDelete(DeleteBehavior.Cascade);
        // Restrict: não existe remoção de Matrícula ainda; evita apagar
        // silenciosamente uma alocação se isso mudar.
        builder.HasOne<Matricula>().WithMany().HasForeignKey(a => a.MatriculaId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.HorarioId, a.MatriculaId }).IsUnique();
    }
}
