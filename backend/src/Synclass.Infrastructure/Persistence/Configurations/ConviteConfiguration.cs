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
    private const int TamanhoCodigo = 5;

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
        // Sem índice único, ao contrário de Token: a unicidade do código
        // (issue #62) é uma regra de negócio contra o subconjunto "convites
        // ativos", validada em ConviteService, não uma constraint global de
        // banco — dois convites finalizados podem compartilhar código.
        builder.Property(c => c.Codigo).IsRequired().HasMaxLength(TamanhoCodigo);
        builder.Property(c => c.ExpiraEm).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasIndex(c => c.Token).IsUnique();
        // Não único (ao contrário de Token): a unicidade do código (issue
        // #62) vale só entre convites ativos, validada em ConviteService —
        // dois convites finalizados podem compartilhar código. O índice
        // existe só para acelerar a query de ExisteCodigoAtivoAsync.
        builder.HasIndex(c => c.Codigo);

        builder.HasOne<Usuario>().WithMany().HasForeignKey(c => c.ProfessorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Matricula>().WithMany().HasForeignKey(c => c.MatriculaId).OnDelete(DeleteBehavior.Restrict);
    }
}
