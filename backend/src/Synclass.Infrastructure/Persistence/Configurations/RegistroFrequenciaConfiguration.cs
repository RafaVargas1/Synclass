using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synclass.Domain.Aulas;
using Synclass.Domain.Frequencias;
using Synclass.Domain.Matriculas;

namespace Synclass.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="RegistroFrequencia"/> para a tabela
/// <c>RegistrosFrequencia</c>. Índice único em (AulaId, MatriculaId) — mesma
/// postura de <c>CancelamentoAulaConfiguration</c> (issue #10).
/// </summary>
public sealed class RegistroFrequenciaConfiguration : IEntityTypeConfiguration<RegistroFrequencia>
{
    public void Configure(EntityTypeBuilder<RegistroFrequencia> builder)
    {
        builder.ToTable("RegistrosFrequencia");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.AulaId).IsRequired();
        builder.Property(r => r.MatriculaId).IsRequired();
        builder.Property(r => r.StatusProfessor);
        builder.Property(r => r.ConfirmadoPeloAluno);
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();

        builder.HasOne<Aula>().WithMany().HasForeignKey(r => r.AulaId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Matricula>().WithMany().HasForeignKey(r => r.MatriculaId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.AulaId, r.MatriculaId }).IsUnique();
    }
}
