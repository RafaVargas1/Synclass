using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Synclass.Domain.Autenticacao;

namespace Synclass.Infrastructure.Autenticacao;

/// <summary>
/// Implementação de <see cref="IValidadorDeIdTokenApple"/> (issue #212):
/// valida a assinatura do idToken contra as chaves JWKS da Apple (via
/// <see cref="IClienteJwksApple"/>), o issuer fixo
/// <c>https://appleid.apple.com</c>, a audience contra o
/// <c>AppleClientId</c> configurado e o lifetime do token. Captura
/// <see cref="SecurityTokenException"/> (classe base de toda exceção de
/// validação do pacote — assinatura inválida, issuer errado, audience errada,
/// token expirado) e devolve <c>null</c>, para a exceção não cruzar a
/// fronteira Domain/Infrastructure (ver
/// docs/spec/security-rules.md#validação-de-entrada).
///
/// Usa <c>MapInboundClaims = false</c> no handler para os claims do token
/// manterem os nomes curtos do JWT original ("email", "email_verified") — sem
/// isso, o mapeamento padrão converte "email" para o URI longo
/// <c>ClaimTypes.Email</c> e a leitura por nome JWT falha em busca vazia
/// (achado do teste de unidade, issue #212).
///
/// O claim <c>email_verified</c> do token da Apple é uma STRING
/// ("true"/"false"), não um booleano JSON — lido por comparação de string
/// (edge point do implementation.md#edge-points). Se o e-mail vier ausente na
/// claim (a Apple só devolve e-mail na primeira autorização), devolve
/// <c>null</c> — limitação conhecida documentada em implementation.md.
/// </summary>
public sealed class ValidadorDeIdTokenApple : IValidadorDeIdTokenApple
{
    private readonly IClienteJwksApple _clienteJwks;
    private readonly string _appleClientId;

    public ValidadorDeIdTokenApple(IClienteJwksApple clienteJwks, string appleClientId)
    {
        _clienteJwks = clienteJwks;
        _appleClientId = appleClientId;
    }

    public async Task<InformacoesIdTokenApple?> ValidarAsync(string idToken, CancellationToken cancellationToken)
    {
        try
        {
            var chaves = await _clienteJwks.ObterChavesAsync(cancellationToken);
            var parametros = new TokenValidationParameters
            {
                ValidIssuer = "https://appleid.apple.com",
                ValidAudience = _appleClientId,
                IssuerSigningKeys = chaves,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
            };
            var principal = new JwtSecurityTokenHandler { MapInboundClaims = false }
                .ValidateToken(idToken, parametros, out _);

            var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            if (string.IsNullOrWhiteSpace(email))
            {
                // E-mail ausente na claim (logins subsequentes da Apple não
                // devolvem e-mail) — ver implementation.md#edge-points.
                return null;
            }

            var emailVerificado = string.Equals(
                principal.FindFirst("email_verified")?.Value,
                "true",
                StringComparison.OrdinalIgnoreCase);

            return new InformacoesIdTokenApple(email, emailVerificado);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }
}
