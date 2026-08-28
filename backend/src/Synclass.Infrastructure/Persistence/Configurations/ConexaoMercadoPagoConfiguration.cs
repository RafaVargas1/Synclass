using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synclass.Domain.Pagamentos;
using Synclass.Domain.Usuarios;

namespace Synclass.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="ConexaoMercadoPago"/> para a tabela
/// <c>ConexaoMercadoPago</c> (issue #203). Relação N:1 com
/// <see cref="Usuario"/> apenas como FK, sem propriedade de navegação (ver
/// implementation.md#por-que-sem-navegação: nenhum consumer navega de
/// conexão para usuário ou vice-versa). <see cref="ConexaoMercadoPago.AccessToken"/>
/// e <see cref="ConexaoMercadoPago.RefreshToken"/> são criptografados em
/// repouso via <see cref="EncryptingValueConverter"/> (IDataProtector —
/// credenciais de terceiros que abrem o cofre do Professor, ver
/// implementation.md#decisão-de-design-criptografia).
/// </summary>
public sealed class ConexaoMercadoPagoConfiguration : IEntityTypeConfiguration<ConexaoMercadoPago>
{
    private const int TamanhoCollectorId = 32;
    private const int TamanhoState = 64;

    public void Configure(EntityTypeBuilder<ConexaoMercadoPago> builder)
    {
        builder.ToTable("ConexaoMercadoPago");
        builder.HasKey(c => c.Id);
        // Id gerado pela aplicação (Guid.NewGuid() em
        // ConexaoMercadoPago.IniciarFluxoDeAutorizacao), não pelo banco —
        // mesmo ajuste de ConfiguracaoProfessorConfiguration.
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.ProfessorId).IsRequired();

        // Tokens criptografados em repouso. As colunas se chamam
        // *CipherText* (não AccessToken/RefreshToken) porque o que é
        // persistido é o ciphertext, protegido via EncryptingValueConverter
        // — o texto plano só existe no ciclo de vida da aplicação (ver
        // implementation.md#decisão-de-design-criptografia).
        builder.Property(c => c.AccessToken)
            .HasColumnName("AccessTokenCipherText")
            .HasConversion(new EncryptingValueConverter())
            .IsRequired();
        builder.Property(c => c.RefreshToken)
            .HasColumnName("RefreshTokenCipherText")
            .HasConversion(new EncryptingValueConverter())
            .IsRequired();

        builder.Property(c => c.CollectorId).IsRequired().HasMaxLength(TamanhoCollectorId);
        builder.Property(c => c.State).HasMaxLength(TamanhoState);
        builder.Property(c => c.ExpiraEm).IsRequired();
        builder.Property(c => c.StateExpiraEm).IsRequired(false);

        // CriadoEm com default do banco (now()) — mesma decisão de
        // ConfiguracaoProfessorConfiguration e das demais entidades do
        // schema (ver implementation.md#modelo-de-dados).
        builder.Property(c => c.CriadoEm).IsRequired().HasDefaultValueSql("now()");
        builder.Property(c => c.AtualizadoEm).IsRequired();

        builder.HasIndex(c => c.ProfessorId).IsUnique();

        builder.HasOne<Usuario>().WithMany().HasForeignKey(c => c.ProfessorId).OnDelete(DeleteBehavior.Restrict);
    }
}
