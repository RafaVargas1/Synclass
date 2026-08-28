using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Pagamentos;

namespace Synclass.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core de <see cref="IPagamentoRepository"/> (issue #199).
/// Segue o padrão de <c>ConexaoMercadoPagoRepository</c>: injeção do
/// <see cref="SynclassDbContext"/> via construtor e cada método que muta já
/// persiste a mudança (sem <c>SalvarAsync</c> separado — ver
/// implementation.md). A unicidade do pendente por (MatriculaId, período) é
/// garantida pela lógica de domínio em <c>PagamentoService.IniciarAsync</c>,
/// não por constraint de banco (ver implementation.md#migration) — então a
/// busca aqui devolve o primeiro pendente que casar na janela exata.
/// <see cref="ObterPorIdAsync"/> e <see cref="AtualizarAsync"/> servem ao
/// webhook de #200 (confirmação/estorno por <c>external_reference</c>).
/// </summary>
public sealed class PagamentoRepository : IPagamentoRepository
{
    private readonly SynclassDbContext _dbContext;

    public PagamentoRepository(SynclassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Pagamento?> BuscarPendentePorMatriculaEPeriodoAsync(
        Guid matriculaId, DateOnly inicio, DateOnly fimExclusivo, CancellationToken cancellationToken)
    {
        return _dbContext.Pagamentos.FirstOrDefaultAsync(
            p => p.MatriculaId == matriculaId
                 && p.PeriodoInicio == inicio
                 && p.PeriodoFimExclusivo == fimExclusivo
                 && p.Status == StatusPagamento.Pendente,
            cancellationToken);
    }

    public Task<Pagamento?> BuscarConfirmadoPorMatriculaEPeriodoAsync(
        Guid matriculaId, DateOnly inicio, DateOnly fimExclusivo, CancellationToken cancellationToken)
    {
        return _dbContext.Pagamentos.FirstOrDefaultAsync(
            p => p.MatriculaId == matriculaId
                 && p.PeriodoInicio == inicio
                 && p.PeriodoFimExclusivo == fimExclusivo
                 && p.Status == StatusPagamento.Confirmado,
            cancellationToken);
    }

    public Task<Pagamento?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.Pagamentos.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task AdicionarAsync(Pagamento pagamento, CancellationToken cancellationToken)
    {
        await _dbContext.Pagamentos.AddAsync(pagamento, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AtualizarAsync(Pagamento pagamento, CancellationToken cancellationToken)
    {
        _dbContext.Pagamentos.Update(pagamento);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<List<Pagamento>> ListarPorMatriculaAsync(Guid matriculaId, CancellationToken cancellationToken)
    {
        return _dbContext.Pagamentos
            .Where(p => p.MatriculaId == matriculaId)
            .OrderBy(p => p.CriadoEm)
            .ToListAsync(cancellationToken);
    }
}
