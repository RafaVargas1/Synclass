using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Alocacoes;
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
    /// Consulta real em <c>AlocacoesHorario</c> (issue #8) — antes disso era
    /// um stub sempre-<c>false</c> (issue #6), já que a tabela não existia.
    /// Ver docs/specs/8-aluno-horario/implementation.md#dependência-da-issue-6-agora-resolvida.
    /// </summary>
    public Task<bool> PossuiAlunosAlocadosAsync(Guid horarioId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<AlocacaoHorario>().AnyAsync(a => a.HorarioId == horarioId, cancellationToken);
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
