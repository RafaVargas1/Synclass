namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Contrato de criação de preferência de checkout no Mercado Pago (issue
/// #199) — implementado em Infrastructure via <c>HttpClient</c> puro
/// (<c>GeradorDeCheckoutMercadoPago</c>), permitindo que o Domain e seus
/// testes de unidade não dependam de rede. Sempre o marketplace mode
/// (ADR-0005): o <paramref name="collectorId"/> identifica a conta do
/// Professor que recebe, nunca a conta fixa do Synclass. Desde #200 também
/// expõe <see cref="ObterPagamentoAsync"/>, usado pelo webhook de
/// confirmação de pagamento pra descobrir status + <c>external_reference</c>
/// de um pagamento notificado pelo Mercado Pago (ver
/// implementation.md#fluxo-completo-do-endpoint-sequência, passo 5).
/// </summary>
public interface IGeradorDeCheckout
{
    Task<ResultadoCheckout> CriarPreferenciaAsync(
        Guid professorId,
        string collectorId,
        decimal valor,
        string descricao,
        string externalReference,
        CancellationToken ct);

    /// <summary>
    /// Busca o status de um pagamento no Mercado Pago (<c>GET
    /// /v1/payments/{paymentId}</c>) — o <c>data.id</c> do webhook de #200.
    /// <see langword="null"/> quando o Mercado Pago responde erro (sem
    /// pagamento utilizável). Exceções de rede (timeout) propagam para o
    /// chamador tratar como falha transitória (implementation.md, edge point 4).
    /// </summary>
    Task<PagamentoMercadoPagoDto?> ObterPagamentoAsync(string paymentId, CancellationToken ct);
}

/// <summary>
/// Resultado da criação da preferência de checkout no Mercado Pago:
/// <see cref="UrlCheckout"/> (init_point) é para onde o Aluno navega pra
/// pagar e <see cref="ReferenciaExterna"/> é o id da preferência (o que o
/// webhook de #200 casa com o <see cref="Pagamento"/>).
/// </summary>
public sealed record ResultadoCheckout(string UrlCheckout, string ReferenciaExterna);
