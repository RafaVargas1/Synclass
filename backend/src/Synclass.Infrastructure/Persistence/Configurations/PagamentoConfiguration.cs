using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Pagamentos;
using Synclass.Domain.Usuarios;

namespace Synclass.Infrastructure.Persistence.Configurations;

/// <summary>
/// Mapeamento EF Core de <see cref="Pagamento"/> para a tabela
/// <c>Pagamentos</c> (issue #199). Sem índice único em
/// (MatriculaId, PeriodoInicio, PeriodoFimExclusivo) — um período pode ter
/// tentativas falhas + um pagamento pendente novo; a unicidade do pendente
/// é garantida pela busca em <see cref="IPagamentoRepository"/> na lógica de
/// domínio, não por constraint de banco (ver
/// implementation.md#migration). <see cref="Pagamento.EventoId"/> é nullable
/// (pagamentos pendentes ainda não têm evento de webhook associado — ver
/// implementation.md#modelo-de-dados).
/// </summary>
public sealed class PagamentoConfiguration : IEntityTypeConfiguration<Pagamento>
{
    private const int TamanhoUrlCheckout = 500;
    private const int TamanhoReferenciaExterna = 64;
    private const int TamanhoEventoId = 64;

    public void Configure(EntityTypeBuilder<Pagamento> builder)
    {
        builder.ToTable("Pagamentos");
        builder.HasKey(p => p.Id);
        // Id gerado pela aplicação (Guid.NewGuid() em PagamentoService.IniciarAsync),
        // não pelo banco — mesmo padrão das demais entidades.
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.MatriculaId).IsRequired();
        builder.Property(p => p.AlunoUsuarioId).IsRequired();
        builder.Property(p => p.ProfessorId).IsRequired();
        builder.Property(p => p.Valor).IsRequired();
        builder.Property(p => p.PeriodoInicio).IsRequired();
        builder.Property(p => p.PeriodoFimExclusivo).IsRequired();

        // Status persistido por nome (ToString), nunca por ordinal — mais
        // estável diante de novos estados (ver implementation.md#entidade-pagamento).
        builder.Property(p => p.Status).HasConversion<string>().IsRequired();

        builder.Property(p => p.ReferenciaExterna).HasMaxLength(TamanhoReferenciaExterna);
        builder.Property(p => p.UrlCheckout).IsRequired().HasMaxLength(TamanhoUrlCheckout);
        builder.Property(p => p.CriadoEm).IsRequired();
        builder.Property(p => p.ConfirmadoEm).IsRequired(false);
        builder.Property(p => p.FalhouEm).IsRequired(false);
        builder.Property(p => p.EventoId).IsRequired(false).HasMaxLength(TamanhoEventoId);

        builder.HasOne<Matricula>().WithMany().HasForeignKey(p => p.MatriculaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(p => p.AlunoUsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Usuario>().WithMany().HasForeignKey(p => p.ProfessorId).OnDelete(DeleteBehavior.Restrict);
    }
}
