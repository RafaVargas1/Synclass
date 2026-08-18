using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Alocacoes;

namespace Synclass.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core de <see cref="IAlocacaoHorarioRepository"/>.
/// </summary>
public sealed class AlocacaoHorarioRepository : IAlocacaoHorarioRepository
{
    private readonly SynclassDbContext _dbContext;

    public AlocacaoHorarioRepository(SynclassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AlocacaoHorario?> BuscarAsync(Guid horarioId, Guid matriculaId, CancellationToken cancellationToken)
    {
        return _dbContext.AlocacoesHorario
            .FirstOrDefaultAsync(a => a.HorarioId == horarioId && a.MatriculaId == matriculaId, cancellationToken);
    }

    public Task<int> ContarPorHorarioAsync(Guid horarioId, CancellationToken cancellationToken)
    {
        return _dbContext.AlocacoesHorario.CountAsync(a => a.HorarioId == horarioId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<AlocacaoHorario>> ListarPorHorarioAsync(Guid horarioId, CancellationToken cancellationToken)
    {
        return await _dbContext.AlocacoesHorario
            .Where(a => a.HorarioId == horarioId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AdicionarAsync(AlocacaoHorario alocacao, CancellationToken cancellationToken)
    {
        await _dbContext.AlocacoesHorario.AddAsync(alocacao, cancellationToken);
    }

    public Task RemoverAsync(AlocacaoHorario alocacao, CancellationToken cancellationToken)
    {
        _dbContext.AlocacoesHorario.Remove(alocacao);
        return Task.CompletedTask;
    }

    public async Task SalvarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Índice único (HorarioId, MatriculaId) violado: a checagem
            // prévia de AlocacaoHorarioService já passou, então só a corrida
            // concorrente explica a violação aqui — mesmo padrão de
            // MatriculaRepository.SalvarAsync/MatriculaConcorrenteException.
            throw new AlocacaoJaExisteException(ex);
        }
    }
}
