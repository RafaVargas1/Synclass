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
        // O Id é gerado pela aplicação (Guid.NewGuid() em PapelAtribuido.Criar),
        // nunca pelo banco. Sem ValueGeneratedNever(), o EF Core assume por
        // convenção que toda chave Guid é gerada no INSERT (ValueGeneratedOnAdd)
        // — como o valor já vem preenchido antes de qualquer rastreamento, o
        // DetectChanges() interpreta uma nova entidade descoberta via a
        // navegação Papeis (sem Add() explícito) como já existente no banco,
        // gerando um UPDATE (0 linhas afetadas) em vez do INSERT esperado.
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Papel).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.HasIndex(p => new { p.UsuarioId, p.Papel }).IsUnique();
    }
}
