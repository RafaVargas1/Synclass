namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Abstrai a persistência de <see cref="ConexaoMercadoPago"/> (issue #203).
/// Implementado em Synclass.Infrastructure (EF Core), permitindo que o
/// Domain e seus testes de unidade não dependam de banco de dados. Cada
/// método que muta já persiste a mudança (o repositório não tem um
/// <c>SalvarAsync</c> separado — ver implementation.md).
/// </summary>
public interface IConexaoMercadoPagoRepository
{
    /// <summary>
    /// Busca a conexão de um Professor pelo id dele — a porta de entrada do
    /// upsert de <c>ConectarAsync</c> (idempotência por Professor, issue
    /// #203). Retorna <see langword="null"/> quando o Professor nunca
    /// iniciou um fluxo.
    /// </summary>
    Task<ConexaoMercadoPago?> ObterPorProfessorAsync(Guid professorId, CancellationToken cancellationToken);

    /// <summary>
    /// Busca o registro pelo <see cref="ConexaoMercadoPago.State"/> pendente
    /// — como o callback OAuth é anônimo (redirect do navegador do
    /// Professor, sem sessão do Synclass), o Professor é resolvido pelo
    /// <c>state</c> (issue #203).
    /// </summary>
    Task<ConexaoMercadoPago?> ObterPorStateAsync(string state, CancellationToken cancellationToken);

    Task AdicionarAsync(ConexaoMercadoPago conexao, CancellationToken cancellationToken);

    Task AtualizarAsync(ConexaoMercadoPago conexao, CancellationToken cancellationToken);

    /// <summary>
    /// Remove a conexão (issue #203): usado quando a renovação via
    /// <c>refresh_token</c> falha (token revogado, irrecuperável) — manter o
    /// registro só acumularia lixo e confundiria o status "conectado".
    /// </summary>
    Task RemoverAsync(ConexaoMercadoPago conexao, CancellationToken cancellationToken);
}
