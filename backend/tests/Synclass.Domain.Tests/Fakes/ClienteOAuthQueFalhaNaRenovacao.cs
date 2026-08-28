using Synclass.Domain.Pagamentos;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Fake de <see cref="IClienteOAuthMercadoPago"/> que falha na renovação de
/// token (issue #203) — simula o refresh_token revogado ou a API do Mercado
/// Pago recusando a renovação. Usado para cobrir o contrato com a Task #199:
/// quando a renovação falha, o service remove o registro e retorna
/// <see langword="null"/> em
/// <see cref="ConexaoMercadoPagoService.ObterCollectorIdAsync"/>.
/// </summary>
public sealed class ClienteOAuthQueFalhaNaRenovacao : IClienteOAuthMercadoPago
{
    public string? UltimoRefreshToken { get; private set; }

    public string MontarUrlAutorizacao(string state, string redirectUri)
    {
        return $"https://auth.mercadopago.com.br/authorization?state={state}&redirect_uri={redirectUri}";
    }

    public Task<TrocaCodePorTokenResultado> TrocarCodePorTokenAsync(string code, string redirectUri, CancellationToken ct)
    {
        throw new NotSupportedException("Este fake só falha na renovação.");
    }

    public Task<TrocaCodePorTokenResultado> RenovarTokenAsync(string refreshToken, CancellationToken ct)
    {
        UltimoRefreshToken = refreshToken;
        throw new InvalidOperationException(
            $"Falha na renovação do token: {refreshToken}. Esperado: refresh_token válido não revogado.");
    }
}
