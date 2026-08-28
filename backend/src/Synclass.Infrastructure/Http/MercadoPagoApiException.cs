namespace Synclass.Infrastructure.Http;

/// <summary>
/// Lançada por <see cref="ClienteOAuthMercadoPago"/> quando o Mercado Pago
/// responde com erro (status <c>4xx</c>/<c>5xx</c>) na troca de <c>code</c>
/// por token ou na renovação via <c>refresh_token</c> (issue #203) — ver
/// implementation.md#exceções-novas-em-camada-de-infraestrutura. O
/// <see cref="Synclass.Api.Controllers.MercadoPagoController"/> a captura e
/// responde <c>502 Bad Gateway</c>, sem vazar o corpo cru do erro (que pode
/// conter token).
/// </summary>
public sealed class MercadoPagoApiException : Exception
{
    public MercadoPagoApiException(string mensagem, Exception innerException)
        : base(mensagem, innerException)
    {
    }
}
