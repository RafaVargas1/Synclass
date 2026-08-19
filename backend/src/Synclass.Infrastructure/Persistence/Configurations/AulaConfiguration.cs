using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synclass.Domain.Aulas;
using Synclass.Domain.Horarios;

namespace Synclass.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="Aula"/> para a tabela <c>Aulas</c>.
/// Índice único em (HorarioId, Data) é o guard rail contra instanciação
/// duplicada sob demanda (issue #10) — ver
/// docs/specs/10-cancelamento-aula/implementation.md#modelo-de-dados.
/// </summary>
public sealed class AulaConfiguration : IEntityTypeConfiguration<Aula>
{
    public void Configure(EntityTypeBuilder<Aula> builder)
    {
        builder.ToTable("Aulas");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.HorarioId).IsRequired();
        builder.Property(a => a.Data).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        builder.HasOne<Horario>().WithMany().HasForeignKey(a => a.HorarioId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.HorarioId, a.Data }).IsUnique();
    }
}
