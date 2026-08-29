using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Synclass.Domain.Tests.Fakes;
using Synclass.Infrastructure.Autenticacao;

namespace Synclass.Domain.Tests.Autenticacao;

/// <summary>
/// Cobre a validação criptográfica do idToken da Apple (issue #212): assinatura
/// válida, assinatura inválida, issuer/audience/lifetime fora do esperado e o
/// edge point do <c>email_verified</c> como string — usando um
/// <see cref="FakeClienteJwksApple"/> com chave RSA de teste e um JWT real
/// assinado com <see cref="JwtSecurityTokenHandler"/>, sem I/O. Desde #213 o
/// validador aceita duas audiences (Services ID da web + Bundle ID do app
/// nativo), então os testes cobrem as duas.
/// </summary>
public sealed class ValidadorDeIdTokenAppleTests
{
    private const string IssuerValido = "https://appleid.apple.com";
    private const string AudienceValida = "com.synclass.services";
    private const string AudienceNativa = "br.com.synclass.app";

    [Fact]
    public async Task ValidarAsync_TokenAssinadoComChaveDoJwks_RetornaInformacoesDoToken()
    {
        var chave = CriarChaveRsa();
        var validador = CriarValidador(chave, AudienceValida, AudienceNativa);
        var idToken = CriarIdToken(
            chave,
            AudienceValida,
            expires: DateTime.UtcNow.AddHours(1),
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            email: "maria@exemplo.com",
            emailVerificado: "true");

        var informacoes = await validador.ValidarAsync(idToken, CancellationToken.None);

        informacoes.Should().NotBeNull();
        informacoes!.Email.Should().Be("maria@exemplo.com");
        informacoes.EmailVerificado.Should().BeTrue();
    }

    [Fact]
    public async Task ValidarAsync_TokenComAudDoBundleIdNativo_Aceita()
    {
        // Issue #213: o identityToken do signInAsync nativo vem com `aud` =
        // bundle identifier do app (`br.com.synclass.app`), um valor diferente
        // do Services ID da web (`com.synclass.services`). Antes desta mudança
        // (ValidAudience singular), um token nativo genuíno falhava a
        // validação de audience — mesmo contrato, o que destrava o login nativo.
        var chave = CriarChaveRsa();
        var validador = CriarValidador(chave, AudienceValida, AudienceNativa);
        var idToken = CriarIdToken(
            chave,
            AudienceNativa,
            expires: DateTime.UtcNow.AddHours(1),
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            email: "maria@exemplo.com",
            emailVerificado: "true");

        var informacoes = await validador.ValidarAsync(idToken, CancellationToken.None);

        informacoes.Should().NotBeNull();
        informacoes!.Email.Should().Be("maria@exemplo.com");
    }

    [Fact]
    public async Task ValidarAsync_TokenAssinadoComChaveForaDoJwks_RetornaNull()
    {
        var chaveNoJwks = CriarChaveRsa();
        var chaveForaDoJwks = CriarChaveRsa();
        var validador = CriarValidador(chaveNoJwks, AudienceValida, AudienceNativa);
        var idToken = CriarIdToken(
            chaveForaDoJwks,
            AudienceValida,
            expires: DateTime.UtcNow.AddHours(1),
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            email: "maria@exemplo.com",
            emailVerificado: "true");

        var informacoes = await validador.ValidarAsync(idToken, CancellationToken.None);

        informacoes.Should().BeNull();
    }

    [Fact]
    public async Task ValidarAsync_IssuerDiferenteDoEsperado_RetornaNull()
    {
        var chave = CriarChaveRsa();
        var validador = CriarValidador(chave, AudienceValida, AudienceNativa);
        var idToken = CriarIdToken(
            chave,
            AudienceValida,
            expires: DateTime.UtcNow.AddHours(1),
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            email: "maria@exemplo.com",
            emailVerificado: "true",
            issuer: "https://outro-emissor.example.com");

        var informacoes = await validador.ValidarAsync(idToken, CancellationToken.None);

        informacoes.Should().BeNull();
    }

    [Fact]
    public async Task ValidarAsync_AudienceDiferenteDasConfiguradas_RetornaNull()
    {
        var chave = CriarChaveRsa();
        var validador = CriarValidador(chave, AudienceValida, AudienceNativa);
        var idToken = CriarIdToken(
            chave,
            audience: "outra-audience",
            expires: DateTime.UtcNow.AddHours(1),
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            email: "maria@exemplo.com",
            emailVerificado: "true");

        var informacoes = await validador.ValidarAsync(idToken, CancellationToken.None);

        informacoes.Should().BeNull();
    }

    [Fact]
    public async Task ValidarAsync_TokenExpirado_RetornaNull()
    {
        var chave = CriarChaveRsa();
        var validador = CriarValidador(chave, AudienceValida, AudienceNativa);
        var idToken = CriarIdToken(
            chave,
            AudienceValida,
            expires: DateTime.UtcNow.AddMinutes(-5),
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            email: "maria@exemplo.com",
            emailVerificado: "true");

        var informacoes = await validador.ValidarAsync(idToken, CancellationToken.None);

        informacoes.Should().BeNull();
    }

    [Fact]
    public async Task ValidarAsync_EmailVerifiedComoStringTrue_InterpretaComoVerificado()
    {
        // Edge point do implementation.md#edge-points: o token da Apple traz
        // `email_verified` como STRING ("true"/"false"), não booleano JSON —
        // o parsing por comparação de string precisa reconhecer "true"
        // (case-insensitive) e ignorar "false"/outros valores.
        var chave = CriarChaveRsa();
        var validador = CriarValidador(chave, AudienceValida, AudienceNativa);
        var idToken = CriarIdToken(
            chave,
            AudienceValida,
            expires: DateTime.UtcNow.AddHours(1),
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            email: "maria@exemplo.com",
            emailVerificado: "true");

        var informacoes = await validador.ValidarAsync(idToken, CancellationToken.None);

        informacoes.Should().NotBeNull();
        informacoes!.EmailVerificado.Should().BeTrue();
    }

    private static ValidadorDeIdTokenApple CriarValidador(RsaSecurityKey chaveNoJwks, params string[] audiences)
    {
        return new ValidadorDeIdTokenApple(new FakeClienteJwksApple(chaveNoJwks), audiences);
    }

    private static RsaSecurityKey CriarChaveRsa()
    {
        return new RsaSecurityKey(System.Security.Cryptography.RSA.Create(2048));
    }

    private static string CriarIdToken(
        RsaSecurityKey chaveDeAssinatura,
        string audience,
        DateTime expires,
        DateTime notBefore,
        string email,
        string emailVerificado,
        string issuer = IssuerValido)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Email, email),
            new("email_verified", emailVerificado),
        };
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: notBefore,
            expires: expires,
            signingCredentials: new SigningCredentials(chaveDeAssinatura, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
