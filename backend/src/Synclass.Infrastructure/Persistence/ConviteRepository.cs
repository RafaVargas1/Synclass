using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Convites;

namespace Synclass.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core de <see cref="IConviteRepository"/>.
/// </summary>
public sealed class ConviteRepository : IConviteRepository
{
    private readonly SynclassDbContext _dbContext;

    public ConviteRepository(SynclassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Convite?> BuscarPorTokenAsync(string token, CancellationToken cancellationToken)
    {
        return _dbContext.Convites.FirstOrDefaultAsync(c => c.Token == token, cancellationToken);
    }

    public async Task AdicionarAsync(Convite convite, CancellationToken cancellationToken)
    {
        await _dbContext.Convites.AddAsync(convite, cancellationToken);
    }

    public async Task SalvarAsync(CancellationToken cancellationToken)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
