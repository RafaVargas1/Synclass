using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synclass.Domain.Usuarios;

namespace Synclass.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="PapelAtribuido"/> para a tabela
/// <c>PapeisUsuario</c>. Índice único em (UsuarioId, Papel) impede papel
/// duplicado a nível de banco, não só de aplicação.
/// </summary>
public sealed class PapelAtribuidoConfiguration : IEntityTypeConfiguration<PapelAtribuido>
{
    public void Configure(EntityTypeBuilder<PapelAtribuido> builder)
    {
        builder.ToTable("PapeisUsuario");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Papel).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasIndex(p => new { p.UsuarioId, p.Papel }).IsUnique();
    }
}
