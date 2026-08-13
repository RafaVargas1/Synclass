using Synclass.Domain.Configuracoes;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Repositório em memória usado nos testes de unidade do Domain, no lugar de
/// um banco real (ver docs/spec/code-style.md#testes). Mesmo padrão de
/// <see cref="FakeHorarioRepository"/>.
/// </summary>
public sealed class FakeConfiguracaoProfessorRepository : IConfiguracaoProfessorRepository
{
    public List<ConfiguracaoProfessor> Configuracoes { get; } = new();

    public Task<ConfiguracaoProfessor?> BuscarPorProfessorAsync(Guid professorId, CancellationToken cancellationToken)
    {
        var configuracao = Configuracoes.FirstOrDefault(c => c.ProfessorId == professorId);
        return Task.FromResult(configuracao);
    }

    public Task AdicionarAsync(ConfiguracaoProfessor configuracao, CancellationToken cancellationToken)
    {
        Configuracoes.Add(configuracao);
        return Task.CompletedTask;
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
