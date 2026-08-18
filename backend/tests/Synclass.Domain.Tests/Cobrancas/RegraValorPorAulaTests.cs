using FluentAssertions;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Cobrancas;

/// <summary>
/// Cobre <see cref="RegraValorPorAula"/> (issue #11): mesma fórmula de
/// <see cref="RegraFixoPorAula"/>, mas carrega <c>FrequenciaSemanalContratada</c>
/// (1-7) — o teste de regressão garante que o valor de uma faixa de
/// frequência não se confunde com o de outra faixa (critério de aceite 2).
/// </summary>
public sealed class RegraValorPorAulaTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(4, 200)]
    [InlineData(8, 400)]
    public void CalcularValorDevido_MultiplicaValorPelaQuantidadeDeAulas(int quantidadeDeAulasNoPeriodo, decimal valorEsperado)
    {
        var regra = RegraValorPorAula.Criar(Guid.NewGuid(), 50m, frequenciaSemanalContratada: 3, Clock);

        var valorDevido = regra.CalcularValorDevido(quantidadeDeAulasNoPeriodo);

        valorDevido.Should().Be(valorEsperado);
    }

    [Fact]
    public void CalcularValorDevido_FaixasDeFrequenciaDiferentes_NaoConfundeOValorDaFaixa()
    {
        var regraTresVezesPorSemana = RegraValorPorAula.Criar(Guid.NewGuid(), 60m, frequenciaSemanalContratada: 3, Clock);
        var regraDuasVezesPorSemana = RegraValorPorAula.Criar(Guid.NewGuid(), 45m, frequenciaSemanalContratada: 2, Clock);

        regraTresVezesPorSemana.CalcularValorDevido(12).Should().Be(720m);
        regraDuasVezesPorSemana.CalcularValorDevido(8).Should().Be(360m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(-1)]
    public void Criar_FrequenciaSemanalContratadaForaDeUmASete_RejeitaComFrequenciaSemanalContratadaInvalidaException(
        int frequenciaSemanalContratada)
    {
        var acao = () => RegraValorPorAula.Criar(Guid.NewGuid(), 50m, frequenciaSemanalContratada, Clock);

        acao.Should().Throw<FrequenciaSemanalContratadaInvalidaException>();
    }
}
