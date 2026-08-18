using FluentAssertions;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Cobrancas;

/// <summary>
/// Garante o critério de aceite 4 da issue #11: o contrato
/// <see cref="IRegraDeCobranca"/> não muda entre implementações — cada
/// regra concreta é instanciada e chamada só pela referência da interface,
/// sem nenhum cast/membro específico de subclasse.
/// </summary>
public sealed class RegrasDeCobrancaContratoTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero));

    public static IEnumerable<object[]> Implementacoes()
    {
        var matriculaId = Guid.NewGuid();
        yield return new object[] { RegraFixoMensal.Criar(matriculaId, 300m, Clock) };
        yield return new object[] { RegraFixoPorAula.Criar(matriculaId, 40m, Clock) };
        yield return new object[] { RegraValorPorAula.Criar(matriculaId, 40m, frequenciaSemanalContratada: 3, Clock) };
    }

    [Theory]
    [MemberData(nameof(Implementacoes))]
    public void CalcularValorDevido_PelaReferenciaDaInterface_NaoLancaEDevolveValorNaoNegativo(IRegraDeCobranca regra)
    {
        var valorDevido = regra.CalcularValorDevido(4);

        valorDevido.Should().BeGreaterThanOrEqualTo(0m);
        regra.MatriculaId.Should().NotBeEmpty();
        regra.Id.Should().NotBeEmpty();
    }
}
