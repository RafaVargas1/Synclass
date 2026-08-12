namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Abstrai a persistência de <see cref="CodigoOtp"/>. Implementado em
/// Synclass.Infrastructure (EF Core), mesmo padrão de
/// <see cref="Usuarios.IUsuarioRepository"/>.
/// </summary>
public interface ICodigoOtpRepository
{
    /// <summary>
    /// Busca o código não-usado mais recente do usuário. Como cada novo
    /// pedido invalida o anterior (<see cref="CodigoOtp.Invalidar"/>), o
    /// mais recente não-usado é sempre, por construção, o único candidato
    /// válido para confirmação.
    /// </summary>
    Task<CodigoOtp?> BuscarMaisRecenteNaoUsadoAsync(Guid usuarioId, CancellationToken cancellationToken);

    Task AdicionarAsync(CodigoOtp codigo, CancellationToken cancellationToken);

    Task SalvarAsync(CancellationToken cancellationToken);
}
