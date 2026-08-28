using Synclass.Domain.Pagamentos;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Repositório em memória de <see cref="Pagamento"/> usado nos testes de
/// unidade do Domain (issue #199 / #200), no lugar de um banco real (ver
/// docs/spec/code-style.md#testes). Expoe a lista de pagamentos para o
/// teste poder inspecionar o que foi persistido, e o
/// <see cref="Atualizado"/> para afirmar quantas vezes o webhook de #200
/// persistiu uma mudança.
/// </summary>
public sealed class FakePagamentoRepository : IPagamentoRepository
{
    public List<Pagamento> Pagamentos { get; } = new();

    /// <summary>
    /// Counter de quantas vezes <see cref="AtualizarAsync"/> foi chamado —
    /// usado pelos testes de idempotência do webhook de #200 (segunda vez do
    /// mesmo evento não deve re-persistir).
    /// </summary>
    public int Atualizado { get; private set; }

    public Task<Pagamento?> BuscarPendentePorMatriculaEPeriodoAsync(
        Guid matriculaId, DateOnly inicio, DateOnly fimExclusivo, CancellationToken cancellationToken)
    {
        var pagamento = Pagamentos.FirstOrDefault(
            p => p.MatriculaId == matriculaId
                 && p.PeriodoInicio == inicio
                 && p.PeriodoFimExclusivo == fimExclusivo
                 && p.Status == StatusPagamento.Pendente);
        return Task.FromResult(pagamento);
    }

    public Task<Pagamento?> BuscarConfirmadoPorMatriculaEPeriodoAsync(
        Guid matriculaId, DateOnly inicio, DateOnly fimExclusivo, CancellationToken cancellationToken)
    {
        var pagamento = Pagamentos.FirstOrDefault(
            p => p.MatriculaId == matriculaId
                 && p.PeriodoInicio == inicio
                 && p.PeriodoFimExclusivo == fimExclusivo
                 && p.Status == StatusPagamento.Confirmado);
        return Task.FromResult(pagamento);
    }

    public Task<Pagamento?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var pagamento = Pagamentos.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(pagamento);
    }

    public Task AdicionarAsync(Pagamento pagamento, CancellationToken cancellationToken)
    {
        Pagamentos.Add(pagamento);
        return Task.CompletedTask;
    }

    public Task AtualizarAsync(Pagamento pagamento, CancellationToken cancellationToken)
    {
        Atualizado++;
        return Task.CompletedTask;
    }

    public Task<List<Pagamento>> ListarPorMatriculaAsync(Guid matriculaId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Pagamentos.Where(p => p.MatriculaId == matriculaId).ToList());
    }
}
