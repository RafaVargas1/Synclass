namespace Synclass.Domain.Usuarios;

/// <summary>
/// Abstrai a persistência de <see cref="Usuario"/>. Implementado em
/// Synclass.Infrastructure (EF Core), permitindo que o Domain e seus testes
/// de unidade não dependam de banco de dados.
/// </summary>
public interface IUsuarioRepository
{
    Task<Usuario?> BuscarPorContatoAsync(string contatoNormalizado, CancellationToken cancellationToken);

    /// <summary>
    /// Confirma se existe um <see cref="Usuario"/> com o <paramref name="id"/>
    /// informado, sem carregar a entidade inteira — usado para validar
    /// referências recebidas de fora do Domain (ex: <c>professorId</c> de rota,
    /// issue #3) antes de persistir algo que depende delas.
    /// </summary>
    Task<bool> ExisteAsync(Guid id, CancellationToken cancellationToken);

    Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken);

    Task SalvarAsync(CancellationToken cancellationToken);
}
