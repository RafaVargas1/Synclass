using FluentAssertions;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Autenticacao;

/// <summary>
/// Cobre a Regra de Negócio do login Google (issue #65): autentica e-mail
/// que corresponde a usuário existente, sinaliza cadastro pendente quando
/// não corresponde (nunca cria conta implicitamente), e rejeita e-mail não
/// verificado e token inválido. Fluxo paralelo ao login OTP — não toca
/// <see cref="LoginService"/> nem <see cref="ContatoSemIdentidadePlenaException"/>.
/// </summary>
public sealed class LoginComGoogleServiceTests
{
    private static readonly string[] IdTokenValido = { "token-google-valido" };

    [Fact]
    public async Task AutenticarAsync_EmailDeUsuarioExistente_RetornaLoginComToken()
    {
        var usuarios = new FakeUsuarioRepository();
        var professor = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, null, new FixedClock(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero)));
        await usuarios.AdicionarAsync(professor, CancellationToken.None);
        var servico = CriarServico(usuarios, new InformacoesIdTokenGoogle("MARIA@Exemplo.com", EmailVerificado: true));

        var resultado = await servico.AutenticarAsync("token-google-valido", CancellationToken.None);

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
        var servico = CriarServico(usuarios, new InformacoesIdTokenGoogle("novo@exemplo.com", EmailVerificado: true));

        var resultado = await servico.AutenticarAsync("token-google-valido", CancellationToken.None);

        resultado.CadastroPendente.Should().BeTrue();
        resultado.EmailNormalizado.Should().Be("novo@exemplo.com");
        resultado.Login.Should().BeNull();
    }

    [Fact]
    public async Task AutenticarAsync_ContatoDeTelefoneSemUsuario_ResultaEmCadastroPendente()
    {
        // Contato de cadastro por telefone não bate com e-mail do Google —
        // cai no caminho "sem usuário correspondente" (edge point do card #65).
        var usuarios = new FakeUsuarioRepository();
        await usuarios.AdicionarAsync(Usuario.Cadastrar("Maria Silva", "11987654321", PapelUsuario.Professor, null, new FixedClock(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero))), CancellationToken.None);
        var servico = CriarServico(usuarios, new InformacoesIdTokenGoogle("maria@exemplo.com", EmailVerificado: true));

        var resultado = await servico.AutenticarAsync("token-google-valido", CancellationToken.None);

        resultado.CadastroPendente.Should().BeTrue();
        resultado.EmailNormalizado.Should().Be("maria@exemplo.com");
    }

    [Fact]
    public async Task AutenticarAsync_EmailNaoVerificado_LancaEmailGoogleNaoVerificado()
    {
        var usuarios = new FakeUsuarioRepository();
        var servico = CriarServico(usuarios, new InformacoesIdTokenGoogle("maria@exemplo.com", EmailVerificado: false));

        var acao = () => servico.AutenticarAsync("token-google-valido", CancellationToken.None);

        await acao.Should().ThrowAsync<EmailGoogleNaoVerificadoException>();
    }

    [Fact]
    public async Task AutenticarAsync_TokenInvalido_LancaTokenGoogleInvalido()
    {
        var usuarios = new FakeUsuarioRepository();
        var servico = CriarServico(usuarios, null);

        var acao = () => servico.AutenticarAsync("token-google-invalido", CancellationToken.None);

        await acao.Should().ThrowAsync<TokenGoogleInvalidoException>();
    }

    private static LoginComGoogleService CriarServico(
        FakeUsuarioRepository usuarios,
        InformacoesIdTokenGoogle? informacoes)
    {
        return new LoginComGoogleService(
            usuarios,
            new FakeValidadorDeIdTokenGoogle(informacoes),
            new FakeGeradorDeTokenSessao());
    }
}
