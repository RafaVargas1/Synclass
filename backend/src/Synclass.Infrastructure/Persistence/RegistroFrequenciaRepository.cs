using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Frequencias;

namespace Synclass.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core de <see cref="IRegistroFrequenciaRepository"/>.
/// </summary>
public sealed class RegistroFrequenciaRepository : IRegistroFrequenciaRepository
{
    private readonly SynclassDbContext _dbContext;

    public RegistroFrequenciaRepository(SynclassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RegistroFrequencia?> BuscarAsync(Guid aulaId, Guid matriculaId, CancellationToken cancellationToken)
    {
        return _dbContext.RegistrosFrequencia
            .FirstOrDefaultAsync(r => r.AulaId == aulaId && r.MatriculaId == matriculaId, cancellationToken);
    }

    public async Task AdicionarAsync(RegistroFrequencia registro, CancellationToken cancellationToken)
    {
        await _dbContext.RegistrosFrequencia.AddAsync(registro, cancellationToken);
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
