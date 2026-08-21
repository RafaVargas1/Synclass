using FluentAssertions;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Autenticacao;

/// <summary>
/// Cobre a Regra de Negócio central da issue #18: login por código de uso
/// único, só para identidades plenas, sem revelar por que um contato não
/// tem conta associada.
/// </summary>
public sealed class LoginServiceTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task SolicitarCodigoAsync_ContatoInexistente_RejeitaSemCriarCodigo()
    {
        var (servico, _, codigos, notificador) = CriarServico();

        var acao = () => servico.SolicitarCodigoAsync("naoexiste@exemplo.com", CancellationToken.None);

        await acao.Should().ThrowAsync<ContatoSemIdentidadePlenaException>();
        codigos.Codigos.Should().BeEmpty();
        notificador.CodigosEnviados.Should().BeEmpty();
    }

    [Fact]
    public async Task SolicitarCodigoAsync_ContatoComIdentidadePlena_GeraCodigoEEnviaViaNotificador()
    {
        var (servico, usuarios, codigos, notificador) = CriarServico();
        var professor = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, null, Clock);
        await usuarios.AdicionarAsync(professor, CancellationToken.None);

        await servico.SolicitarCodigoAsync("maria@exemplo.com", CancellationToken.None);

        codigos.Codigos.Should().ContainSingle(c => c.UsuarioId == professor.Id);
        notificador.CodigosEnviados.Should().ContainSingle(e => e.Contato == "maria@exemplo.com");
    }

    [Fact]
    public async Task SolicitarCodigoAsync_SegundoPedidoAntesDeExpirar_InvalidaOCodigoAnterior()
    {
        var (servico, usuarios, codigos, _) = CriarServico();
        var professor = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, null, Clock);
        await usuarios.AdicionarAsync(professor, CancellationToken.None);
        await servico.SolicitarCodigoAsync("maria@exemplo.com", CancellationToken.None);
        var codigoAnterior = codigos.Codigos.Single();

        await servico.SolicitarCodigoAsync("maria@exemplo.com", CancellationToken.None);

        codigoAnterior.UsadoEm.Should().NotBeNull();
        codigos.Codigos.Should().HaveCount(2);
    }

    [Fact]
    public async Task ConfirmarCodigoAsync_CodigoCorretoDentroDoPrazo_GeraTokenEInvalidaOCodigo()
    {
        var (servico, usuarios, codigos, _) = CriarServico();
        var professor = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, null, Clock);
        await usuarios.AdicionarAsync(professor, CancellationToken.None);
        await servico.SolicitarCodigoAsync("maria@exemplo.com", CancellationToken.None);

        var resultado = await servico.ConfirmarCodigoAsync("maria@exemplo.com", "123456", CancellationToken.None);

        resultado.Token.Should().Be($"token-para-{professor.Id}");
        codigos.Codigos.Single().UsadoEm.Should().NotBeNull();
    }

    [Fact]
    public async Task ConfirmarCodigoAsync_CodigoIncorreto_RejeitaSemGerarToken()
    {
        var (servico, usuarios, _, _) = CriarServico();
        var professor = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, null, Clock);
        await usuarios.AdicionarAsync(professor, CancellationToken.None);
        await servico.SolicitarCodigoAsync("maria@exemplo.com", CancellationToken.None);

        var acao = () => servico.ConfirmarCodigoAsync("maria@exemplo.com", "000000", CancellationToken.None);

        await acao.Should().ThrowAsync<CodigoOtpInvalidoException>();
    }

    [Fact]
    public async Task ConfirmarCodigoAsync_CodigoExpirado_Rejeita()
    {
        var (usuarios, codigos, notificador) = (new FakeUsuarioRepository(), new FakeCodigoOtpRepository(), new FakeNotificador());
        var relogioNaSolicitacao = Clock;
        var servicoNaSolicitacao = new LoginService(usuarios, codigos, new FakeGeradorDeCodigoOtp(), notificador, new FakeGeradorDeTokenSessao(), relogioNaSolicitacao);
        var professor = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, null, Clock);
        await usuarios.AdicionarAsync(professor, CancellationToken.None);
        await servicoNaSolicitacao.SolicitarCodigoAsync("maria@exemplo.com", CancellationToken.None);
        var relogioDepoisDoPrazo = new FixedClock(Clock.UtcNow.AddMinutes(11));
        var servicoNaConfirmacao = new LoginService(usuarios, codigos, new FakeGeradorDeCodigoOtp(), notificador, new FakeGeradorDeTokenSessao(), relogioDepoisDoPrazo);

        var acao = () => servicoNaConfirmacao.ConfirmarCodigoAsync("maria@exemplo.com", "123456", CancellationToken.None);

        await acao.Should().ThrowAsync<CodigoOtpInvalidoException>();
    }

    [Fact]
    public async Task ConfirmarCodigoAsync_CodigoJaUsado_Rejeita()
    {
        var (servico, usuarios, _, _) = CriarServico();
        var professor = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, null, Clock);
        await usuarios.AdicionarAsync(professor, CancellationToken.None);
        await servico.SolicitarCodigoAsync("maria@exemplo.com", CancellationToken.None);
        await servico.ConfirmarCodigoAsync("maria@exemplo.com", "123456", CancellationToken.None);

        var acao = () => servico.ConfirmarCodigoAsync("maria@exemplo.com", "123456", CancellationToken.None);

        await acao.Should().ThrowAsync<CodigoOtpInvalidoException>();
    }

    [Fact]
    public async Task ConfirmarCodigoAsync_AtingeLimiteDeTentativasErradas_BloqueiaOCodigo()
    {
        var (servico, usuarios, codigos, _) = CriarServico();
        var professor = Usuario.Cadastrar("Maria Silva", "maria@exemplo.com", PapelUsuario.Professor, null, Clock);
        await usuarios.AdicionarAsync(professor, CancellationToken.None);
        await servico.SolicitarCodigoAsync("maria@exemplo.com", CancellationToken.None);

        for (var tentativa = 1; tentativa < CodigoOtp.MaxTentativasFalhas; tentativa++)
        {
            var acaoTentativaErrada = () => servico.ConfirmarCodigoAsync("maria@exemplo.com", "000000", CancellationToken.None);
            await acaoTentativaErrada.Should().ThrowAsync<CodigoOtpInvalidoException>();
        }

        var acaoQueEsgotaOLimite = () => servico.ConfirmarCodigoAsync("maria@exemplo.com", "000000", CancellationToken.None);
        await acaoQueEsgotaOLimite.Should().ThrowAsync<CodigoOtpBloqueadoException>();

        var acaoComCodigoCorretoAposBloqueio = () => servico.ConfirmarCodigoAsync("maria@exemplo.com", "123456", CancellationToken.None);
        await acaoComCodigoCorretoAposBloqueio.Should().ThrowAsync<CodigoOtpBloqueadoException>();
        codigos.Codigos.Single().TentativasFalhas.Should().Be(CodigoOtp.MaxTentativasFalhas);
    }

    [Fact]
    public async Task ConfirmarCodigoAsync_ContatoSemIdentidadePlena_Rejeita()
    {
        var (servico, _, _, _) = CriarServico();

        var acao = () => servico.ConfirmarCodigoAsync("naoexiste@exemplo.com", "123456", CancellationToken.None);

        await acao.Should().ThrowAsync<ContatoSemIdentidadePlenaException>();
    }

    private static (LoginService Servico, FakeUsuarioRepository Usuarios, FakeCodigoOtpRepository Codigos, FakeNotificador Notificador) CriarServico()
    {
        var usuarios = new FakeUsuarioRepository();
        var codigos = new FakeCodigoOtpRepository();
        var notificador = new FakeNotificador();
        var servico = new LoginService(usuarios, codigos, new FakeGeradorDeCodigoOtp(), notificador, new FakeGeradorDeTokenSessao(), Clock);
        return (servico, usuarios, codigos, notificador);
    }
}
