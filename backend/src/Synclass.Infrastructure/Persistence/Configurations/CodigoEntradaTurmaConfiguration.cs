using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synclass.Domain.CodigosEntradaTurma;
using Synclass.Domain.Usuarios;

namespace Synclass.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="CodigoEntradaTurma"/> para a tabela
/// <c>CodigosEntradaTurma</c>. Mesma decisão de índice não-único em
/// <c>Codigo</c> que <c>ConviteConfiguration</c>: a unicidade vale só entre
/// códigos ativos, validada em <see cref="CodigoEntradaTurmaService"/>.
/// </summary>
public sealed class CodigoEntradaTurmaConfiguration : IEntityTypeConfiguration<CodigoEntradaTurma>
{
    private const int TamanhoCodigo = 5;

    public void Configure(EntityTypeBuilder<CodigoEntradaTurma> builder)
    {
        builder.ToTable("CodigosEntradaTurma");
        builder.HasKey(c => c.Id);
        // Id gerado pela aplicação (Guid.NewGuid() em CodigoEntradaTurma.Gerar),
        // não pelo banco — mesmo ajuste de ConviteConfiguration.
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.ProfessorId).IsRequired();
        builder.Property(c => c.Codigo).IsRequired().HasMaxLength(TamanhoCodigo);
        builder.Property(c => c.ExpiraEm).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        // Não único: a unicidade do código vale só entre códigos ativos,
        // validada em CodigoEntradaTurmaService.GerarAsync — dois códigos já
        // expirados podem compartilhar o mesmo valor. O índice existe só
        // para acelerar ExisteCodigoAtivoAsync/BuscarAtivoPorCodigoAsync.
        builder.HasIndex(c => c.Codigo);

        builder.HasOne<Usuario>().WithMany().HasForeignKey(c => c.ProfessorId).OnDelete(DeleteBehavior.Restrict);
    }
}
