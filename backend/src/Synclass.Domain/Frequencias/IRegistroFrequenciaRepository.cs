namespace Synclass.Domain.Frequencias;

/// <summary>
/// Abstrai a persistência de <see cref="RegistroFrequencia"/>. Implementado
/// em Synclass.Infrastructure (EF Core), mesmo padrão de
/// <c>ICancelamentoAulaRepository</c>.
/// </summary>
public interface IRegistroFrequenciaRepository
{
    /// <summary>
    /// Busca a linha de um Aluno específico para uma aula, se existir —
    /// usada pelo upsert de <c>FrequenciaService.RegistrarAsync</c> (cria a
    /// linha na primeira vez, atualiza nas seguintes).
    /// </summary>
    Task<RegistroFrequencia?> BuscarAsync(Guid aulaId, Guid matriculaId, CancellationToken cancellationToken);

    Task AdicionarAsync(RegistroFrequencia registro, CancellationToken cancellationToken);

    Task SalvarAsync(CancellationToken cancellationToken);
}
