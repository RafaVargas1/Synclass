using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Aulas;

namespace Synclass.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core de <see cref="ICancelamentoAulaRepository"/>.
/// </summary>
public sealed class CancelamentoAulaRepository : ICancelamentoAulaRepository
{
    private readonly SynclassDbContext _dbContext;

    public CancelamentoAulaRepository(SynclassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CancelamentoAula?> BuscarAsync(Guid aulaId, Guid matriculaId, CancellationToken cancellationToken)
    {
        return _dbContext.CancelamentosAula
            .FirstOrDefaultAsync(c => c.AulaId == aulaId && c.MatriculaId == matriculaId, cancellationToken);
    }

    public async Task AdicionarAsync(CancelamentoAula cancelamento, CancellationToken cancellationToken)
    {
        await _dbContext.CancelamentosAula.AddAsync(cancelamento, cancellationToken);
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
