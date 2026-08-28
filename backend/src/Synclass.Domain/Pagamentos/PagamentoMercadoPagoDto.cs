using System.Text.Json.Serialization;

namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Retorno de <c>GET /v1/payments/{id}</c> no Mercado Pago, usado pelo
/// webhook de #200 (issue #200) para descobrir o status e o
/// <c>external_reference</c> de um pagamento notificado. <see cref="Id"/> é
/// o <c>data.id</c> do webhook (o payment id do Mercado Pago);
/// <see cref="Status"/> é um dos valores do MP (<c>approved</c>/<c>rejected</c>/
/// <c>pending</c>/<c>refunded</c>, entre outros); <see cref="ExternalReference"/>
/// é o nosso <see cref="Pagamento.Id"/> (o id passado na criação da
/// preferência de checkout, ver
/// <c>GeradorDeCheckoutMercadoPago.CriarPreferenciaAsync</c>).
/// </summary>
public sealed record PagamentoMercadoPagoDto
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("external_reference")]
    public string? ExternalReference { get; init; }
}
