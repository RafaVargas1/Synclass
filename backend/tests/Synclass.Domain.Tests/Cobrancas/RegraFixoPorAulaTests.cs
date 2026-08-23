using FluentAssertions;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Cobrancas;

/// <summary>
/// Cobre <see cref="RegraFixoPorAula"/> (issue #11): valor devido =
/// <c>Valor * quantidadeDeAulasNoPeriodo</c>, sem parâmetro de frequência
/// contratada (diferença de <see cref="RegraValorPorAula"/> é semântica, não
/// de fórmula — ver implementation.md).
/// </summary>
public sealed class RegraFixoPorAulaTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(4, 200)]
    [InlineData(8, 400)]
    public void CalcularValorDevido_MultiplicaValorPelaQuantidadeDeAulas(int quantidadeDeAulasNoPeriodo, decimal valorEsperado)
    {
        var regra = RegraFixoPorAula.Criar(Guid.NewGuid(), 50m, Clock);

        var valorDevido = regra.CalcularValorDevido(quantidadeDeAulasNoPeriodo);

        valorDevido.Should().Be(valorEsperado);
    }

    [Fact]
    public void Criar_SemInformarBaseDeContagem_AplicaDefaultAgendamento()
    {
        var regra = RegraFixoPorAula.Criar(Guid.NewGuid(), 50m, Clock);

        regra.BaseDeContagemAula.Should().Be(BaseDeContagemAula.Agendamento);
    }

    [Fact]
    public void Criar_ComBaseDeContagemInformada_UsaOValorInformado()
    {
        var regra = RegraFixoPorAula.Criar(Guid.NewGuid(), 50m, Clock, BaseDeContagemAula.PresencaConfirmada);

        regra.BaseDeContagemAula.Should().Be(BaseDeContagemAula.PresencaConfirmada);
    }
}
