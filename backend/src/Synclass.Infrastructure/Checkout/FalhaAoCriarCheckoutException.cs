namespace Synclass.Infrastructure.Checkout;

/// <summary>
/// Lançada por <see cref="GeradorDeCheckoutMercadoPago"/> quando a criação
/// da preferência de checkout falha (issue #199) — resposta não-2xx do
/// Mercado Pago, falha de rede/timeout, ou resposta 2xx com corpo fora do
/// formato esperado (sem <c>init_point</c>/<c>id</c>). O controller a captura
/// e responde 502, sem vazar o corpo cru do erro. Mesmo padrão de
/// <see cref="Synclass.Infrastructure.Http.MercadoPagoApiException"/>.
/// </summary>
public sealed class FalhaAoCriarCheckoutException : Exception
{
    public FalhaAoCriarCheckoutException(string mensagem, Exception innerException)
        : base(mensagem, innerException)
    {
    }
}
