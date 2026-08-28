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

    private const string RequestId = "req-abc-123";
    private const string Timestamp = "1747353600";

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

    private static string MontarPayloadJson(string dataId) =>
        $"{{\"type\":\"payment\",\"data\":{{\"id\":\"{dataId}\"}},\"action\":\"payment.created\"}}";

    [Fact]
    public async Task VerificarAssinaturaAsync_AssinaturaValida_RetornaTrue()
    {
        var dataId = "123456789";
        var payloadJson = MontarPayloadJson(dataId);
        var xSignature = MontarXSignature(dataId, RequestId, Timestamp);

        var servico = CriarServico();

        var aceito = await servico.VerificarAssinaturaAsync(
            payloadJson, xSignature, RequestId, CancellationToken.None);

        aceito.Should().BeTrue();
    }

    [Fact]
    public async Task VerificarAssinaturaAsync_PayloadAdulterado_MudaDataId_RetornaFalse()
    {
        // Assinatura calculada para dataId original, mas o payload chega com
        // um data.id diferente (adulteração) — o manifest montado diverge, o
        // HMAC não confere.
        var dataIdAssinado = "123456789";
        var dataIdAdulterado = "987654321";
        var payloadJson = MontarPayloadJson(dataIdAdulterado);
        var xSignature = MontarXSignature(dataIdAssinado, RequestId, Timestamp);

        var servico = CriarServico();

        var aceito = await servico.VerificarAssinaturaAsync(
            payloadJson, xSignature, RequestId, CancellationToken.None);

        aceito.Should().BeFalse();
    }

    [Fact]
    public async Task VerificarAssinaturaAsync_SemXSignature_LancaAssinaturaInvalida()
    {
        var payloadJson = MontarPayloadJson("123456789");

        var servico = CriarServico();

        var acao = async () => await servico.VerificarAssinaturaAsync(
            payloadJson, "", RequestId, CancellationToken.None);

        await acao.Should().ThrowAsync<AssinaturaInvalidaException>();
    }

    [Fact]
    public async Task VerificarAssinaturaAsync_XSignatureMalformada_SemV1_LancaAssinaturaInvalida()
    {
        // Header com apenas ts= (sem o par v1=) — não dá para comparar o hash.
        var dataId = "123456789";
        var payloadJson = MontarPayloadJson(dataId);
        var xSignatureMalformada = $"ts={Timestamp}";

        var servico = CriarServico();

        var acao = async () => await servico.VerificarAssinaturaAsync(
            payloadJson, xSignatureMalformada, RequestId, CancellationToken.None);

        await acao.Should().ThrowAsync<AssinaturaInvalidaException>();
    }
}
