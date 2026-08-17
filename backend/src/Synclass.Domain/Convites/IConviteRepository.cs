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

    Task AdicionarAsync(Convite convite, CancellationToken cancellationToken);

    Task SalvarAsync(CancellationToken cancellationToken);
}
