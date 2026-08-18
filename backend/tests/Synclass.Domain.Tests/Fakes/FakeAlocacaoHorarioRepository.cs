using Synclass.Domain.Alocacoes;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Repositório em memória usado nos testes de unidade do Domain, no lugar de
/// um banco real (ver docs/spec/code-style.md#testes). Mesmo padrão de
/// <see cref="FakeHorarioRepository"/>.
/// </summary>
public sealed class FakeAlocacaoHorarioRepository : IAlocacaoHorarioRepository
{
    private readonly List<AlocacaoHorario> _alocacoes = new();

    public IReadOnlyCollection<AlocacaoHorario> Alocacoes => _alocacoes.AsReadOnly();

    public Task<AlocacaoHorario?> BuscarAsync(Guid horarioId, Guid matriculaId, CancellationToken cancellationToken)
    {
        var alocacao = _alocacoes.FirstOrDefault(a => a.HorarioId == horarioId && a.MatriculaId == matriculaId);
        return Task.FromResult(alocacao);
    }

    public Task<int> ContarPorHorarioAsync(Guid horarioId, CancellationToken cancellationToken)
    {
        var quantidade = _alocacoes.Count(a => a.HorarioId == horarioId);
        return Task.FromResult(quantidade);
    }

    public Task<IReadOnlyCollection<AlocacaoHorario>> ListarPorHorarioAsync(Guid horarioId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<AlocacaoHorario> resultado = _alocacoes.Where(a => a.HorarioId == horarioId).ToList();
        return Task.FromResult(resultado);
    }

    public Task<IReadOnlyCollection<AlocacaoHorario>> ListarPorMatriculaAsync(Guid matriculaId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<AlocacaoHorario> resultado = _alocacoes.Where(a => a.MatriculaId == matriculaId).ToList();
        return Task.FromResult(resultado);
    }

    public Task<bool> PossuiAlocacaoOrigemProfessorAsync(Guid horarioId, CancellationToken cancellationToken)
    {
        var possui = _alocacoes.Any(a => a.HorarioId == horarioId && a.OrigemAlocacao == OrigemAlocacao.Professor);
        return Task.FromResult(possui);
    }

    public Task AdicionarAsync(AlocacaoHorario alocacao, CancellationToken cancellationToken)
    {
        _alocacoes.Add(alocacao);
        return Task.CompletedTask;
    }

    public Task RemoverAsync(AlocacaoHorario alocacao, CancellationToken cancellationToken)
    {
        _alocacoes.Remove(alocacao);
        return Task.CompletedTask;
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
