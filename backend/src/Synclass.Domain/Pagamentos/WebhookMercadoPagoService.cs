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

        byte[] hashRecebido;
        try
        {
            hashRecebido = Convert.FromHexString(hashHex);
        }
        catch (FormatException)
        {
            // v1= não é hex válido (comprimento ímpar ou caractere fora de
            // 0-9a-fA-F) — payload/header adulterado ou malformado, mesmo
            // tratamento de qualquer outra assinatura inválida (dev-review
            // do PR #209: antes propagava sem tratamento e virava 500 em vez
            // do 400 documentado).
            throw new AssinaturaInvalidaException();
        }

        // Comparação em tempo constante (slow-equals) — não vaza informação
        // sobre o valor do hash por timing (security-rules.md).
        var confere = hashCalculado.Length == hashRecebido.Length
                      && CryptographicOperations.FixedTimeEquals(hashCalculado, hashRecebido);
        return Task.FromResult(confere);
    }

    /// <summary>
    /// Aplica as transições de estado no <see cref="Pagamento"/> local a
    /// partir do evento do Mercado Pago (issue #200), e persiste via
    /// <see cref="IPagamentoRepository.AtualizarAsync"/>. Recebe o DTO do
    /// <c>GET /v1/payments/{data.id}</c> (já buscado pelo controller) — o
    /// <see cref="PagamentoMercadoPagoDto.ExternalReference"/> é o nosso
    /// <see cref="Pagamento.Id"/>, e só após o GET é que esse valor existe
    /// (ver implementation.md#fluxo, passos 5-6). Sempre devolve um
    /// <see cref="ResultadoProcessamentoWebhook"/> (não lança pra pagamento
    /// inexistente — o controller responde 200 e loga aviso em vez de pedir
    /// reenvio ao MP, ver implementation.md#edge-points, item 1). A guarda de
    /// idempotência (<see cref="Pagamento.EventoId"/> == <c>data.id</c>)
    /// acontece ANTES de qualquer transição, <c>AtualizarAsync</c> ou log do
    /// controller — um evento já visto não tem efeito nenhum.
    /// </summary>
    public async Task<ResultadoProcessamentoWebhook> ProcessarEventoAsync(
        PagamentoMercadoPagoDto pagamentoMercadoPago, CancellationToken ct)
    {
        var pagamento = await LocalizarPagamentoAsync(pagamentoMercadoPago, ct);
        if (pagamento is null)
        {
            return new ResultadoProcessamentoWebhook(
                TipoProcessamentoWebhook.NaoEncontrado, pagamentoMercadoPago.Id, null, null);
        }

        // Idempotência: mesmo evento já processado antes — NÃO aplicar
        // transição, NÃO AtualizarAsync, NÃO logar de novo (guarda ANTES de
        // qualquer efeito, ver implementation.md#idempotência).
        if (pagamento.EventoId == pagamentoMercadoPago.Id)
        {
            return new ResultadoProcessamentoWebhook(
                TipoProcessamentoWebhook.Ignorado, pagamentoMercadoPago.Id, pagamento.Id, null);
        }

        return await AplicarTransicaoAsync(pagamento, pagamentoMercadoPago, ct);
    }

    /// <summary>
    /// Localiza o <see cref="Pagamento"/> local pelo <c>external_reference</c>
    /// do evento (issue #200). <see langword="null"/> quando o valor não é
    /// um <see cref="Guid"/> válido ou quando não existe pagamento com esse
    /// id — os dois casos são "não encontrado" pro chamador.
    /// </summary>
    private async Task<Pagamento?> LocalizarPagamentoAsync(
        PagamentoMercadoPagoDto pagamentoMercadoPago, CancellationToken ct)
    {
        if (!Guid.TryParse(pagamentoMercadoPago.ExternalReference, out var pagamentoId))
        {
            return null;
        }

        return await _pagamentos.ObterPorIdAsync(pagamentoId, ct);
    }

    /// <summary>
    /// Aplica a transição de estado correspondente ao <c>status</c> do
    /// evento (issue #200) — <c>approved</c> confirma, <c>refunded</c>/
    /// <c>rejected</c> estorna (se estava <c>Confirmado</c>), qualquer outro
    /// status é no-op. Sempre registra <see cref="Pagamento.EventoId"/> e
    /// persiste, exceto no caso "outros status" (sem efeito a persistir).
    /// </summary>
    private async Task<ResultadoProcessamentoWebhook> AplicarTransicaoAsync(
        Pagamento pagamento, PagamentoMercadoPagoDto pagamentoMercadoPago, CancellationToken ct)
    {
        var status = pagamentoMercadoPago.Status;
        if (status == "approved")
        {
            pagamento.Confirmar(_clock);
            return await RegistrarEPersistirAsync(
                pagamento, pagamentoMercadoPago.Id, TipoProcessamentoWebhook.Confirmado, ct);
        }

        if (status is "refunded" or "rejected")
        {
            // Só estorna de fato se o pagamento tinha sido Confirmado antes;
            // nos demais estados é no-op. De qualquer forma EventoId é
            // sobrescrito, pra reentrega desse MESMO evento não reprocessar.
            var estavaConfirmado = pagamento.Status == StatusPagamento.Confirmado;
            if (estavaConfirmado)
            {
                pagamento.Estornar(_clock);
            }

            var tipo = estavaConfirmado ? TipoProcessamentoWebhook.Estornado : TipoProcessamentoWebhook.Ignorado;
            return await RegistrarEPersistirAsync(pagamento, pagamentoMercadoPago.Id, tipo, ct);
        }

        // Outros status (pending, in_process, etc.): no-op total — não houve
        // efeito a proteger contra reentrega; o MP reenviará quando o status
        // mudar de verdade (implementation.md#fluxo, passo 6). Não registra
        // EventoId nem persiste: não houve efeito nenhum a proteger.
        return new ResultadoProcessamentoWebhook(
            TipoProcessamentoWebhook.Ignorado, pagamentoMercadoPago.Id, pagamento.Id, pagamento.Valor);
    }

    /// <summary>
    /// Sobrescreve <see cref="Pagamento.EventoId"/> com o evento atual,
    /// persiste via <see cref="IPagamentoRepository.AtualizarAsync"/>, e
    /// monta o <see cref="ResultadoProcessamentoWebhook"/> do desfecho —
    /// último passo comum aos ramos <c>approved</c>/<c>refunded</c>/
    /// <c>rejected</c> de <see cref="AplicarTransicaoAsync"/>.
    /// </summary>
    private async Task<ResultadoProcessamentoWebhook> RegistrarEPersistirAsync(
        Pagamento pagamento, string dataId, TipoProcessamentoWebhook tipo, CancellationToken ct)
    {
        pagamento.RegistrarEventoId(dataId);
        await _pagamentos.AtualizarAsync(pagamento, ct);
        return new ResultadoProcessamentoWebhook(tipo, dataId, pagamento.Id, pagamento.Valor);
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

        var campos = AnalisarCamposDoHeader(xSignatureHeader);
        if (!campos.TryGetValue("v1", out var v1) || string.IsNullOrEmpty(v1)
            || !campos.TryGetValue("ts", out var tsTexto)
            || !long.TryParse(tsTexto, NumberStyles.None, CultureInfo.InvariantCulture, out var ts))
        {
            throw new AssinaturaInvalidaException();
        }

        return (ts, v1);
    }

    /// <summary>
    /// Separa o header <c>x-signature</c> (<c>chave=valor</c> separados por
    /// vírgula) em um dicionário — usado por <see cref="ExtrairParametrosDoHeader"/>
    /// para ler <c>ts</c>/<c>v1</c> sem depender da ordem dos campos.
    /// </summary>
    private static Dictionary<string, string> AnalisarCamposDoHeader(string xSignatureHeader)
    {
        var campos = new Dictionary<string, string>();
        foreach (var parte in xSignatureHeader.Split(',', StringSplitOptions.TrimEntries))
        {
            var separador = parte.IndexOf('=');
            if (separador > 0)
            {
                campos[parte[..separador]] = parte[(separador + 1)..];
            }
        }

        return campos;
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
