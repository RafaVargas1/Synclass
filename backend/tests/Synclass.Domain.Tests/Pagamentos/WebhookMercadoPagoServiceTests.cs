using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Synclass.Domain.Pagamentos;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Pagamentos;

/// <summary>
/// Cobre <see cref="WebhookMercadoPagoService"/> (issue #200): verificação
/// de assinatura HMAC-SHA256 do webhook do Mercado Pago e o processamento
/// dos eventos (<c>approved</c>/<c>refunded</c>/<c>rejected</c>) com
/// idempotência por <c>EventoId</c> — ver implementation.md#fluxo-completo-do-endpoint-sequência.
/// </summary>
public sealed class WebhookMercadoPagoServiceTests
{
    private const string WebhookSecret = "segredo-de-webhook-teste";
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero));

    private static WebhookMercadoPagoService CriarServico(
        FakePagamentoRepository? pagamentos = null,
        string webhookSecret = WebhookSecret)
    {
        return new WebhookMercadoPagoService(
            webhookSecret,
            pagamentos ?? new FakePagamentoRepository(),
            Clock);
    }

    /// <summary>
    /// Monta o header <c>x-signature</c> no formato documentado do Mercado
    /// Pago (<c>ts=&lt;timestamp&gt;,v1=&lt;hmac&gt;</c>), com o HMAC-SHA256
    /// (chave <c>MercadoPago:WebhookSecret</c>) sobre o manifest
    /// <c>id:{data.id};request-id:{xRequestId};ts:{ts};</c> — ver
    /// implementation.md#verificação-de-assinatura.
    /// </summary>
    private static string MontarXSignature(string dataId, string requestId, string timestamp)
    {
        var manifest = $"id:{dataId};request-id:{requestId};ts:{timestamp};";
        return $"ts={timestamp},v1={CalcularHmacSha256Hex(WebhookSecret, manifest)}";
    }

    private static string CalcularHmacSha256Hex(string chave, string valor)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(chave));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(valor));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    [Fact]
    public async Task VerificarAssinaturaAsync_AssinaturaValida_RetornaTrue()
    {
        var dataId = "123456789";
        var requestId = "req-abc-123";
        var timestamp = "1747353600";
        var payloadJson = $"{{\"type\":\"payment\",\"data\":{{\"id\":\"{dataId}\"}},\"action\":\"payment.created\"}}";
        var xSignature = MontarXSignature(dataId, requestId, timestamp);

        var servico = CriarServico();

        var aceito = await servico.VerificarAssinaturaAsync(
            payloadJson, xSignature, requestId, CancellationToken.None);

        aceito.Should().BeTrue();
    }
}
