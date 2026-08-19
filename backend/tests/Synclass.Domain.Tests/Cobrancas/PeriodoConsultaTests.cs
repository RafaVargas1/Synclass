using FluentAssertions;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Horarios;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Cobrancas;

/// <summary>
/// Cobre <see cref="PeriodoConsulta"/> (issue #12): validação de
/// `inicio`/`fim`, resolução do mês corrente a partir de um <see cref="FixedClock"/>
/// e a contagem de ocorrências semanais usada por
/// <see cref="ConsultaCobrancaService"/> para chegar em
/// `quantidadeDeAulasNoPeriodo` (ver implementation.md).
/// </summary>
public sealed class PeriodoConsultaTests
{
    [Fact]
    public void Criar_InicioMaiorOuIgualAFim_RejeitaComPeriodoConsultaInvalidoException()
    {
        var inicio = new DateOnly(2026, 8, 10);
        var fim = new DateOnly(2026, 8, 10);

        var acao = () => PeriodoConsulta.Criar(inicio, fim);

        acao.Should().Throw<PeriodoConsultaInvalidoException>();
    }

    [Fact]
    public void MesCorrente_UsaClock_RetornaPrimeiroDiaDoMesAtePrimeiroDiaDoMesSeguinteExclusivo()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero));

        var periodo = PeriodoConsulta.MesCorrente(clock);

        periodo.Inicio.Should().Be(new DateOnly(2026, 8, 1));
        periodo.FimExclusivo.Should().Be(new DateOnly(2026, 9, 1));
    }

    [Fact]
    public void ContarOcorrencias_MesComQuatroTercasFeiras_Retorna4()
    {
        // Agosto/2026 tem só 4 terças-feiras: 04, 11, 18, 25.
        var periodo = PeriodoConsulta.Criar(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1));

        var quantidade = periodo.ContarOcorrencias(DiaSemana.Terca);

        quantidade.Should().Be(4);
    }

    [Fact]
    public void ContarOcorrencias_MesComCincoSegundasFeiras_Retorna5()
    {
        // Agosto/2026 tem 5 segundas-feiras: 03, 10, 17, 24, 31.
        var periodo = PeriodoConsulta.Criar(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1));

        var quantidade = periodo.ContarOcorrencias(DiaSemana.Segunda);

        quantidade.Should().Be(5);
    }

    [Fact]
    public void GerarDatas_MesComQuatroTercasFeiras_RetornaAsQuatroDatas()
    {
        // Agosto/2026 tem só 4 terças-feiras: 04, 11, 18, 25.
        var periodo = PeriodoConsulta.Criar(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1));

        var datas = periodo.GerarDatas(DiaSemana.Terca);

        datas.Should().BeEquivalentTo(new[]
        {
            new DateOnly(2026, 8, 4),
            new DateOnly(2026, 8, 11),
            new DateOnly(2026, 8, 18),
            new DateOnly(2026, 8, 25),
        });
    }

    [Fact]
    public void GerarDatas_FimExclusivoNaoEntraNoResultado_RespeitaLimiteSuperior()
    {
        var periodo = PeriodoConsulta.Criar(new DateOnly(2026, 8, 18), new DateOnly(2026, 8, 25));

        var datas = periodo.GerarDatas(DiaSemana.Terca);

        datas.Should().BeEquivalentTo(new[] { new DateOnly(2026, 8, 18) });
    }
}
