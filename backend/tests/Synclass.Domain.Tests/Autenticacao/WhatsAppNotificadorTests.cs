using FluentAssertions;
using Microsoft.Extensions.Logging;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.Tests.Fakes;
using Synclass.Infrastructure.Autenticacao;

namespace Synclass.Domain.Tests.Autenticacao;

/// <summary>
/// Cobre o comportamento de <see cref="WhatsAppNotificador"/>
/// (INotificador para o canal WhatsApp, issue #193): envia a mensagem ao
/// provedor via <see cref="IWhatsAppHttpClient"/> e propaga falha de envio
/// como <see cref="OtpEnvioException"/> — nunca expondo o código OTP em
/// log estruturado.
/// </summary>
public sealed class WhatsAppNotificadorTests
{
    private const string ContatoE164 = "+5511999999999";
    private const string Codigo = "123456";

    [Fact]
    public async Task EnviarCodigoOtpAsync_CanalResponde_EnviaMensagemSemLancarErro()
    {
        var http = new FakeWhatsAppHttpClientSucesso();
        var logger = new FakeLogger<WhatsAppNotificador>();
        var notificador = new WhatsAppNotificador(http, logger);

        var acao = () => notificador.EnviarCodigoOtpAsync(ContatoE164, Codigo, CancellationToken.None);

        await acao.Should().NotThrowAsync();
        var (numeroE164, mensagem) = http.MensagensEnviadas.Should().ContainSingle().Which;
        numeroE164.Should().Be(ContatoE164);
        mensagem.Should().Contain(Codigo);
    }

    [Fact]
    public async Task EnviarCodigoOtpAsync_CanalResponde_LogaOtpEnviadoSemCodigoEmTextoPuro()
    {
        var http = new FakeWhatsAppHttpClientSucesso();
        var logger = new FakeLogger<WhatsAppNotificador>();
        var notificador = new WhatsAppNotificador(http, logger);

        await notificador.EnviarCodigoOtpAsync(ContatoE164, Codigo, CancellationToken.None);

        var evento = logger.Eventos.Should().ContainSingle(e => e.Linha.StartsWith("OtpEnviado")).Which;
        evento.Linha.Should().NotContain(Codigo);
        NenhumArgumentoContemOCodigo(evento.Argumentos);
    }

    [Fact]
    public async Task EnviarCodigoOtpAsync_CanalFalha_PropagaOtpEnvioExceptionComMotivoSemDetalheTecnico()
    {
        var http = new FakeWhatsAppHttpClientFalha();
        var logger = new FakeLogger<WhatsAppNotificador>();
        var notificador = new WhatsAppNotificador(http, logger);

        var acao = () => notificador.EnviarCodigoOtpAsync(ContatoE164, Codigo, CancellationToken.None);

        var excecao = await acao.Should().ThrowAsync<OtpEnvioException>();
        excecao.Which.Motivo.Should().Contain("Não foi possível enviar o código");
        excecao.Which.Motivo.Should().NotContain("Twilio");
        excecao.Which.Motivo.Should().NotContain("HttpRequest");
    }

    [Fact]
    public async Task EnviarCodigoOtpAsync_CanalFalha_LogaOtpEnvioFalhouSemCodigo()
    {
        var http = new FakeWhatsAppHttpClientFalha();
        var logger = new FakeLogger<WhatsAppNotificador>();
        var notificador = new WhatsAppNotificador(http, logger);

        await Assert.ThrowsAsync<OtpEnvioException>(() =>
            notificador.EnviarCodigoOtpAsync(ContatoE164, Codigo, CancellationToken.None));

        var evento = logger.Eventos.Should().ContainSingle(e => e.Linha.StartsWith("OtpEnvioFalhou")).Which;
        evento.Linha.Should().NotContain(Codigo);
        NenhumArgumentoContemOCodigo(evento.Argumentos);
    }

    [Fact]
    public async Task EnviarCodigoOtpAsync_ContatoEmail_LancaOtpEnvioExceptionSemChamarProvedor()
    {
        var http = new FakeWhatsAppHttpClientSucesso();
        var logger = new FakeLogger<WhatsAppNotificador>();
        var notificador = new WhatsAppNotificador(http, logger);

        var acao = () => notificador.EnviarCodigoOtpAsync("maria@exemplo.com", Codigo, CancellationToken.None);

        // Contato válido (e-mail), mas sem canal de envio implementado ainda
        // (issue #193 cobre só WhatsApp): 502 (OtpEnvioException), nunca 400
        // (ContatoInvalidoException) — o contato em si não está errado.
        var excecao = await acao.Should().ThrowAsync<OtpEnvioException>();
        excecao.Which.Motivo.Should().Contain("não está disponível");
        http.MensagensEnviadas.Should().BeEmpty();
    }

    private static void NenhumArgumentoContemOCodigo(IReadOnlyList<object?> argumentos)
    {
        foreach (var argumento in argumentos)
        {
            if (argumento is string texto)
            {
                texto.Should().NotContain(Codigo);
            }
        }
    }
}
