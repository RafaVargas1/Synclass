using FluentAssertions;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Cobrancas;

/// <summary>
/// Cobre <see cref="RegraFixoMensal"/> (issue #11): o valor devido nunca
/// depende de <c>quantidadeDeAulasNoPeriodo</c>, ao contrário das outras
/// implementações de <see cref="IRegraDeCobranca"/> — RN do card (fixo
/// mensal não depende de frequência real).
/// </summary>
public sealed class RegraFixoMensalTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero));

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(30)]
    public void CalcularValorDevido_IgnoraQuantidadeDeAulas_RetornaSempreValor(int quantidadeDeAulasNoPeriodo)
    {
        var regra = RegraFixoMensal.Criar(Guid.NewGuid(), 350m, Clock);

        var valorDevido = regra.CalcularValorDevido(quantidadeDeAulasNoPeriodo);

        valorDevido.Should().Be(350m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Criar_ComValorMenorOuIgualAZero_LancaValorDeRegraDeCobrancaInvalidoException(decimal valor)
    {
        var acao = () => RegraFixoMensal.Criar(Guid.NewGuid(), valor, Clock);

        acao.Should().Throw<ValorDeRegraDeCobrancaInvalidoException>();
    }
}
