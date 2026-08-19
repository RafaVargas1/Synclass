using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synclass.Domain.Configuracoes;

namespace Synclass.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="ConfiguracaoProfessor"/> para a tabela
/// <c>ConfiguracoesProfessor</c>. Índice único em <c>ProfessorId</c> garante,
/// a nível de banco, 1 configuração por Professor (Regra de Negócio da issue
/// #7). A issue #10 (prazo de cancelamento) adiciona a coluna
/// <c>PrazoCancelamentoMinutos</c> nesta mesma tabela depois — ver
/// docs/specs/7-modelo-agendamento/implementation.md#modelo-de-dados.
/// </summary>
public sealed class ConfiguracaoProfessorConfiguration : IEntityTypeConfiguration<ConfiguracaoProfessor>
{
    public void Configure(EntityTypeBuilder<ConfiguracaoProfessor> builder)
    {
        builder.ToTable("ConfiguracoesProfessor");
        builder.HasKey(c => c.Id);
        // Id gerado pela aplicação (Guid.NewGuid() em ConfiguracaoProfessor.Criar),
        // não pelo banco — mesmo ajuste e justificativa de HorarioConfiguration.
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.ProfessorId).IsRequired();
        // ModeloAgendamento trafega como inteiro (mapeamento padrão de enum
        // do EF Core) — mesma decisão de DiaSemana na issue #6.
        builder.Property(c => c.ModeloAgendamento).IsRequired();
        // Issue #10 — default 0 ("sem antecedência mínima exigida") cobre
        // Professores cadastrados antes desta issue, ver
        // docs/specs/10-cancelamento-aula/implementation.md.
        builder.Property(c => c.PrazoCancelamentoMinutos).IsRequired().HasDefaultValue(0);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        builder.HasIndex(c => c.ProfessorId).IsUnique();

        builder.HasOne<Synclass.Domain.Usuarios.Usuario>()
            .WithMany()
            .HasForeignKey(c => c.ProfessorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
