using Synclass.Domain.Common;

namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Regra de cobrança "fixo por aula": o valor devido é
/// <c>Valor * quantidadeDeAulasNoPeriodo</c>, sem depender de uma frequência
/// semanal contratada (diferença de <see cref="RegraValorPorAula"/> é
/// semântica/de configuração, não de cálculo — ver implementation.md).
/// <see cref="BaseDeContagemAula"/> (issue #186) decide se
/// <c>quantidadeDeAulasNoPeriodo</c> conta aula agendada ou presença
/// confirmada — quem resolve isso é <see cref="ConsultaCobrancaService"/>,
/// não esta classe.
/// </summary>
public sealed class RegraFixoPorAula : RegraDeCobranca, IRegraComBaseDeContagemAula
{
    private RegraFixoPorAula(
        Guid id, Guid matriculaId, decimal valor, BaseDeContagemAula baseDeContagemAula, DateTimeOffset createdAt, DateTimeOffset updatedAt)
        : base(id, matriculaId, valor, createdAt, updatedAt)
    {
        BaseDeContagemAula = baseDeContagemAula;
    }

    public BaseDeContagemAula BaseDeContagemAula { get; private set; }

    public static RegraFixoPorAula Criar(
        Guid matriculaId, decimal valor, IClock clock, BaseDeContagemAula baseDeContagemAula = BaseDeContagemAula.Agendamento)
    {
        return new RegraFixoPorAula(Guid.NewGuid(), matriculaId, valor, baseDeContagemAula, clock.UtcNow, clock.UtcNow);
    }

    public override decimal CalcularValorDevido(int quantidadeDeAulasNoPeriodo)
    {
        return Valor * quantidadeDeAulasNoPeriodo;
    }
}
