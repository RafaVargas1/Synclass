using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synclass.Domain.Aulas;
using Synclass.Domain.Matriculas;

namespace Synclass.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="CancelamentoAula"/> para a tabela
/// <c>CancelamentosAula</c>. Índice único em (AulaId, MatriculaId) é o guard
/// rail final contra a corrida concorrente entre dois cancelamentos do mesmo
/// Aluno na mesma aula (issue #10, mesma postura de
/// <c>AlocacaoHorarioConfiguration</c>).
/// </summary>
public sealed class CancelamentoAulaConfiguration : IEntityTypeConfiguration<CancelamentoAula>
{
    public void Configure(EntityTypeBuilder<CancelamentoAula> builder)
    {
        builder.ToTable("CancelamentosAula");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.AulaId).IsRequired();
        builder.Property(c => c.MatriculaId).IsRequired();
        builder.Property(c => c.CanceladoEm).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasOne<Aula>().WithMany().HasForeignKey(c => c.AulaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Matricula>().WithMany().HasForeignKey(c => c.MatriculaId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.AulaId, c.MatriculaId }).IsUnique();
    }
}
