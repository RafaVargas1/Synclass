using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Usuarios;

namespace Synclass.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="Matricula"/> para a tabela
/// <c>Matriculas</c>. Índice único parcial em
/// (<c>ProfessorId</c>, <c>IdentificadorProvisorio</c>) onde não nulo
/// garante, a nível de banco, a Regra de Negócio da issue #3: identificador
/// único por Professor, não globalmente.
/// </summary>
public sealed class MatriculaConfiguration : IEntityTypeConfiguration<Matricula>
{
    public void Configure(EntityTypeBuilder<Matricula> builder)
    {
        builder.ToTable("Matriculas");
        builder.HasKey(m => m.Id);
        // Id gerado pela aplicação (Guid.NewGuid() em Matricula.CriarProvisoria),
        // não pelo banco — mesmo ajuste e justificativa de UsuarioConfiguration.
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.ProfessorId).IsRequired();
        builder.Property(m => m.NomeProvisorio).HasMaxLength(NomeUsuario.TamanhoMaximo);
        builder.Property(m => m.IdentificadorProvisorio).HasMaxLength(IdentificadorProvisorio.TamanhoMaximo);
        builder.Property(m => m.CreatedAt).IsRequired();

        // Sem navegação de Usuario para Matricula (nenhum requisito funcional
        // ainda precisa navegar "meus Alunos" a partir de Usuario) — só a FK,
        // resolvida via Guid simples, como já feito para ProfessorId acima.
        builder.HasOne<Usuario>().WithMany().HasForeignKey(m => m.ProfessorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(m => m.AlunoUsuarioId).OnDelete(DeleteBehavior.Restrict);

        // Índice único parcial: unicidade só entre matrículas provisórias
        // (identificador não nulo) e só dentro do mesmo Professor.
        builder.HasIndex(m => new { m.ProfessorId, m.IdentificadorProvisorio })
            .IsUnique()
            .HasFilter("\"IdentificadorProvisorio\" IS NOT NULL");
    }
}
