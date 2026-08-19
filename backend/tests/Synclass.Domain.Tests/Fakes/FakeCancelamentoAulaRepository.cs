using Synclass.Domain.Aulas;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Repositório em memória usado nos testes de unidade do Domain, no lugar de
/// um banco real (ver docs/spec/code-style.md#testes). Mesmo padrão de
/// <see cref="FakeAulaRepository"/>.
/// </summary>
public sealed class FakeCancelamentoAulaRepository : ICancelamentoAulaRepository
{
    private readonly List<CancelamentoAula> _cancelamentos = new();

    public IReadOnlyCollection<CancelamentoAula> Cancelamentos => _cancelamentos.AsReadOnly();

    public Task<CancelamentoAula?> BuscarAsync(Guid aulaId, Guid matriculaId, CancellationToken cancellationToken)
    {
        var cancelamento = _cancelamentos.FirstOrDefault(c => c.AulaId == aulaId && c.MatriculaId == matriculaId);
        return Task.FromResult(cancelamento);
    }

    public Task AdicionarAsync(CancelamentoAula cancelamento, CancellationToken cancellationToken)
    {
        _cancelamentos.Add(cancelamento);
        return Task.CompletedTask;
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
