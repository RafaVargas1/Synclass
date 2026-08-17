using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synclass.Domain.Convites;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Usuarios;

namespace Synclass.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="Convite"/> para a tabela <c>Convites</c>.
/// Índice único em <c>Token</c> garante, a nível de banco, que o link de
/// aceite é resolvido sem ambiguidade (issue #2).
/// </summary>
public sealed class ConviteConfiguration : IEntityTypeConfiguration<Convite>
{
    private const int TamanhoMaximoToken = 64;

    public void Configure(EntityTypeBuilder<Convite> builder)
    {
        builder.ToTable("Convites");
        builder.HasKey(c => c.Id);
        // Id gerado pela aplicação (Guid.NewGuid() em Convite.Gerar), não
        // pelo banco — mesmo ajuste e justificativa de UsuarioConfiguration.
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.ProfessorId).IsRequired();
        builder.Property(c => c.Contato).IsRequired().HasMaxLength(Contato.TamanhoMaximo);
        builder.Property(c => c.ContatoTipo).IsRequired();
        builder.Property(c => c.Token).IsRequired().HasMaxLength(TamanhoMaximoToken);
        builder.Property(c => c.ExpiraEm).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasIndex(c => c.Token).IsUnique();

        builder.HasOne<Usuario>().WithMany().HasForeignKey(c => c.ProfessorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Matricula>().WithMany().HasForeignKey(c => c.MatriculaId).OnDelete(DeleteBehavior.Restrict);
    }
}
