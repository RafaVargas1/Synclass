using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Cobrancas;

namespace Synclass.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core de <see cref="IRegraDeCobrancaRepository"/>.
/// <see cref="SalvarAsync"/> resolve o upsert removendo a linha anterior da
/// mesma matrícula (se existir) antes de adicionar a nova — mesma
/// transação implícita do <c>SaveChangesAsync</c>, sem histórico versionado
/// (ver implementation.md#edge-points). Duas chamadas quase simultâneas
/// para a mesma matrícula podem ambas ler "sem regra anterior" antes de
/// qualquer uma commitar — o índice único em <c>MatriculaId</c>
/// (<see cref="RegraDeCobrancaConfiguration"/>) rejeita a segunda inserção
/// a nível de banco; <see cref="SalvarAsync"/> traduz esse
/// <see cref="DbUpdateException"/> para
/// <see cref="RegraDeCobrancaConflitanteException"/> em vez de deixá-lo
/// vazar como 500 não tratado (achado de dev-review, rodada 2 do PR #31).
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
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            throw new RegraDeCobrancaConflitanteException(regra.MatriculaId, ex);
        }
    }
}
