using Synclass.Domain.Pagamentos;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Fake de <see cref="IClienteOAuthMercadoPago"/> usado nos testes de
/// unidade do Domain — não faz rede nem criptografia, só registra as
/// chamadas para o teste poder inspecionar e devolve valores controlados
/// (ver docs/spec/code-style.md#testes — fakes manuais nomeados, sem Moq).
/// </summary>
public sealed class FakeClienteOAuthMercadoPago : IClienteOAuthMercadoPago
{
    public string? UltimoStateNaUrl { get; private set; }
    public string? UltimaRedirectUri { get; private set; }
    public string? UltimoCode { get; private set; }
    public string? UltimoRefreshToken { get; private set; }

    /// <summary>
    /// Resultado devolvido por <see cref="TrocarCodePorTokenAsync"/> e <see
    /// cref="RenovarTokenAsync"/> — configurável por teste.
    /// </summary>
    public TrocaCodePorTokenResultado? ResultadoTroca { get; set; }

    public string MontarUrlAutorizacao(string state, string redirectUri)
    {
        UltimoStateNaUrl = state;
        UltimaRedirectUri = redirectUri;
        return $"https://auth.mercadopago.com.br/authorization?state={state}&redirect_uri={redirectUri}";
    }

    public Task<TrocaCodePorTokenResultado> TrocarCodePorTokenAsync(string code, string redirectUri, CancellationToken ct)
    {
        UltimoCode = code;
        return Task.FromResult(ResultadoTroca!);
    }

    public Task<TrocaCodePorTokenResultado> RenovarTokenAsync(string refreshToken, CancellationToken ct)
    {
        UltimoRefreshToken = refreshToken;
        return Task.FromResult(ResultadoTroca!);
    }
}
