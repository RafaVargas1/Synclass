using Synclass.Domain.Pagamentos;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Fake de <see cref="IClienteOAuthMercadoPago"/> que lança
/// <see cref="OperationCanceledException"/> vinculada ao <c>ct</c> recebido
/// (issue #203, achado de dev-review do PR #207) — simula um cancelamento
/// genuíno do chamador (ex: cliente HTTP desconectou) durante a renovação,
/// diferente de uma falha real do Mercado Pago (refresh_token revogado). O
/// contrato de <see cref="ConexaoMercadoPagoService.ObterCollectorIdAsync"/>
/// exige que esse caso propague o cancelamento, sem apagar o registro do
/// Professor (só falhas de fato tratam a conexão como irrecuperável).
/// </summary>
public sealed class ClienteOAuthQueCancelaNaRenovacao : IClienteOAuthMercadoPago
{
    public string MontarUrlAutorizacao(string state, string redirectUri)
    {
        return $"https://auth.mercadopago.com.br/authorization?state={state}&redirect_uri={redirectUri}";
    }

    public Task<TrocaCodePorTokenResultado> TrocarCodePorTokenAsync(string code, string redirectUri, CancellationToken ct)
    {
        throw new NotSupportedException("Este fake só cancela na renovação.");
    }

    public Task<TrocaCodePorTokenResultado> RenovarTokenAsync(string refreshToken, CancellationToken ct)
    {
        throw new OperationCanceledException("Cancelamento simulado do chamador.", ct);
    }
}
