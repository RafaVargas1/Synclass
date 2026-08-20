namespace Synclass.Domain.Convites;

/// <summary>
/// Abstrai a persistência de <see cref="Convite"/>. Implementado em
/// Synclass.Infrastructure (EF Core), permitindo que o Domain e seus testes
/// de unidade não dependam de banco de dados.
/// </summary>
public interface IConviteRepository
{
    /// <summary>
    /// Busca o convite pelo token do link de aceite — único caminho de
    /// leitura usado por <c>ConviteService.AceitarAsync</c>.
    /// </summary>
    Task<Convite?> BuscarPorTokenAsync(string token, CancellationToken cancellationToken);

    /// <summary>
    /// Busca o convite mais recente (por <see cref="Convite.CreatedAt"/>)
    /// entre os que têm <paramref name="codigo"/> — único caminho de leitura
    /// usado por <c>ConviteService.AceitarPorCodigoAsync</c> (issue #63). O
    /// código só é único entre convites ativos (issue #62), então um convite
    /// finalizado antigo pode ter o mesmo código de um convite ativo mais
    /// novo; devolver o mais recente evita que o antigo "esconda" o ativo
    /// (ver edge points de docs/specs/63-entrar-turma-codigo/implementation.md).
    /// </summary>
    Task<Convite?> BuscarPorCodigoAsync(string codigo, CancellationToken cancellationToken);

    /// <summary>
    /// Indica se <paramref name="codigo"/> já pertence a um convite ativo
    /// (não usado e não expirado em <paramref name="agora"/>) — usado por
    /// <c>ConviteService.GerarAsync</c> para checar unicidade só entre
    /// convites ativos (issue #62): um código de convite já usado ou
    /// expirado pode ser reaproveitado.
    /// </summary>
    Task<bool> ExisteCodigoAtivoAsync(string codigo, DateTimeOffset agora, CancellationToken cancellationToken);

    Task AdicionarAsync(Convite convite, CancellationToken cancellationToken);

    Task SalvarAsync(CancellationToken cancellationToken);
}
