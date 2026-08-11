using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synclass.Domain.Autenticacao;

namespace Synclass.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="CodigoOtp"/> para a tabela
/// <c>CodigosOtp</c> (ver Critérios técnicos da issue #18).
/// </summary>
public sealed class CodigoOtpConfiguration : IEntityTypeConfiguration<CodigoOtp>
{
    public void Configure(EntityTypeBuilder<CodigoOtp> builder)
    {
        builder.ToTable("CodigosOtp");
        builder.HasKey(c => c.Id);
        // Id gerado pela aplicação (Guid.NewGuid() em CodigoOtp.Gerar), não
        // pelo banco — mesmo ajuste e justificativa de UsuarioConfiguration.
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.UsuarioId).IsRequired();
        builder.Property(c => c.CodigoHash).IsRequired().HasMaxLength(64);
        builder.Property(c => c.ExpiraEm).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasIndex(c => c.UsuarioId);

        builder.HasOne<Domain.Usuarios.Usuario>()
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
