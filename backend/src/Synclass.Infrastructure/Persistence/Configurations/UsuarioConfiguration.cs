using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synclass.Domain.Usuarios;

namespace Synclass.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="Usuario"/> para a tabela <c>Usuarios</c>.
/// Índice único em <c>Contato</c> garante, a nível de banco, que a
/// identidade de usuário é única por contato normalizado.
/// </summary>
public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nome).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Contato).IsRequired().HasMaxLength(320);
        builder.Property(u => u.CreatedAt).IsRequired();

        builder.HasIndex(u => u.Contato).IsUnique();

        builder.HasMany(u => u.Papeis)
            .WithOne()
            .HasForeignKey(p => p.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.Papeis).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
