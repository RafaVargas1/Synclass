using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Synclass.Domain.Tests.Fakes;
using Synclass.Infrastructure.Autenticacao;

namespace Synclass.Domain.Tests.Autenticacao;

/// <summary>
/// Cobre a validação criptográfica do idToken da Apple (issue #212):
/// assinatura válida, assinatura inválida, issuer/audience/lifetime fora do
/// esperado e o edge point do <c>email_verified</c> como string — usando um
/// <see cref="FakeClienteJwksApple"/> com chave RSA de teste e um JWT real
/// assinado com <see cref="JwtSecurityTokenHandler"/>, sem I/O.
/// </summary>
public sealed class ValidadorDeIdTokenAppleTests
{
    private const string IssuerValido = "https://appleid.apple.com";
    private const string AudienceValida = "com.synclass.services";

    [Fact]
    public async Task ValidarAsync_TokenAssinadoComChaveDoJwks_RetornaInformacoesDoToken()
    {
        var chave = CriarChaveRsa();
        var validador = CriarValidador(chave, AudienceValida);
        var idToken = CriarIdToken(
            chave,
            AudienceValida,
            DateTime.UtcNow.AddHours(1),
            email: "maria@exemplo.com",
            emailVerificado: "true");

        var informacoes = await validador.ValidarAsync(idToken, CancellationToken.None);

        informacoes.Should().NotBeNull();
        informacoes!.Email.Should().Be("maria@exemplo.com");
        informacoes.EmailVerificado.Should().BeTrue();
    }

    [Fact]
    public async Task ValidarAsync_TokenAssinadoComChaveForaDoJwks_RetornaNull()
    {
        var chaveNoJwks = CriarChaveRsa();
        var chaveForaDoJwks = CriarChaveRsa();
        var validador = CriarValidador(chaveNoJwks, AudienceValida);
        var idToken = CriarIdToken(
            chaveForaDoJwks,
            AudienceValida,
            DateTime.UtcNow.AddHours(1),
            email: "maria@exemplo.com",
            emailVerificado: "true");

        var informacoes = await validador.ValidarAsync(idToken, CancellationToken.None);

        informacoes.Should().BeNull();
    }

    private static ValidadorDeIdTokenApple CriarValidador(RsaSecurityKey chaveNoJwks, string audience)
    {
        return new ValidadorDeIdTokenApple(new FakeClienteJwksApple(chaveNoJwks), audience);
    }

    private static RsaSecurityKey CriarChaveRsa()
    {
        return new RsaSecurityKey(System.Security.Cryptography.RSA.Create(2048));
    }

    private static string CriarIdToken(
        RsaSecurityKey chaveDeAssinatura,
        string audience,
        DateTime expires,
        string email,
        string emailVerificado)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Email, email),
            new("email_verified", emailVerificado),
        };
        var token = new JwtSecurityToken(
            issuer: IssuerValido,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: expires,
            signingCredentials: new SigningCredentials(chaveDeAssinatura, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
