using Synclass.Domain.Common;

namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Regra de cobrança "valor por aula por frequência semanal contratada":
/// mesma fórmula de <see cref="RegraFixoPorAula"/>
/// (<c>Valor * quantidadeDeAulasNoPeriodo</c>), mas carrega
/// <see cref="FrequenciaSemanalContratada"/> — o Professor já informa, em
/// <see cref="Valor"/>, o preço resolvido para a faixa de frequência
/// escolhida (não há tabela de preço por frequência neste escopo).
/// </summary>
public sealed class RegraValorPorAula : RegraDeCobranca
{
    private const int FrequenciaMinima = 1;
    private const int FrequenciaMaxima = 7;

    private RegraValorPorAula(
        Guid id, Guid matriculaId, decimal valor, int frequenciaSemanalContratada, DateTimeOffset createdAt, DateTimeOffset updatedAt)
        : base(id, matriculaId, valor, createdAt, updatedAt)
    {
        FrequenciaSemanalContratada = frequenciaSemanalContratada;
    }

    public int FrequenciaSemanalContratada { get; private set; }

    public static RegraValorPorAula Criar(Guid matriculaId, decimal valor, int frequenciaSemanalContratada, IClock clock)
    {
        ValidarFrequenciaSemanalContratada(frequenciaSemanalContratada);
        return new RegraValorPorAula(Guid.NewGuid(), matriculaId, valor, frequenciaSemanalContratada, clock.UtcNow, clock.UtcNow);
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
