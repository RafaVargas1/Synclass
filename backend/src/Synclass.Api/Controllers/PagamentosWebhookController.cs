using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Pagamentos;
using Synclass.Infrastructure.Checkout;

namespace Synclass.Api.Controllers;

/// <summary>
/// Endpoint de webhook de confirmação de pagamento (issue #200):
/// <c>POST /webhooks/mercado-pago</c>, consumido exclusivamente pelo Mercado
/// Pago (é a <c>notification_url</c> enviada na criação da preferência de
/// checkout desde #199). Nenhum frontend toca este endpoint de propósito. O
/// fluxo segue implementation.md#fluxo-completo-do-endpoint-sequência: (1)
/// valida a assinatura HMAC-SHA256 do header <c>x-signature</c>, (2) busca o
/// pagamento no Mercado Pago via <c>GET /v1/payments/{data.id}</c>
/// (reutilizando o <c>HttpClient</c>/Bearer do
/// <see cref="GeradorDeCheckoutMercadoPago"/>) para descobrir status e
/// <c>external_reference</c>, (3) só então aplica a transição no
/// <see cref="Pagamento"/> local via
/// <see cref="WebhookMercadoPagoService.ProcessarEventoAsync"/>. O controller
/// é quem loga os eventos estruturados — o Domain não injeta <c>ILogger</c>
/// (padrão do repo, ver task.md#inconsistências-encontradas, item 4).
/// </summary>
[ApiController]
[Route("webhooks/mercado-pago")]
public sealed class PagamentosWebhookController : ControllerBase
{
    private readonly WebhookMercadoPagoService _webhookService;
    private readonly IGeradorDeCheckout _geradorDeCheckout;
    private readonly ILogger<PagamentosWebhookController> _logger;

    public PagamentosWebhookController(
        WebhookMercadoPagoService webhookService,
        IGeradorDeCheckout geradorDeCheckout,
        ILogger<PagamentosWebhookController> logger)
    {
        _webhookService = webhookService;
        _geradorDeCheckout = geradorDeCheckout;
        _logger = logger;
    }

    /// <summary>
    /// Recebe a notificação de pagamento do Mercado Pago. <c>[AllowAnonymous]</c>
    /// é INTENCIONAL: este é um endpoint público AUTENTICADO via assinatura
    /// HMAC-SHA256 verificada no <see cref="WebhookMercadoPagoService"/>, NÃO
    /// via sessão de usuário — o Mercado Pago não carrega um JWT do Synclass
    /// na notificação. NÃO troque para <c>[Authorize]</c> sem reavaliar a
    /// verificação de assinatura primeiro (um futuro refactor faria o webhook
    /// silenciosamente quebrar; ver implementation.md#contrato-de-api). A rota
    /// é fixa <c>POST /webhooks/mercado-pago</c> (a <c>notification_url</c> já
    /// em produção desde #199 — não pode mudar). O <c>data.id</c> vem da query
    /// string (<c>?type=payment&amp;data.id=</c>, formato IPN v2); o corpo JSON
    /// carrega o mesmo id mas não é a fonte de verdade implementada aqui. Sem
    /// body na resposta (o MP só agradece o ACK), 200 mesmo quando o evento é
    /// no-op.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> ReceberWebhook(CancellationToken cancellationToken)
    {
        var dataId = Request.Query["data.id"].ToString();
        var xSignature = Request.Headers["x-signature"].ToString();
        var xRequestId = Request.Headers["x-request-id"].ToString();

        // Payload BRUTO recebido — a assinatura é calculada sobre o texto
        // exato, nunca sobre JSON re-serializado (implementation.md#fluxo,
        // passo 3). Lido sempre: o webhook pode vir sem body (só query), e o
        // serviço valida o corpo na verificação da assinatura.
        var payloadJson = await LerCorpoBrutoAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(dataId))
        {
            // Evento inutilizável: 400 (não 200) para o MP não reenviar algo
            // que nunca vai funcionar (implementation.md, edge point 2).
            LogarRejeitado(dataId: null, motivo: "data.id ausente");
            return BadRequest();
        }

        bool assinaturaValida;
        try
        {
            assinaturaValida = await _webhookService.VerificarAssinaturaAsync(
                payloadJson, xSignature, xRequestId, cancellationToken);
        }
        catch (AssinaturaInvalidaException)
        {
            assinaturaValida = false;
        }

        if (!assinaturaValida)
        {
            // 400 genérico — assinatura não confere, header ausente/malformado
            // ou payload adulterado. Interrompe ANTES de buscar o pagamento
            // local (implementation.md, passo 4).
            LogarRejeitado(dataId, motivo: "assinatura inválida ou ausente");
            return BadRequest();
        }

        // Passo 5: GET /v1/payments/{data.id} descobre status +
        // external_reference (= nosso Pagamento.Id). null = falha da chamada
        // (rede/404/401) — não confirmar nem estornar nada; 500 pro MP reenviar
        // (implementation.md, edge point 4). NÃO é o "pagamento local não
        // encontrado" (isso o ProcessarEventoAsync sinaliza e vira 200 + aviso).
        var pagamentoMercadoPago = await _geradorDeCheckout.ObterPagamentoAsync(dataId, cancellationToken);
        if (pagamentoMercadoPago is null)
        {
            LogarRejeitado(dataId, motivo: "GET v1/payments falhou");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        var resultado = await _webhookService.ProcessarEventoAsync(pagamentoMercadoPago, cancellationToken);
        LogarProcessamento(resultado);
        // 200 sem body — pro MP agradecer o ACK; todo desfecho do
        // ProcessarEventoAsync (confirmado, estornado, ignorado, não encontrado)
        // é 200 (implementation.md#contrato-de-api e edge point 1).
        return Ok();
    }

    /// <summary>
    /// Lê o corpo cru da requisição como string para a verificação de
    /// assinatura. Mesmo que o corpo venha em branco (o <c>data.id</c> vem da
    /// query), precisa ser lido — a assinatura HMAC é calculada sobre o texto
    /// exato recebido.
    /// </summary>
    private async Task<string> LerCorpoBrutoAsync(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    /// <summary>
    /// Log estruturado do evento <c>WebhookPagamentoRejeitado</c> (issue
    /// #200), com <c>TrackId</c> e o motivo — nunca o payload bruto nem o
    /// <c>x-signature</c> completo (segredo, ver security-rules.md).
    /// </summary>
    private void LogarRejeitado(string? dataId, string motivo)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        _logger.LogWarning(
            "WebhookPagamentoRejeitado {TrackId} {DataId} {Motivo}",
            trackId, dataId ?? "ausente", motivo);
    }

    /// <summary>
    /// Log estruturado dos desfechos de <see cref="ResultadoProcessamentoWebhook"/>
    /// (issue #200) — a camada do controller é a única que loga (o Domain não
    /// injeta <c>ILogger</c>). Mapeia cada <see cref="TipoProcessamentoWebhook"/>
    /// para o evento definido em implementation.md#logs-estruturados;
    /// <see cref="TipoProcessamentoWebhook.Ignorado"/> não loga (200 sem efeito).
    /// </summary>
    private void LogarProcessamento(ResultadoProcessamentoWebhook resultado)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();

        switch (resultado.Tipo)
        {
            case TipoProcessamentoWebhook.Confirmado:
                _logger.LogInformation(
                    "WebhookPagamentoRecebido {TrackId} {DataId} {PagamentoId} {StatusPagamento}",
                    trackId, resultado.DataId, resultado.PagamentoId, "approved");
                break;
            case TipoProcessamentoWebhook.Estornado:
                _logger.LogInformation(
                    "PagamentoEstornado {TrackId} {PagamentoId} {Valor}",
                    trackId, resultado.PagamentoId, resultado.Valor);
                break;
            case TipoProcessamentoWebhook.NaoEncontrado:
                // Webhook órfão (external_reference não casa com Pagamento
                // local): aviso, mas resposta 200 para o MP não reenviar algo
                // que nunca vai resolver (implementation.md, edge point 1).
                _logger.LogWarning(
                    "PagamentoNaoEncontrado {TrackId} {DataId}",
                    trackId, resultado.DataId);
                break;
            case TipoProcessamentoWebhook.Ignorado:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(resultado), resultado.Tipo, null);
        }
    }
}
