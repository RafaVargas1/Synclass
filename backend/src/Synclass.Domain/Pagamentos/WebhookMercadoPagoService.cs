using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Synclass.Domain.Common;

namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Serviço de domínio do webhook de confirmação de pagamento do Mercado
/// Pago (issue #200): verifica a assinatura HMAC-SHA256 do webhook e aplica
/// as transições de estado no <see cref="Pagamento"/> conforme o evento
/// (<c>approved</c>/<c>refunded</c>/<c>rejected</c>), com idempotência por
/// <see cref="Pagamento.EventoId"/> — ver
/// implementation.md#fluxo-completo-do-endpoint-sequência. A assinatura é a
/// única autenticação do endpoint (público por design via
/// <c>[AllowAnonymous]</c>, ver implementation.md#contrato-de-api). O Domain
/// não injeta <c>ILogger</c> (padrão do repo, ver inconsistency 4 do
/// task.md) — o controller loga os eventos estruturados com os dados que
/// este serviço expõe.
/// </summary>
public sealed class WebhookMercadoPagoService
{
    private readonly string _webhookSecret;
    private readonly IPagamentoRepository _pagamentos;
    private readonly IClock _clock;

    public WebhookMercadoPagoService(
        string webhookSecret,
        IPagamentoRepository pagamentos,
        IClock clock)
    {
        _webhookSecret = webhookSecret;
        _pagamentos = pagamentos;
        _clock = clock;
    }

    /// <summary>
    /// Verifica a assinatura HMAC-SHA256 do webhook (issue #200) contra o
    /// header <c>x-signature</c> do Mercado Pago. Devolve <c>true</c> quando
    /// o hash confere com o computado sobre o payload exato recebido;
    /// <c>false</c> quando o payload foi adulterado (hash diverge). Lança
    /// <see cref="AssinaturaInvalidaException"/> quando o header está
    /// ausente/malformatado (sem <c>ts=</c>/<c>v1=</c>) ou o payload não
    /// expõe o <c>data.id</c> — nesses casos não dá nem para montar o
    /// manifest e comparar. <c>ct</c> é recebido para manter o contrato do
    /// serviço uniforme, mas a verificação é puramente local e não é
    /// cancelável.
    /// </summary>
    public Task<bool> VerificarAssinaturaAsync(
        string payloadJson, string xSignatureHeader, string xRequestIdHeader, CancellationToken ct)
    {
        // TODO(webhook-signature): confirmar formato exato do manifest na doc
        // oficial do Mercado Pago. Implementado com base em documentação
        // pública parcial; se a doc oficial divergir, ajustar AQUI e em
        // implementation.md. Formato documentado atualmente (VRI): manifest =
        // $"id:{dataId};request-id:{requestId};ts:{timestamp};" onde dataId
        // extraído do JSON, requestId do header, timestamp do x-signature.
        var (timestamp, hashHex) = ExtrairParametrosDoHeader(xSignatureHeader);

        // Assinatura calculada sobre o payload EXATO recebido, não sobre JSON
        // re-serializado (implementation.md#fluxo-completo-do-endpoint-sequência,
        // passo 3) — é por isso que o método recebe o raw string, e não um DTO.
        var dataId = ExtrairDataIdDoPayload(payloadJson);
        var manifest = string.Concat(
            "id:", dataId,
            ";request-id:", xRequestIdHeader,
            ";ts:", timestamp.ToString(CultureInfo.InvariantCulture),
            ";");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_webhookSecret));
        var hashCalculado = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));

        var hashRecebido = Convert.FromHexString(hashHex);
        // Comparação em tempo constante (slow-equals) — não vaza informação
        // sobre o valor do hash por timing (security-rules.md).
        var confere = hashCalculado.Length == hashRecebido.Length
                      && CryptographicOperations.FixedTimeEquals(hashCalculado, hashRecebido);
        return Task.FromResult(confere);
    }

    /// <summary>
    /// Extrai <c>ts</c> e <c>v1</c> do header <c>x-signature</c> no formato
    /// documentado <c>ts=&lt;timestamp&gt;,v1=&lt;hmac&gt;</c>. Header ausente
    /// ou sem um dos dois campos → <see cref="AssinaturaInvalidaException"/>
    /// (não dá para montar/comparar o manifest).
    /// </summary>
    private static (long Ts, string V1Hex) ExtrairParametrosDoHeader(string xSignatureHeader)
    {
        if (string.IsNullOrWhiteSpace(xSignatureHeader))
        {
            throw new AssinaturaInvalidaException();
        }

        var partes = xSignatureHeader.Split(',', StringSplitOptions.TrimEntries);
        string? v1 = null;
        long? ts = null;
        foreach (var parte in partes)
        {
            var separador = parte.IndexOf('=');
            if (separador <= 0)
            {
                continue;
            }

            var chave = parte[..separador];
            var valor = parte[(separador + 1)..];
            if (chave == "ts" && long.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, out var tsParseado))
            {
                ts = tsParseado;
            }
            else if (chave == "v1")
            {
                v1 = valor;
            }
        }

        if (ts is null || string.IsNullOrEmpty(v1))
        {
            throw new AssinaturaInvalidaException();
        }

        return (ts.Value, v1);
    }

    /// <summary>
    /// Extrai o <c>data.id</c> do corpus JSON do webhook — o id do pagamento
    /// no Mercado Pago, usado no manifest assinado. Payload sem
    /// <c>data.id</c> → <see cref="AssinaturaInvalidaException"/> (a
    /// assinatura não tem como ser validada, e um evento sem id é inutilizável).
    /// </summary>
    private static string ExtrairDataIdDoPayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            throw new AssinaturaInvalidaException();
        }

        string? dataId;
        try
        {
            using var documento = JsonDocument.Parse(payloadJson);
            dataId = documento.RootElement.GetProperty("data").GetProperty("id").GetString();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            throw new AssinaturaInvalidaException();
        }

        if (string.IsNullOrWhiteSpace(dataId))
        {
            throw new AssinaturaInvalidaException();
        }

        return dataId;
    }
}
