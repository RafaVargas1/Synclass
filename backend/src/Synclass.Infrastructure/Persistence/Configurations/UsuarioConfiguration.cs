using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synclass.Domain.Usuarios;
using Synclass.Infrastructure.Alunos;

namespace Synclass.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="Usuario"/> para a tabela <c>Usuarios</c>.
/// Índice único em <c>Contato</c> garante, a nível de banco, que a
/// identidade de usuário é única por contato normalizado. Índice único
/// parcial em <c>IdentificadorAluno</c> (onde não nulo) garante que cada
/// identificador de Aluno emitido é único no sistema (issue #70).
/// </summary>
public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.Id);
        // Id gerado pela aplicação (Guid.NewGuid() em Usuario.Cadastrar), não
        // pelo banco — ver o mesmo ajuste e a mesma justificativa em
        // PapelAtribuidoConfiguration.
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Nome).IsRequired().HasMaxLength(NomeUsuario.TamanhoMaximo);
        builder.Property(u => u.Contato).IsRequired().HasMaxLength(Contato.TamanhoMaximo);
        builder.Property(u => u.IdentificadorAluno).HasMaxLength(GeradorDeIdentificadorAluno.TamanhoIdentificador);
        builder.Property(u => u.CreatedAt).IsRequired();

        builder.HasIndex(u => u.Contato).IsUnique();

        // Índice único parcial: unicidade só entre identificadores não nulos
        // — um Usuario que nunca foi Aluno não compete por um valor.
        builder.HasIndex(u => u.IdentificadorAluno)
            .IsUnique()
            .HasFilter("\"IdentificadorAluno\" IS NOT NULL");

        builder.HasMany(u => u.Papeis)
            .WithOne()
            .HasForeignKey(p => p.UsuarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.Papeis).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
