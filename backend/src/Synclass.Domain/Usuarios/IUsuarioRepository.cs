namespace Synclass.Domain.Usuarios;

/// <summary>
/// Abstrai a persistência de <see cref="Usuario"/>. Implementado em
/// Synclass.Infrastructure (EF Core), permitindo que o Domain e seus testes
/// de unidade não dependam de banco de dados.
/// </summary>
public interface IUsuarioRepository
{
    Task<Usuario?> BuscarPorContatoAsync(string contatoNormalizado, CancellationToken cancellationToken);

    Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken);

    Task SalvarAsync(CancellationToken cancellationToken);
}
