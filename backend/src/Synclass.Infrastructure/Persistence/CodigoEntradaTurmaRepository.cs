using Microsoft.EntityFrameworkCore;
using Synclass.Domain.CodigosEntradaTurma;

namespace Synclass.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core de <see cref="ICodigoEntradaTurmaRepository"/>.
/// </summary>
public sealed class CodigoEntradaTurmaRepository : ICodigoEntradaTurmaRepository
{
    private readonly SynclassDbContext _dbContext;

    public CodigoEntradaTurmaRepository(SynclassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExisteCodigoAtivoAsync(string codigo, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        return _dbContext.CodigosEntradaTurma.AnyAsync(c => c.Codigo == codigo && c.ExpiraEm > agora, cancellationToken);
    }

    public Task<CodigoEntradaTurma?> BuscarAtivoPorCodigoAsync(string codigo, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        return _dbContext.CodigosEntradaTurma
            .Where(c => c.Codigo == codigo && c.ExpiraEm > agora)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AdicionarAsync(CodigoEntradaTurma codigoEntradaTurma, CancellationToken cancellationToken)
    {
        await _dbContext.CodigosEntradaTurma.AddAsync(codigoEntradaTurma, cancellationToken);
    }

    public async Task SalvarAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
