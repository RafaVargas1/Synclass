using Synclass.Domain.Common;

namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Regra de cobrança "fixo por aula": o valor devido é
/// <c>Valor * quantidadeDeAulasNoPeriodo</c>, sem depender de uma frequência
/// semanal contratada (diferença de <see cref="RegraValorPorAula"/> é
/// semântica/de configuração, não de cálculo — ver implementation.md).
/// </summary>
public sealed class RegraFixoPorAula : RegraDeCobranca
{
    private RegraFixoPorAula(Guid id, Guid matriculaId, decimal valor, DateTimeOffset createdAt, DateTimeOffset updatedAt)
        : base(id, matriculaId, valor, createdAt, updatedAt)
    {
    }

    public static RegraFixoPorAula Criar(Guid matriculaId, decimal valor, IClock clock)
    {
        return new RegraFixoPorAula(Guid.NewGuid(), matriculaId, valor, clock.UtcNow, clock.UtcNow);
    }

    public override decimal CalcularValorDevido(int quantidadeDeAulasNoPeriodo)
    {
        return Valor * quantidadeDeAulasNoPeriodo;
    }
}
