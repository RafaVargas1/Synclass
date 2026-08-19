using Synclass.Domain.Aulas;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Repositório em memória usado nos testes de unidade do Domain, no lugar de
/// um banco real (ver docs/spec/code-style.md#testes). Mesmo padrão de
/// <see cref="FakeAlocacaoHorarioRepository"/>.
/// </summary>
public sealed class FakeAulaRepository : IAulaRepository
{
    private readonly List<Aula> _aulas = new();

    public IReadOnlyCollection<Aula> Aulas => _aulas.AsReadOnly();

    public Task<Aula?> BuscarPorHorarioEDataAsync(Guid horarioId, DateOnly data, CancellationToken cancellationToken)
    {
        var aula = _aulas.FirstOrDefault(a => a.HorarioId == horarioId && a.Data == data);
        return Task.FromResult(aula);
    }

    public Task AdicionarAsync(Aula aula, CancellationToken cancellationToken)
    {
        _aulas.Add(aula);
        return Task.CompletedTask;
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
