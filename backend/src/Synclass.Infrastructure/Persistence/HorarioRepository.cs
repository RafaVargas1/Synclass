using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Horarios;

namespace Synclass.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core de <see cref="IHorarioRepository"/>.
/// </summary>
public sealed class HorarioRepository : IHorarioRepository
{
    private readonly SynclassDbContext _dbContext;

    public HorarioRepository(SynclassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Horario>> ListarPorProfessorEDiaAsync(Guid professorId, DiaSemana diaSemana, CancellationToken cancellationToken)
    {
        return await _dbContext.Horarios
            .Where(h => h.ProfessorId == professorId && h.DiaSemana == diaSemana)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Horario>> ListarPorProfessorAsync(Guid professorId, CancellationToken cancellationToken)
    {
        return await _dbContext.Horarios
            .Where(h => h.ProfessorId == professorId)
            .OrderBy(h => h.DiaSemana).ThenBy(h => h.HoraInicio)
            .ToListAsync(cancellationToken);
    }

    public Task<Horario?> BuscarPorIdAsync(Guid horarioId, CancellationToken cancellationToken)
    {
        return _dbContext.Horarios.FirstOrDefaultAsync(h => h.Id == horarioId, cancellationToken);
    }

    /// <summary>
    /// Sempre <c>false</c> até a issue #8 modelar a alocação de Alunos a
    /// horários — não existe tabela para consultar de verdade ainda. Ver
    /// docs/specs/6-horarios-disponiveis/implementation.md#dependência-da-issue-8.
    /// </summary>
    public Task<bool> PossuiAlunosAlocadosAsync(Guid horarioId, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public async Task AdicionarAsync(Horario horario, CancellationToken cancellationToken)
    {
        await _dbContext.Horarios.AddAsync(horario, cancellationToken);
    }

    public Task RemoverAsync(Horario horario, CancellationToken cancellationToken)
    {
        _dbContext.Horarios.Remove(horario);
        return Task.CompletedTask;
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
