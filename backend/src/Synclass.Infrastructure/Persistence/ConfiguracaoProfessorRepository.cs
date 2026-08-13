using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Configuracoes;

namespace Synclass.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core de <see cref="IConfiguracaoProfessorRepository"/>.
/// </summary>
public sealed class ConfiguracaoProfessorRepository : IConfiguracaoProfessorRepository
{
    private readonly SynclassDbContext _dbContext;

    public ConfiguracaoProfessorRepository(SynclassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ConfiguracaoProfessor?> BuscarPorProfessorAsync(Guid professorId, CancellationToken cancellationToken)
    {
        return _dbContext.ConfiguracoesProfessor.FirstOrDefaultAsync(c => c.ProfessorId == professorId, cancellationToken);
    }

    public async Task AdicionarAsync(ConfiguracaoProfessor configuracao, CancellationToken cancellationToken)
    {
        await _dbContext.ConfiguracoesProfessor.AddAsync(configuracao, cancellationToken);
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
