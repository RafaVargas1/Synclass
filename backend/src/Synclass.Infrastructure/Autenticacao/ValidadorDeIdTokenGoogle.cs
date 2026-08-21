using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Synclass.Domain.Autenticacao;

namespace Synclass.Infrastructure.Autenticacao;

/// <summary>
/// Implementação de <see cref="IValidadorDeIdTokenGoogle"/> envolvendo o SDK
/// <c>Google.Apis.Auth</c> (issue #65): valida a assinatura/issuer do idToken
/// e a <c>Audience</c> contra o <c>GoogleClientId</c> configurado
/// (GOOGLE_CLIENT_ID). Captura <see cref="InvalidJwtException"/> e devolve
/// <c>null</c>, para a exceção do SDK nunca cruzar a fronteira
/// Domain/Infrastructure — token malformado é esperado vindo de um client
/// não confiável (ver docs/spec/security-rules.md#validação-de-entrada).
/// </summary>
public sealed class ValidadorDeIdTokenGoogle : IValidadorDeIdTokenGoogle
{
    private readonly string _googleClientId;

    public ValidadorDeIdTokenGoogle(IConfiguration configuration)
    {
        _googleClientId = configuration["GoogleClientId"] ?? string.Empty;
    }

    public async Task<InformacoesIdTokenGoogle?> ValidarAsync(string idToken, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { _googleClientId } });

            return new InformacoesIdTokenGoogle(payload.Email, payload.EmailVerified);
        }
        catch (InvalidJwtException)
        {
            return null;
        }
    }
}
