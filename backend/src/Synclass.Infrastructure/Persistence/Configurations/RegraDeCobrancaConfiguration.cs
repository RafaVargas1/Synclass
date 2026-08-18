using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Matriculas;

namespace Synclass.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="RegraDeCobranca"/> para a tabela
/// <c>RegrasDeCobranca</c>, usando Table-Per-Hierarchy (TPH) — primeiro caso
/// de TPH no projeto (issue #11): as 3 implementações concretas
/// (<see cref="RegraFixoMensal"/>, <see cref="RegraFixoPorAula"/>,
/// <see cref="RegraValorPorAula"/>) compartilham a maior parte do estado
/// (<c>Valor</c>) e diferem só em <c>FrequenciaSemanalContratada</c>
/// (exclusivo de <see cref="RegraValorPorAula"/>), o que não justifica uma
/// tabela por tipo (TPT) — uma única tabela com coluna discriminadora
/// <c>Tipo</c> é mais simples de consultar (não precisa de JOIN para
/// resolver "qual a regra vigente desta matrícula"). Índice único em
/// <c>MatriculaId</c> garante, a nível de banco, no máximo 1 regra vigente
/// por matrícula (upsert, sem histórico versionado — ver
/// implementation.md#edge-points).
/// </summary>
public sealed class RegraDeCobrancaConfiguration : IEntityTypeConfiguration<RegraDeCobranca>
{
    public void Configure(EntityTypeBuilder<RegraDeCobranca> builder)
    {
        builder.ToTable("RegrasDeCobranca");
        builder.HasKey(r => r.Id);
        // Id gerado pela aplicação (Guid.NewGuid() nos métodos Criar de cada
        // implementação), não pelo banco — mesmo ajuste de HorarioConfiguration.
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.MatriculaId).IsRequired();
        builder.Property(r => r.Valor).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.UpdatedAt).IsRequired();

        builder.HasIndex(r => r.MatriculaId).IsUnique();

        builder.HasOne<Matricula>().WithMany().HasForeignKey(r => r.MatriculaId).OnDelete(DeleteBehavior.Cascade);

        // Discriminador de string, mesmos literais do enum TipoRegraDeCobranca
        // (Api/Domain) por consistência de leitura no banco — tipos distintos
        // de propósito, não compartilham o mesmo tipo C# (implementation.md#modelo-de-dados).
        builder.HasDiscriminator<string>("Tipo")
            .HasValue<RegraValorPorAula>("ValorPorAula")
            .HasValue<RegraFixoMensal>("FixoMensal")
            .HasValue<RegraFixoPorAula>("FixoPorAula");
    }
}
