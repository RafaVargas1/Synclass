using System.Net.Http.Json;
using Microsoft.IdentityModel.Tokens;
using Synclass.Domain.Autenticacao;

namespace Synclass.Infrastructure.Autenticacao;

/// <summary>
/// Implementação de <see cref="IClienteJwksApple"/> (issue #212): busca as
/// chaves públicas de assinatura da Apple via
/// <c>GET https://appleid.apple.com/auth/keys</c> e as devolve prontas para
/// validação usando <see cref="JsonWebKeySet.GetSigningKeys"/> (mesmo pacote
/// <c>Microsoft.IdentityModel.Tokens</c> da autenticação JWT do projeto).
/// Desserializa o corpo como <see cref="JsonWebKeySet"/> — a classe já faz a
/// conversão dos campos JWK (kty/n/e) para <see cref="SecurityKey"/> sem
/// conversão manual de RSA.
///
/// Sem cache em memória por enquanto: as chaves raramente rotacionam mas o
/// custo de uma chamada por login é aceitável para o volume atual (edge
/// point do implementation.md#edge-points — cache pode ser adicionado depois
/// sem mudar o contrato da interface).
/// </summary>
public sealed class ClienteJwksApple : IClienteJwksApple
{
    private const string UrlJwksApple = "https://appleid.apple.com/auth/keys";

    private readonly HttpClient _httpClient;

    public ClienteJwksApple(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyCollection<SecurityKey>> ObterChavesAsync(CancellationToken cancellationToken)
    {
        using var resposta = await _httpClient.GetAsync(UrlJwksApple, cancellationToken);
        resposta.EnsureSuccessStatusCode();

        var jwks = await resposta.Content.ReadFromJsonAsync<JsonWebKeySet>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Resposta vazia do endpoint JWKS da Apple.");
        return jwks.GetSigningKeys().ToArray();
    }
}
