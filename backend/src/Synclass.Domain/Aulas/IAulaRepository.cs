namespace Synclass.Domain.Aulas;

/// <summary>
/// Abstrai a persistência de <see cref="Aula"/>. Implementado em
/// Synclass.Infrastructure (EF Core), mesmo padrão de
/// <c>IAlocacaoHorarioRepository</c>.
/// </summary>
public interface IAulaRepository
{
    /// <summary>
    /// Busca a ocorrência de um horário em uma data específica — usado por
    /// <c>AulaService</c> para decidir se instancia sob demanda (issue #10).
    /// </summary>
    Task<Aula?> BuscarPorHorarioEDataAsync(Guid horarioId, DateOnly data, CancellationToken cancellationToken);

    Task AdicionarAsync(Aula aula, CancellationToken cancellationToken);

    Task SalvarAsync(CancellationToken cancellationToken);
}
