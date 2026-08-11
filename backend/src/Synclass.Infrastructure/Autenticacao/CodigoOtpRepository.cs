using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Autenticacao;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Infrastructure.Autenticacao;

/// <summary>
/// Implementação EF Core de <see cref="ICodigoOtpRepository"/>.
/// </summary>
public sealed class CodigoOtpRepository : ICodigoOtpRepository
{
    private readonly SynclassDbContext _dbContext;

    public CodigoOtpRepository(SynclassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<CodigoOtp?> BuscarMaisRecenteNaoUsadoAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        return _dbContext.CodigosOtp
            .Where(c => c.UsuarioId == usuarioId && c.UsadoEm == null)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AdicionarAsync(CodigoOtp codigo, CancellationToken cancellationToken)
    {
        await _dbContext.CodigosOtp.AddAsync(codigo, cancellationToken);
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
