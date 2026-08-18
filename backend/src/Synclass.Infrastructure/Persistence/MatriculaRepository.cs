using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Matriculas;

namespace Synclass.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core de <see cref="IMatriculaRepository"/>.
/// </summary>
public sealed class MatriculaRepository : IMatriculaRepository
{
    private readonly SynclassDbContext _dbContext;

    public MatriculaRepository(SynclassDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Matricula?> BuscarPorIdentificadorAsync(
        Guid professorId, string identificadorProvisorio, CancellationToken cancellationToken)
    {
        return _dbContext.Matriculas
            .FirstOrDefaultAsync(
                m => m.ProfessorId == professorId && m.IdentificadorProvisorio == identificadorProvisorio,
                cancellationToken);
    }

    public Task<Matricula?> BuscarPorIdAsync(Guid matriculaId, CancellationToken cancellationToken)
    {
        return _dbContext.Matriculas.FirstOrDefaultAsync(m => m.Id == matriculaId, cancellationToken);
    }

    public Task<Matricula?> BuscarVinculoAsync(Guid professorId, Guid alunoUsuarioId, CancellationToken cancellationToken)
    {
        return _dbContext.Matriculas
            .FirstOrDefaultAsync(m => m.ProfessorId == professorId && m.AlunoUsuarioId == alunoUsuarioId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Matricula>> ListarPorProfessorAsync(Guid professorId, CancellationToken cancellationToken)
    {
        return await _dbContext.Matriculas
            .Where(m => m.ProfessorId == professorId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Matricula>> ListarPorAlunoAsync(Guid alunoUsuarioId, CancellationToken cancellationToken)
    {
        return await _dbContext.Matriculas
            .Where(m => m.AlunoUsuarioId == alunoUsuarioId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AdicionarAsync(Matricula matricula, CancellationToken cancellationToken)
    {
        await _dbContext.Matriculas.AddAsync(matricula, cancellationToken);
    }

    public async Task SalvarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Índice único (ProfessorId, IdentificadorProvisorio) violado:
            // duas requisições concorrentes passaram pela checagem de
            // duplicidade da aplicação antes de qualquer uma confirmar a
            // escrita — mesmo padrão de UsuarioRepository.SalvarAsync.
            throw new MatriculaConcorrenteException(ex);
        }
    }
}
