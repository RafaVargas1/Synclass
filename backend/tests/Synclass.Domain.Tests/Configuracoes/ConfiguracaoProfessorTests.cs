using FluentAssertions;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Configuracoes;

namespace Synclass.Domain.Tests.Configuracoes;

/// <summary>
/// Cobre a Regra de Negócio central da issue #7 na entidade: criação,
/// troca de modelo sem versionamento e a regra pura de
/// <see cref="ConfiguracaoProfessor.PermiteMarcacaoLivre"/> usada como guard
/// rail pelas issues #8/#9 (ver implementation.md#dependência-das-issues-8-e-9).
/// </summary>
public sealed class ConfiguracaoProfessorTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
    private static readonly Guid ProfessorId = Guid.NewGuid();

    [Fact]
    public void Criar_ModeloInformado_CriaConfiguracaoComCamposInformados()
    {
        var configuracao = ConfiguracaoProfessor.Criar(ProfessorId, ModeloAgendamento.Vago, Clock);

        configuracao.ProfessorId.Should().Be(ProfessorId);
        configuracao.ModeloAgendamento.Should().Be(ModeloAgendamento.Vago);
        configuracao.CreatedAt.Should().Be(Clock.UtcNow);
        configuracao.UpdatedAt.Should().Be(Clock.UtcNow);
    }

    [Fact]
    public void AlterarModelo_ModeloDiferente_TrocaModeloEAtualizaUpdatedAt()
    {
        var configuracao = ConfiguracaoProfessor.Criar(ProfessorId, ModeloAgendamento.Fixo, Clock);
        var relogioDaTroca = new FixedClock(Clock.UtcNow.AddDays(1));

        configuracao.AlterarModelo(ModeloAgendamento.Vago, relogioDaTroca);

        configuracao.ModeloAgendamento.Should().Be(ModeloAgendamento.Vago);
        configuracao.UpdatedAt.Should().Be(relogioDaTroca.UtcNow);
        configuracao.CreatedAt.Should().Be(Clock.UtcNow);
    }

    [Theory]
    [InlineData(ModeloAgendamento.Vago, false, true)]
    [InlineData(ModeloAgendamento.Vago, true, true)]
    [InlineData(ModeloAgendamento.Fixo, false, false)]
    [InlineData(ModeloAgendamento.Fixo, true, false)]
    [InlineData(ModeloAgendamento.Hibrido, false, true)]
    [InlineData(ModeloAgendamento.Hibrido, true, false)]
    public void PermiteMarcacaoLivre_ConformeModeloEAtribuicaoFixa_RetornaEsperado(
        ModeloAgendamento modelo, bool horarioPossuiAtribuicaoFixa, bool esperado)
    {
        var configuracao = ConfiguracaoProfessor.Criar(ProfessorId, modelo, Clock);

        configuracao.PermiteMarcacaoLivre(horarioPossuiAtribuicaoFixa).Should().Be(esperado);
    }
}
