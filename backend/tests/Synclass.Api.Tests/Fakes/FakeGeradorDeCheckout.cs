using Synclass.Domain.Pagamentos;

namespace Synclass.Api.Tests.Fakes;

/// <summary>
/// Fake de <see cref="IGeradorDeCheckout"/> usado nos testes de fumaça da
/// Api (issue #199): não faz a chamada real ao Mercado Pago, só devolve um
/// resultado fixo configurável por teste — mesmo padrão de
/// <see cref="FakeClienteOAuthMercadoPago"/>. Sem isso, o endpoint de
/// pagamento dispararia HTTP real contra a Api do Mercado Pago no teste.
/// </summary>
public sealed class FakeGeradorDeCheckout : IGeradorDeCheckout
{
    public FakeGeradorDeCheckout()
    {
        Resultado = new ResultadoCheckout("https://checkout.mercadopago.com/pref-teste", "pref-teste");
    }

    /// <summary>
    /// Resultado devolvido por <see cref="CriarPreferenciaAsync"/> —
    /// configurável por teste.
    /// </summary>
    public ResultadoCheckout Resultado { get; set; }

    /// <summary>
    /// O id do <see cref="Pagamento"/> passado como <c>externalReference</c>
    /// na última chamada — exposto para o teste confirmar que o pagamento
    /// criado foi o mesmo usado no payload de checkout.
    /// </summary>
    public string? UltimaExternalReference { get; private set; }

    public Task<ResultadoCheckout> CriarPreferenciaAsync(
        Guid professorId,
        string collectorId,
        decimal valor,
        string descricao,
        string externalReference,
        CancellationToken ct)
    {
        UltimaExternalReference = externalReference;
        return Task.FromResult(Resultado);
    }
}
