using Synclass.Domain.Pagamentos;

namespace Synclass.Api.Tests.Fakes;

/// <summary>
/// Fake de <see cref="IClienteOAuthMercadoPago"/> usado nos testes de fumaça
/// da Api (issue #203): não faz a chamada real à Api do Mercado Pago, só
/// devolve um resultado fixo configurável por teste — mesmo padrão de
/// <see cref="ValidadorDeIdTokenGoogleFixo"/> para o login do Google. Sem
/// isso, o callback dispararia HTTP real contra o Mercado Pago no teste.
/// </summary>
public sealed class FakeClienteOAuthMercadoPago : IClienteOAuthMercadoPago
{
    /// <summary>
    /// Resultado devolvido por <see cref="TrocarCodePorTokenAsync"/> e <see
    /// cref="RenovarTokenAsync"/> — configurável por teste.
    /// </summary>
    public TrocaCodePorTokenResultado Resultado { get; set; } = new(
        "access-teste", "refresh-teste", "collector-teste", DateTimeOffset.UtcNow.AddHours(1));

    public string MontarUrlAutorizacao(string state, string redirectUri)
    {
        return $"https://auth.mercadopago.com.br/authorization?state={state}&redirect_uri={redirectUri}";
    }

    public Task<TrocaCodePorTokenResultado> TrocarCodePorTokenAsync(string code, string redirectUri, CancellationToken ct)
    {
        return Task.FromResult(Resultado);
    }

    public Task<TrocaCodePorTokenResultado> RenovarTokenAsync(string refreshToken, CancellationToken ct)
    {
        return Task.FromResult(Resultado);
    }
}
