namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Desfecho de <see cref="WebhookMercadoPagoService.ProcessarEventoAsync"/>,
/// usado pelo controller para decidir o status HTTP e logar os eventos
/// estruturados — o Domain não injeta <c>ILogger</c> (ver
/// task.md#inconsistências-encontradas, item 4, e
/// implementation.md#logs-estruturados). Todo desfecho válido (confirmado,
/// estornado, ignorado, não encontrado) resulta em HTTP 200 pro Mercado
/// Pago; a diferença entre os tipos é o que o controller loga.
/// </summary>
/// <param name="Tipo">O que aconteceu com o pagamento local (<see
/// cref="TipoProcessamentoWebhook"/>).</param>
/// <param name="DataId">O <c>data.id</c> do evento do Mercado Pago.</param>
/// <param name="PagamentoId">Nosso <see cref="Pagamento.Id"/> —
/// <c>null</c> quando o pagamento não foi encontrado.</param>
/// <param name="Valor">Valor do pagamento — presente só no desfecho de
/// estorno, para o log <c>PagamentoEstornado</c>.</param>
public sealed record ResultadoProcessamentoWebhook(
    TipoProcessamentoWebhook Tipo,
    string DataId,
    Guid? PagamentoId,
    decimal? Valor);

/// <summary>
/// O que <see cref="WebhookMercadoPagoService.ProcessarEventoAsync"/> fez com
/// o pagamento local (issue #200). O controller mapeia para o evento de log
/// estruturado correspondente (ver implementation.md#logs-estruturados).
/// </summary>
public enum TipoProcessamentoWebhook
{
    /// <summary>
    /// Evento <c>approved</c> confirmou o pagamento — log
    /// <c>WebhookPagamentoRecebido</c>.
    /// </summary>
    Confirmado,

    /// <summary>
    /// Evento <c>refunded</c>/<c>rejected</c> estornou um pagamento
    /// Confirmado — log <c>PagamentoEstornado</c>.
    /// </summary>
    Estornado,

    /// <summary>
    /// Pagamento local não encontrado pro <c>external_reference</c> — log de
    /// aviso <c>PagamentoNaoEncontrado</c>, 200 pro MP (ver
    /// implementation.md#fluxo, passo 6).
    /// </summary>
    NaoEncontrado,

    /// <summary>
    /// Evento sem efeito: status não relevante (<c>pending</c>/<c>in_process</c>),
    /// evento já processado antes (idempotência por <c>EventoId</c>), ou
    /// <c>refunded</c>/<c>rejected</c> num pagamento não-Confirmado. 200 sem
    /// log específico.
    /// </summary>
    Ignorado,
}
