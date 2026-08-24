namespace Synclass.Domain.CodigosEntradaTurma;

/// <summary>
/// Abstrai a persistência de <see cref="CodigoEntradaTurma"/>. Implementado
/// em Synclass.Infrastructure (EF Core), permitindo que o Domain e seus
/// testes de unidade não dependam de banco de dados.
/// </summary>
public interface ICodigoEntradaTurmaRepository
{
    /// <summary>
    /// Indica se <paramref name="codigo"/> já pertence a um código ativo
    /// (não expirado em <paramref name="agora"/>) — usado por
    /// <c>CodigoEntradaTurmaService.GerarAsync</c> para checar unicidade só
    /// entre códigos ativos, mesmo racional de <c>Convite.Codigo</c>.
    /// </summary>
    Task<bool> ExisteCodigoAtivoAsync(string codigo, DateTimeOffset agora, CancellationToken cancellationToken);

    /// <summary>
    /// Busca o código ativo (não expirado em <paramref name="agora"/>) mais
    /// recente com <paramref name="codigo"/> — devolve <c>null</c> se não
    /// existir ou já tiver expirado, tratado pelo service como código
    /// inválido.
    /// </summary>
    Task<CodigoEntradaTurma?> BuscarAtivoPorCodigoAsync(string codigo, DateTimeOffset agora, CancellationToken cancellationToken);

    Task AdicionarAsync(CodigoEntradaTurma codigoEntradaTurma, CancellationToken cancellationToken);

    Task SalvarAsync(CancellationToken cancellationToken);
}
