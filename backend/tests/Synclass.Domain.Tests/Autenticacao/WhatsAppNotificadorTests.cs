using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.Tests.Fakes;
using Synclass.Infrastructure.Autenticacao;

namespace Synclass.Domain.Tests.Autenticacao;

/// <summary>
/// Cobre a entrega de código OTP via WhatsAppNotificador real (issue #193):
/// o notificador normaliza o contato para E.164, chama o transporte HTTP e
/// converte falha do provedor em <see cref="OtpEnvioException"/> com motivo
/// amigável, sem detalhe técnico (ver edge point do card).
/// </summary>
public sealed class WhatsAppNotificadorTests
{
    [Fact]
    public async Task EnviarCodigoOtpAsync_ProvedorSucesso_NaoLancaExcecao()
    {
        var httpClient = new FakeWhatsAppHttpClientSucesso();
        var notificador = new WhatsAppNotificador(httpClient, NullLogger<WhatsAppNotificador>.Instance);

        var acao = () => notificador.EnviarCodigoOtpAsync("+5511987654321", "123456", CancellationToken.None);

        await acao.Should().NotThrowAsync();
        httpClient.MensagensEnviadas.Should().ContainSingle(m => m.NumeroE164 == "+5511987654321");
    }

    [Fact]
    public async Task EnviarCodigoOtpAsync_ProvedorFalha_LancaOtpEnvioExceptionComMotivoAmigavelSemDetalheTecnico()
    {
        var notificador = new WhatsAppNotificador(
            new FakeWhatsAppHttpClientFalha(),
            NullLogger<WhatsAppNotificador>.Instance);

        var acao = () => notificador.EnviarCodigoOtpAsync("+5511987654321", "123456", CancellationToken.None);

        var excecao = await acao.Should().ThrowAsync<OtpEnvioException>();
        // Motivo amigável, sem detalhe técnico do provedor; causa original
        // preservada apenas para diagnóstico interno, não serializada em log.
        excecao.And.Motivo.Should().Be("Não foi possível enviar o código. Tente novamente em instantes.");
        excecao.And.CausaOriginal.Should().BeOfType<HttpRequestException>();
    }
}
