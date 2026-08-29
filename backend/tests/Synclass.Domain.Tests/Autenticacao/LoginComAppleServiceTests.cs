using FluentAssertions;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Autenticacao;

/// <summary>
/// Cobre a Regra de Negócio do login Apple (issue #212): autentica e-mail
/// que corresponde a usuário existente, sinaliza cadastro pendente quando
/// não corresponde (a Apple não emite conta nova — mesma RN do Google, issue
/// #65), e rejeita e-mail não verificado e token inválido. Mesma estrutura
/// de <see cref="LoginComGoogleServiceTests"/>, sem abstração compartilhada
/// entre os dois provedores (ver implementation.md#decisão-sem-serviço-genérico-compartilhado).
/// </summary>
public sealed class LoginComAppleServiceTests
{
    [Fact]
    public async Task AutenticarAsync_EmailDeUsuarioExistente_RetornaLoginComToken()
    {
        var usuarios = new FakeUsuarioRepository();
        var professor = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, null, new FixedClock(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero)));
        await usuarios.AdicionarAsync(professor, CancellationToken.None);
        var servico = CriarServico(usuarios, new InformacoesIdTokenApple("MARIA@Exemplo.com", EmailVerificado: true));

        var resultado = await servico.AutenticarAsync("idToken-apple-valido", CancellationToken.None);

        resultado.CadastroPendente.Should().BeFalse();
        resultado.Login.Should().NotBeNull();
        resultado.Login!.Token.Should().Be($"token-para-{professor.Id}");
        resultado.Login!.Usuario.Id.Should().Be(professor.Id);
        resultado.EmailNormalizado.Should().Be("maria@exemplo.com");
    }

    [Fact]
    public async Task AutenticarAsync_EmailSemUsuarioCorrespondente_ResultaEmCadastroPendente()
    {
        var usuarios = new FakeUsuarioRepository();
        var servico = CriarServico(usuarios, new InformacoesIdTokenApple("novo@exemplo.com", EmailVerificado: true));

        var resultado = await servico.AutenticarAsync("idToken-apple-valido", CancellationToken.None);

        resultado.CadastroPendente.Should().BeTrue();
        resultado.EmailNormalizado.Should().Be("novo@exemplo.com");
        resultado.Login.Should().BeNull();
    }

    [Fact]
    public async Task AutenticarAsync_EmailNaoVerificado_LancaEmailAppleNaoVerificado()
    {
        var usuarios = new FakeUsuarioRepository();
        var servico = CriarServico(usuarios, new InformacoesIdTokenApple("maria@exemplo.com", EmailVerificado: false));

        var acao = () => servico.AutenticarAsync("idToken-apple-valido", CancellationToken.None);

        await acao.Should().ThrowAsync<EmailAppleNaoVerificadoException>();
    }

    [Fact]
    public async Task AutenticarAsync_TokenInvalido_LancaTokenAppleInvalido()
    {
        var usuarios = new FakeUsuarioRepository();
        var servico = CriarServico(usuarios, null);

        var acao = () => servico.AutenticarAsync("idToken-apple-invalido", CancellationToken.None);

        await acao.Should().ThrowAsync<TokenAppleInvalidoException>();
    }

    private static LoginComAppleService CriarServico(
        FakeUsuarioRepository usuarios,
        InformacoesIdTokenApple? informacoes)
    {
        return new LoginComAppleService(
            usuarios,
            new FakeValidadorDeIdTokenApple(informacoes),
            new FakeGeradorDeTokenSessao());
    }
}
