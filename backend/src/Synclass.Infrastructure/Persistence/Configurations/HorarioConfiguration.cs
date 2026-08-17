using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synclass.Domain.Horarios;

namespace Synclass.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="Horario"/> para a tabela <c>Horarios</c>.
/// Índice não-único em (ProfessorId, DiaSemana) acelera a consulta de
/// conflito (roda a cada criação) — não pode ser único porque a regra de
/// não-sobreposição é um teste de intervalo, não de igualdade.
/// </summary>
public sealed class HorarioConfiguration : IEntityTypeConfiguration<Horario>
{
    public void Configure(EntityTypeBuilder<Horario> builder)
    {
        builder.ToTable("Horarios");
        builder.HasKey(h => h.Id);
        // Id gerado pela aplicação (Guid.NewGuid() em Horario.Criar), não
        // pelo banco — mesmo ajuste e justificativa de UsuarioConfiguration.
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.ProfessorId).IsRequired();
        // DiaSemana trafega como inteiro (mapeamento padrão de enum do EF
        // Core) — decisão documentada em
        // docs/specs/6-horarios-disponiveis/implementation.md#contrato-de-api.
        builder.Property(h => h.DiaSemana).IsRequired();
        builder.Property(h => h.HoraInicio).IsRequired();
        builder.Property(h => h.DuracaoMinutos).IsRequired();
        builder.Property(h => h.CreatedAt).IsRequired();
        // Default 1 no banco (issue #17) cobre linhas existentes de antes
        // deste card, que nascem como aula individual — mesmo default já
        // aplicado em Horario.Criar quando limiteAlunos é omitido.
        builder.Property(h => h.LimiteAlunos).IsRequired().HasDefaultValue(LimiteAlunosHorario.Padrao);

        builder.HasIndex(h => new { h.ProfessorId, h.DiaSemana });

        builder.HasOne<Synclass.Domain.Usuarios.Usuario>()
            .WithMany()
            .HasForeignKey(h => h.ProfessorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
