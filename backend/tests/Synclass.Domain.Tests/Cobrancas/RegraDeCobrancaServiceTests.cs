using FluentAssertions;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Cobrancas;

/// <summary>
/// Cobre <see cref="RegraDeCobrancaService.DefinirAsync"/> (issue #11):
/// criação quando a matrícula não tem regra, upsert quando já tem uma (FK
/// única em <c>MatriculaId</c>), e rejeição para matrícula inexistente.
/// </summary>
public sealed class RegraDeCobrancaServiceTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero));

    private static Matricula CriarMatriculaExistente(FakeMatriculaRepository matriculas)
    {
        var matricula = Matricula.CriarProvisoria(Guid.NewGuid(), "Aluno Teste", "aluno-teste", Clock);
        matriculas.AdicionarAsync(matricula, CancellationToken.None).GetAwaiter().GetResult();
        return matricula;
    }

    [Fact]
    public async Task DefinirAsync_MatriculaSemRegra_CriaNovaRegra()
    {
        var matriculas = new FakeMatriculaRepository();
        var matricula = CriarMatriculaExistente(matriculas);
        var regras = new FakeRegraDeCobrancaRepository();
        var servico = new RegraDeCobrancaService(regras, matriculas, Clock);

        var resultado = await servico.DefinirAsync(
            matricula.Id, TipoRegraDeCobranca.FixoMensal, 300m, frequenciaSemanalContratada: null, CancellationToken.None);

        resultado.Regra.Should().BeOfType<RegraFixoMensal>();
        resultado.ValorAnterior.Should().BeNull();
        regras.Regras.Should().ContainSingle(r => r.MatriculaId == matricula.Id);
    }

    [Fact]
    public async Task DefinirAsync_MatriculaComRegraDeOutroTipo_SubstituiARegraExistente()
    {
        var matriculas = new FakeMatriculaRepository();
        var matricula = CriarMatriculaExistente(matriculas);
        var regras = new FakeRegraDeCobrancaRepository();
        var servico = new RegraDeCobrancaService(regras, matriculas, Clock);
        await servico.DefinirAsync(matricula.Id, TipoRegraDeCobranca.FixoMensal, 300m, null, CancellationToken.None);

        var resultado = await servico.DefinirAsync(
            matricula.Id, TipoRegraDeCobranca.ValorPorAula, 50m, frequenciaSemanalContratada: 3, CancellationToken.None);

        resultado.Regra.Should().BeOfType<RegraValorPorAula>();
        resultado.ValorAnterior.Should().Be(300m);
        regras.Regras.Should().ContainSingle(r => r.MatriculaId == matricula.Id);
        regras.Regras.Single().Should().BeOfType<RegraValorPorAula>();
    }

    [Fact]
    public async Task DefinirAsync_MatriculaInexistente_RejeitaComMatriculaNaoEncontradaException()
    {
        var matriculas = new FakeMatriculaRepository();
        var regras = new FakeRegraDeCobrancaRepository();
        var servico = new RegraDeCobrancaService(regras, matriculas, Clock);

        var acao = () => servico.DefinirAsync(
            Guid.NewGuid(), TipoRegraDeCobranca.FixoMensal, 300m, null, CancellationToken.None);

        await acao.Should().ThrowAsync<MatriculaNaoEncontradaException>();
        regras.Regras.Should().BeEmpty();
    }
}
