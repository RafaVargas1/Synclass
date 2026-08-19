namespace Synclass.Domain.Aulas;

/// <summary>
/// Abstrai a persistência de <see cref="CancelamentoAula"/>. Implementado em
/// Synclass.Infrastructure (EF Core), mesmo padrão de
/// <c>IAlocacaoHorarioRepository</c>.
/// </summary>
public interface ICancelamentoAulaRepository
{
    /// <summary>
    /// Busca o cancelamento de um Aluno específico para uma aula, se
    /// existir — usado para a idempotência de <c>AulaService.CancelarAsync</c>
    /// e para <c>ListarProximasAsync</c> pular ocorrências já canceladas.
    /// </summary>
    Task<CancelamentoAula?> BuscarAsync(Guid aulaId, Guid matriculaId, CancellationToken cancellationToken);

    Task AdicionarAsync(CancelamentoAula cancelamento, CancellationToken cancellationToken);

    Task SalvarAsync(CancellationToken cancellationToken);
}
