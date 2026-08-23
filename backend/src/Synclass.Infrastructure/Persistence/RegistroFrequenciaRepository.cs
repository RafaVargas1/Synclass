using Microsoft.EntityFrameworkCore;
using Synclass.Domain.Cobrancas;
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

    /// <summary>
    /// N+1 evitado via join direto entre <c>RegistrosFrequencia</c> e
    /// <c>Aulas</c> (mesma tabela já mapeada) — sem carregar as entidades
    /// inteiras, só a contagem.
    /// </summary>
    public Task<int> ContarPresencasNoPeriodoAsync(Guid matriculaId, PeriodoConsulta periodo, CancellationToken cancellationToken)
    {
        return (
            from registro in _dbContext.RegistrosFrequencia
            join aula in _dbContext.Aulas on registro.AulaId equals aula.Id
            where registro.MatriculaId == matriculaId
                && registro.StatusProfessor == StatusFrequencia.Presente
                && aula.Data >= periodo.Inicio
                && aula.Data < periodo.FimExclusivo
            select registro.Id
        ).CountAsync(cancellationToken);
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
