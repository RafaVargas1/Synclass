using Microsoft.IdentityModel.Tokens;

namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Informações extraídas de um idToken da Apple após validação criptográfica
/// (assinatura/issuer/audience). Espelha <see cref="InformacoesIdTokenGoogle"/> —
/// mesmo padrão de resultado puro de validação, sem comportamento (ver
/// <c>docs/spec/212-login-apple-web/implementation.md</c>).
/// </summary>
public sealed record InformacoesIdTokenApple(string Email, bool EmailVerificado);

/// <summary>
/// Abstrai a busca das chaves públicas de assinatura da Apple (JWKS em
/// <c>https://appleid.apple.com/auth/keys</c>) atrás de uma interface fina,
/// conforme docs/spec/code-style.md#dependências — mesmo racional de
/// <see cref="IValidadorDeIdTokenGoogle"/> abstrair o SDK, aqui para o Domain
/// não depender de <c>HttpClient</c> diretamente. A implementação concreta
/// (<c>ClienteJwksApple</c>, em Synclass.Infrastructure) cuida do HTTP e do
/// cache; o Domain só consome as chaves já prontas para validação (tipadas
/// como <see cref="SecurityKey"/> pelo <c>JsonWebKeySet.GetSigningKeys()</c>).
/// </summary>
public interface IClienteJwksApple
{
    Task<IReadOnlyCollection<SecurityKey>> ObterChavesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Envolve a validação criptográfica de um idToken da Apple
/// (JwtSecurityTokenHandler + JWKS, em Synclass.Infrastructure) atrás de uma
/// interface fina — mesmo padrão de <see cref="IValidadorDeIdTokenGoogle"/>.
/// Retorna <c>null</c> quando a assinatura/issuer/audience/lifetime não
/// valida e nunca lança para esse caso (token malformado é esperado vindo de
/// um client não confiável), para o domínio decidir como tratar conforme a
/// Regra de Negócio do card #212.
/// </summary>
public interface IValidadorDeIdTokenApple
{
    Task<InformacoesIdTokenApple?> ValidarAsync(string idToken, CancellationToken cancellationToken);
}
