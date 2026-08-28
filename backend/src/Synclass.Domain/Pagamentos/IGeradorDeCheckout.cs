namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Contrato de criação de preferência de checkout no Mercado Pago (issue
/// #199) — implementado em Infrastructure via <c>HttpClient</c> puro
/// (<c>GeradorDeCheckoutMercadoPago</c>), permitindo que o Domain e seus
/// testes de unidade não dependam de rede. Sempre o marketplace mode
/// (ADR-0005): o <paramref name="collectorId"/> identifica a conta do
/// Professor que recebe, nunca a conta fixa do Synclass.
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
}

/// <summary>
/// Resultado da criação da preferência de checkout no Mercado Pago:
/// <see cref="UrlCheckout"/> (init_point) é para onde o Aluno navega pra
/// pagar e <see cref="ReferenciaExterna"/> é o id da preferência (o que o
/// webhook de #200 casa com o <see cref="Pagamento"/>).
/// </summary>
public sealed record ResultadoCheckout(string UrlCheckout, string ReferenciaExterna);
