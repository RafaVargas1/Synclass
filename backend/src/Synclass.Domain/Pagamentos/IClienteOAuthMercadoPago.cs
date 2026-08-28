namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Wrapper fino sobre a API OAuth 2.0 do Mercado Pago (issue #203), no
/// Domain — envolve o HTTP de saída atrás de uma interface de propriedade
/// do projeto (docs/spec/code-style.md#dependências). <see
/// cref="MontarUrlAutorizacao"/> é síncrono porque só monta a string da URL
/// de autorização (não chama rede); apenas <see cref="TrocarCodePorTokenAsync"/>
/// e <see cref="RenovarTokenAsync"/> são assíncronos porque chamam a API do
/// Mercado Pago. Não existe <c>ObterRedirectUriAsync</c>: a
/// <c>redirect_uri</c> é uma constante fixa lida via
/// <c>IConfiguration["MercadoPago:RedirectUri"]</c>, nunca obtida de forma
/// assíncrona.
/// </summary>
public interface IClienteOAuthMercadoPago
{
    /// <summary>
    /// Monta a URL de autorização que o Professor abre no navegador (ver
    /// implementation.md#integração-http), já com o <paramref name="state"/>
    /// gerado e a <paramref name="redirectUri"/> fixa embutidos.
    /// </summary>
    string MontarUrlAutorizacao(string state, string redirectUri);

    /// <summary>
    /// Troca o <c>code</c> de autorização (recebido pelo callback) por
    /// access/refresh token e o <c>collector_id</c> (issue #203).
    /// </summary>
    Task<TrocaCodePorTokenResultado> TrocarCodePorTokenAsync(string code, string redirectUri, CancellationToken ct);

    /// <summary>
    /// Renova o access token usando o <c>refresh_token</c> (issue #203),
    /// quando o token atual está a menos de <see cref="ConexaoMercadoPago.MargemRenovacaoMinutos"/>
    /// de expirar.
    /// </summary>
    Task<TrocaCodePorTokenResultado> RenovarTokenAsync(string refreshToken, CancellationToken ct);
}
