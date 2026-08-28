using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Synclass.Domain.Pagamentos;

namespace Synclass.Infrastructure.Http;

/// <summary>
/// Implementação real de <see cref="IClienteOAuthMercadoPago"/> (issue
/// #203): fala com a API OAuth 2.0 do Mercado Pago via <see
/// cref="HttpClient"/>. <see cref="MontarUrlAutorizacao"/> é síncrono porque
/// só monta a string da URL de autorização que o Professor abre no
/// navegador (não chama rede); apenas a troca de <c>code</c> por token e a
/// renovação via <c>refresh_token</c> fazem chamada HTTP. É o primeiro
/// wrapper HTTP do repo — ver implementation.md#integração-http.
/// </summary>
public sealed class ClienteOAuthMercadoPago : IClienteOAuthMercadoPago
{
    private const string UrlAutorizacao = "https://auth.mercadopago.com.br/authorization";
    private const string EndpointToken = "/oauth/token";

    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly ILogger<ClienteOAuthMercadoPago> _logger;

    public ClienteOAuthMercadoPago(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ClienteOAuthMercadoPago> logger)
    {
        _httpClient = httpClient;
        _clientId = configuration["MercadoPago:ClientId"]
            ?? throw new InvalidOperationException("Configuração ausente: MercadoPago:ClientId.");
        _clientSecret = configuration["MercadoPago:ClientSecret"]
            ?? throw new InvalidOperationException("Configuração ausente: MercadoPago:ClientSecret.");
        _logger = logger;
    }

    public string MontarUrlAutorizacao(string state, string redirectUri)
    {
        // URL de autorização que o Professor abre no navegador (ver
        // implementation.md#integração-http) — não é chamada de rede, só
        // montagem de string. QueryHelpers.AddQueryString já codifica
        // redirect_uri e state (query value encoding), sem re-codificar o
        // client_id já codificado no primeiro parâmetro.
        return QueryHelpers.AddQueryString(UrlAutorizacao, new Dictionary<string, string?>
        {
            ["client_id"] = _clientId,
            ["response_type"] = "code",
            ["platform_id"] = "mp",
            ["redirect_uri"] = redirectUri,
            ["state"] = state,
        });
    }

    public async Task<TrocaCodePorTokenResultado> TrocarCodePorTokenAsync(
        string code, string redirectUri, CancellationToken ct)
    {
        var corpo = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
        };
        return await TrocarTokenAsync("troca de code", corpo, ct);
    }

    public async Task<TrocaCodePorTokenResultado> RenovarTokenAsync(string refreshToken, CancellationToken ct)
    {
        var corpo = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _clientId,
            ["client_secret"] = _clientSecret,
            ["refresh_token"] = refreshToken,
        };
        return await TrocarTokenAsync("renovação de token", corpo, ct);
    }

    /// <summary>
    /// Executa o POST /oauth/token com o payload de credenciais e devolve o
    /// resultado (access/refresh token, <c>collector_id</c> e expiração),
    /// lançando <see cref="MercadoPagoApiException"/> em resposta não-2xx —
    /// sem logar o corpo cru do erro (que pode conter token, ver
    /// implementation.md#erro-cru-do-mercado-pago-não-vaza).
    /// </summary>
    private async Task<TrocaCodePorTokenResultado> TrocarTokenAsync(
        string acao, Dictionary<string, string> corpo, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, EndpointToken)
            {
                Content = new FormUrlEncodedContent(corpo),
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var resposta = await _httpClient.SendAsync(request, ct);
            var corpoResposta = await resposta.Content.ReadAsStringAsync(ct);

            if (!resposta.IsSuccessStatusCode)
            {
                throw new MercadoPagoApiException(
                    $"Falha na {acao} no Mercado Pago. Status: {resposta.StatusCode}", new HttpRequestException());
            }

            using var documento = JsonDocument.Parse(corpoResposta);
            var raiz = documento.RootElement;
            return new TrocaCodePorTokenResultado(
                raiz.GetProperty("access_token").GetString()!,
                raiz.GetProperty("refresh_token").GetString()!,
                raiz.GetProperty("user_id").GetInt64().ToString(),
                DateTimeOffset.UtcNow.AddSeconds(raiz.GetProperty("expires_in").GetInt32()));
        }
        catch (MercadoPagoApiException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha de rede na chamada ao Mercado Pago para {Endpoint}", EndpointToken);
            throw new MercadoPagoApiException($"Falha na chamada ao Mercado Pago: {EndpointToken}", ex);
        }
    }
}
