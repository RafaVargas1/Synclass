using Synclass.Domain.Common;

namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Regra de cobrança "valor por aula por frequência semanal contratada":
/// mesma fórmula de <see cref="RegraFixoPorAula"/>
/// (<c>Valor * quantidadeDeAulasNoPeriodo</c>), mas carrega
/// <see cref="FrequenciaSemanalContratada"/> — o Professor já informa, em
/// <see cref="Valor"/>, o preço resolvido para a faixa de frequência
/// escolhida (não há tabela de preço por frequência neste escopo).
/// <see cref="BaseDeContagemAula"/> (issue #186) decide se
/// <c>quantidadeDeAulasNoPeriodo</c> conta aula agendada ou presença
/// confirmada — quem resolve isso é <see cref="ConsultaCobrancaService"/>,
/// não esta classe.
/// </summary>
public sealed class RegraValorPorAula : RegraDeCobranca, IRegraComBaseDeContagemAula
{
    private const int FrequenciaMinima = 1;
    private const int FrequenciaMaxima = 7;

    private RegraValorPorAula(
        Guid id,
        Guid matriculaId,
        decimal valor,
        int frequenciaSemanalContratada,
        BaseDeContagemAula baseDeContagemAula,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        : base(id, matriculaId, valor, createdAt, updatedAt)
    {
        FrequenciaSemanalContratada = frequenciaSemanalContratada;
        BaseDeContagemAula = baseDeContagemAula;
    }

    public int FrequenciaSemanalContratada { get; private set; }

    public BaseDeContagemAula BaseDeContagemAula { get; private set; }

    public static RegraValorPorAula Criar(
        Guid matriculaId,
        decimal valor,
        int frequenciaSemanalContratada,
        IClock clock,
        BaseDeContagemAula baseDeContagemAula = BaseDeContagemAula.Agendamento)
    {
        ValidarFrequenciaSemanalContratada(frequenciaSemanalContratada);
        return new RegraValorPorAula(
            Guid.NewGuid(), matriculaId, valor, frequenciaSemanalContratada, baseDeContagemAula, clock.UtcNow, clock.UtcNow);
    }

    public override decimal CalcularValorDevido(int quantidadeDeAulasNoPeriodo)
    {
        return Valor * quantidadeDeAulasNoPeriodo;
    }

    private static void ValidarFrequenciaSemanalContratada(int frequenciaSemanalContratada)
    {
        if (frequenciaSemanalContratada < FrequenciaMinima || frequenciaSemanalContratada > FrequenciaMaxima)
        {
            throw new FrequenciaSemanalContratadaInvalidaException(frequenciaSemanalContratada);
        }
    }
}
