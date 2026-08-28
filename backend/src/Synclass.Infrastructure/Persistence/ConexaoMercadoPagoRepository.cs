using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Pagamentos;

namespace Synclass.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core de <see cref="IConexaoMercadoPagoRepository"/>
/// (issue #203). Segue o padrão de <c>ConfiguracaoProfessorRepository</c>:
/// injeção do <see cref="SynclassDbContext"/> via construtor e cada método
/// que muta já persiste a mudança (sem <c>SalvarAsync</c> separado — ver
/// implementation.md). As credenciais são criptografadas em repouso pelo
/// mapeamento da entidade (<c>ConexaoMercadoPagoConfiguration</c>), não
/// aqui no repositório.
/// </summary>
public sealed class ConexaoMercadoPagoRepository : IConexaoMercadoPagoRepository
{
    private readonly SynclassDbContext _dbContext;

    public ConexaoMercadoPagoRepository(SynclassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ConexaoMercadoPago?> ObterPorProfessorAsync(Guid professorId, CancellationToken cancellationToken)
    {
        return _dbContext.ConexoesMercadoPago.FirstOrDefaultAsync(
            c => c.ProfessorId == professorId, cancellationToken);
    }

    public Task<ConexaoMercadoPago?> ObterPorStateAsync(string state, CancellationToken cancellationToken)
    {
        return _dbContext.ConexoesMercadoPago.FirstOrDefaultAsync(
            c => c.State == state, cancellationToken);
    }

    public async Task AdicionarAsync(ConexaoMercadoPago conexao, CancellationToken cancellationToken)
    {
        await _dbContext.ConexoesMercadoPago.AddAsync(conexao, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AtualizarAsync(ConexaoMercadoPago conexao, CancellationToken cancellationToken)
    {
        _dbContext.ConexoesMercadoPago.Update(conexao);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoverAsync(ConexaoMercadoPago conexao, CancellationToken cancellationToken)
    {
        _dbContext.ConexoesMercadoPago.Remove(conexao);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
