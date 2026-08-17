namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Abstrai a persistência de <see cref="RegraDeCobranca"/>. Implementado em
/// Synclass.Infrastructure (EF Core), permitindo que o Domain e seus testes
/// de unidade não dependam de banco de dados.
/// </summary>
public interface IRegraDeCobrancaRepository
{
    Task<RegraDeCobranca?> BuscarPorMatriculaAsync(Guid matriculaId, CancellationToken cancellationToken);

    /// <summary>
    /// Upsert: substitui a regra existente da mesma <c>MatriculaId</c>, nunca
    /// duas linhas (FK única em <c>MatriculaId</c>) — não há histórico
    /// versionado nesta Task (ver implementation.md#edge-points).
    /// </summary>
    Task SalvarAsync(RegraDeCobranca regra, CancellationToken cancellationToken);
}
