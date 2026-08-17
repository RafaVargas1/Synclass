using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Cobrancas;

namespace Synclass.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core de <see cref="IRegraDeCobrancaRepository"/>.
/// <see cref="SalvarAsync"/> resolve o upsert removendo a linha anterior da
/// mesma matrícula (se existir) antes de adicionar a nova — mesma
/// transação implícita do <c>SaveChangesAsync</c>, sem histórico versionado
/// (ver implementation.md#edge-points).
/// </summary>
public sealed class RegraDeCobrancaRepository : IRegraDeCobrancaRepository
{
    private readonly SynclassDbContext _dbContext;

    public RegraDeCobrancaRepository(SynclassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<RegraDeCobranca?> BuscarPorMatriculaAsync(Guid matriculaId, CancellationToken cancellationToken)
    {
        return _dbContext.RegrasDeCobranca.FirstOrDefaultAsync(r => r.MatriculaId == matriculaId, cancellationToken);
    }

    public async Task SalvarAsync(RegraDeCobranca regra, CancellationToken cancellationToken)
    {
        var regraAnterior = await BuscarPorMatriculaAsync(regra.MatriculaId, cancellationToken);
        if (regraAnterior is not null)
        {
            _dbContext.RegrasDeCobranca.Remove(regraAnterior);
        }

        await _dbContext.RegrasDeCobranca.AddAsync(regra, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
