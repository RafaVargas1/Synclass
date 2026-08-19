using FluentAssertions;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Configuracoes;

namespace Synclass.Domain.Tests.Configuracoes;

/// <summary>
/// Cobre o único caso de uso de escrita do card (issue #7):
/// <see cref="ConfiguracaoProfessorService.DefinirModeloAsync"/>, que cria a
/// configuração na 1ª chamada e altera nas seguintes — operação idempotente
/// e não versionada (ver implementation.md#edge-points).
/// </summary>
public sealed class ConfiguracaoProfessorServiceTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 13, 12, 0, 0, TimeSpan.Zero));
    private static readonly Guid ProfessorId = Guid.NewGuid();

    [Fact]
    public async Task DefinirModeloAsync_SemConfiguracaoExistente_CriaConfiguracao()
    {
        var repositorio = new FakeConfiguracaoProfessorRepository();
        var servico = new ConfiguracaoProfessorService(repositorio, Clock);

        var configuracao = await servico.DefinirModeloAsync(ProfessorId, ModeloAgendamento.Vago, CancellationToken.None);

        configuracao.ProfessorId.Should().Be(ProfessorId);
        configuracao.ModeloAgendamento.Should().Be(ModeloAgendamento.Vago);
        repositorio.Configuracoes.Should().ContainSingle(c => c.ProfessorId == ProfessorId);
    }

    [Fact]
    public async Task DefinirModeloAsync_ComConfiguracaoExistente_AlteraModeloPreservandoProfessorIdECreatedAt()
    {
        var repositorio = new FakeConfiguracaoProfessorRepository();
        var servico = new ConfiguracaoProfessorService(repositorio, Clock);
        var configuracaoOriginal = await servico.DefinirModeloAsync(ProfessorId, ModeloAgendamento.Fixo, CancellationToken.None);
        var relogioDaTroca = new FixedClock(Clock.UtcNow.AddDays(1));
        var servicoDaTroca = new ConfiguracaoProfessorService(repositorio, relogioDaTroca);

        var configuracaoAlterada = await servicoDaTroca.DefinirModeloAsync(ProfessorId, ModeloAgendamento.Vago, CancellationToken.None);

        configuracaoAlterada.Id.Should().Be(configuracaoOriginal.Id);
        configuracaoAlterada.ProfessorId.Should().Be(ProfessorId);
        configuracaoAlterada.ModeloAgendamento.Should().Be(ModeloAgendamento.Vago);
        configuracaoAlterada.CreatedAt.Should().Be(configuracaoOriginal.CreatedAt);
        configuracaoAlterada.UpdatedAt.Should().Be(relogioDaTroca.UtcNow);
        repositorio.Configuracoes.Should().ContainSingle();
    }

    [Fact]
    public async Task DefinirPrazoCancelamentoAsync_ComConfiguracaoExistente_AlteraPrazoPreservandoModelo()
    {
        var repositorio = new FakeConfiguracaoProfessorRepository();
        var servico = new ConfiguracaoProfessorService(repositorio, Clock);
        await servico.DefinirModeloAsync(ProfessorId, ModeloAgendamento.Hibrido, CancellationToken.None);
        var relogioDaTroca = new FixedClock(Clock.UtcNow.AddDays(1));
        var servicoDaTroca = new ConfiguracaoProfessorService(repositorio, relogioDaTroca);

        var configuracaoAlterada = await servicoDaTroca.DefinirPrazoCancelamentoAsync(
            ProfessorId, 48 * 60, CancellationToken.None);

        configuracaoAlterada.Should().NotBeNull();
        configuracaoAlterada!.PrazoCancelamentoMinutos.Should().Be(48 * 60);
        configuracaoAlterada.ModeloAgendamento.Should().Be(ModeloAgendamento.Hibrido);
        configuracaoAlterada.UpdatedAt.Should().Be(relogioDaTroca.UtcNow);
        repositorio.Configuracoes.Should().ContainSingle();
    }

    [Fact]
    public async Task DefinirPrazoCancelamentoAsync_SemConfiguracaoExistente_RetornaNull()
    {
        var repositorio = new FakeConfiguracaoProfessorRepository();
        var servico = new ConfiguracaoProfessorService(repositorio, Clock);

        var resultado = await servico.DefinirPrazoCancelamentoAsync(ProfessorId, 60, CancellationToken.None);

        resultado.Should().BeNull();
        repositorio.Configuracoes.Should().BeEmpty();
    }
}
