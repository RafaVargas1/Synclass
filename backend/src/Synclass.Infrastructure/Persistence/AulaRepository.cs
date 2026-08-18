using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Aulas;

namespace Synclass.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core de <see cref="IAulaRepository"/>.
/// </summary>
public sealed class AulaRepository : IAulaRepository
{
    private readonly SynclassDbContext _dbContext;

    public AulaRepository(SynclassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Aula?> BuscarPorHorarioEDataAsync(Guid horarioId, DateOnly data, CancellationToken cancellationToken)
    {
        return _dbContext.Aulas.FirstOrDefaultAsync(a => a.HorarioId == horarioId && a.Data == data, cancellationToken);
    }

    public async Task AdicionarAsync(Aula aula, CancellationToken cancellationToken)
    {
        await _dbContext.Aulas.AddAsync(aula, cancellationToken);
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
