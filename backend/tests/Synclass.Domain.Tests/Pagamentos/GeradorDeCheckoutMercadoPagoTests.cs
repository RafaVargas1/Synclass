using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Synclass.Domain.Pagamentos;
using Synclass.Infrastructure.Checkout;

namespace Synclass.Domain.Tests.Pagamentos;

/// <summary>
/// Cobre <see cref="GeradorDeCheckoutMercadoPago"/> (issue #199/#200) contra
/// um <see cref="HttpMessageHandler"/> fake — sem rede real. Verifica o
/// payload JSON de criação de preferência (sem o bloco <c>payer</c>, campo
/// opcional — ver task.md#inconsistências-encontradas, item 7), o header de
/// autenticação, o parse da resposta (<c>init_point</c> e <c>id</c> da
/// preferência) e o erro quando a resposta não é parseável ou é não-2xx. A
/// partir de #200 cobre também <see cref="GeradorDeCheckoutMercadoPago.ObterPagamentoAsync"/>:
/// GET <c>/v1/payments/{id}</c> no MESMO HttpClient/Bearer, parse do DTO e
/// <c>null</c> em resposta não-2xx. O header <c>Authorization: Bearer</c> e
/// o <c>BaseAddress</c> vêm do contêiner (Program.cs, <c>AddHttpClient</c>),
/// não da classe — o teste os configura no <see cref="HttpClient"/> de teste
/// para refletir esse ambiente (mesmo padrão de teste HTTP fake de
/// WhatsAppHttpClientTests, docs/spec/code-style.md#dependências).
/// </summary>
public sealed class GeradorDeCheckoutMercadoPagoTests
{
    private const string AccessToken = "APP_USR-access-token-de-teste";
    private const string UrlBaseApp = "https://app.synclass.com.br";
    private const string ApiBaseUrlPublica = "https://api.synclass.com.br";
    private static readonly Uri BaseAddress = new("https://api.mercadopago.com");

    [Fact]
    public async Task CriarPreferenciaAsync_SerializaPayloadCorretoEOrdenaUrlCheckout()
    {
        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.Created,
            "{\"id\":\"pref-123\",\"init_point\":\"https://checkout.mercadopago.com/pref-123\"}");
        var httpClient = CriarHttpClient(handler);
        var gerador = new GeradorDeCheckoutMercadoPago(httpClient, CriarConfiguracao(), NullLogger<GeradorDeCheckoutMercadoPago>.Instance);

        var resultado = await gerador.CriarPreferenciaAsync(
            professorId: Guid.NewGuid(),
            collectorId: "collector-987",
            valor: 120.00m,
            descricao: "Aula particular — 2026-08-01 a 2026-09-01",
            externalReference: "66a6e4c8-0000-0000-0000-000000000000",
            CancellationToken.None);

        handler.RequisicaoCapturada!.Method.Should().Be(HttpMethod.Post);
        handler.RequisicaoCapturada.RequestUri!.ToString().Should().EndWith("/checkout/preferences");
        handler.RequisicaoCapturada.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.RequisicaoCapturada.Headers.Authorization.Parameter.Should().Be(AccessToken);

        var payload = JsonDocument.Parse(handler.PayloadCapturado!).RootElement;
        payload.GetProperty("auto_return").GetString().Should().Be("approved");
        payload.GetProperty("external_reference").GetString().Should().Be("66a6e4c8-0000-0000-0000-000000000000");
        payload.GetProperty("collector_id").GetString().Should().Be("collector-987");

        var item = payload.GetProperty("items").EnumerateArray().Single();
        item.GetProperty("title").GetString().Should().Be("Aula particular — 2026-08-01 a 2026-09-01");
        item.GetProperty("quantity").GetInt32().Should().Be(1);
        item.GetProperty("unit_price").GetDecimal().Should().Be(120.00m);
        item.GetProperty("currency_id").GetString().Should().Be("BRL");

        var backUrls = payload.GetProperty("back_urls");
        backUrls.GetProperty("success").GetString().Should().Be($"{UrlBaseApp}/aluno/pagamento/confirmado");
        backUrls.GetProperty("failure").GetString().Should().Be($"{UrlBaseApp}/aluno/pagamento/falhou");
        backUrls.GetProperty("pending").GetString().Should().Be($"{UrlBaseApp}/aluno/pagamento/pendente");

        payload.GetProperty("notification_url").GetString().Should().Be($"{ApiBaseUrlPublica}/webhooks/mercado-pago");

        // Campo payer é opcional no Checkout Pro e o fluxo de IniciarAsync não
        // transporta o email do Aluno (task.md#inconsistências-encontradas, item 7).
        payload.TryGetProperty("payer", out _).Should().BeFalse();

        // Parse da resposta: init_point vira a URL do checkout e id vira a
        // referência externa (o que #200 casa com o Pagamento).
        resultado.UrlCheckout.Should().Be("https://checkout.mercadopago.com/pref-123");
        resultado.ReferenciaExterna.Should().Be("pref-123");
    }

    /// <summary>
    /// Resposta 2xx mas com <c>init_point</c> ausente — contrato quebrado do
    /// Mercado Pago. O erro não vaza cru pro chamador.
    /// </summary>
    [Fact]
    public async Task CriarPreferenciaAsync_RespostaSemInitPoint_LancaFalhaAoCriarCheckoutException()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Created, "{\"id\":\"pref-123\"}");
        var httpClient = CriarHttpClient(handler);
        var gerador = new GeradorDeCheckoutMercadoPago(httpClient, CriarConfiguracao(), NullLogger<GeradorDeCheckoutMercadoPago>.Instance);

        var acao = () => gerador.CriarPreferenciaAsync(
            Guid.NewGuid(), "collector-987", 120m, "Aula particular", "ext-ref", CancellationToken.None);

        var excecao = await acao.Should().ThrowAsync<FalhaAoCriarCheckoutException>();
        excecao.Which.Message.Should().Contain("preferência de checkout");
    }

    /// <summary>
    /// Resposta não-2xx do Mercado Pago — erro de autenticação ou de payload.
    /// </summary>
    [Fact]
    public async Task CriarPreferenciaAsync_RespostaNao2xx_LancaFalhaAoCriarCheckoutException()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized, "{\"message\":\"Unauthorized\"}");
        var httpClient = CriarHttpClient(handler);
        var gerador = new GeradorDeCheckoutMercadoPago(httpClient, CriarConfiguracao(), NullLogger<GeradorDeCheckoutMercadoPago>.Instance);

        var acao = () => gerador.CriarPreferenciaAsync(
            Guid.NewGuid(), "collector-987", 120m, "Aula particular", "ext-ref", CancellationToken.None);

        var excecao = await acao.Should().ThrowAsync<FalhaAoCriarCheckoutException>();
        excecao.Which.Message.Should().Contain("preferência de checkout");
    }

    [Fact]
    public async Task ObterPagamentoAsync_Sucesso_ParseiaDtoComIdStatusEExternalReference()
    {
        var corpo = "{\"id\":\"123456789\",\"status\":\"approved\",\"external_reference\":\"66a6e4c8-0000-0000-0000-000000000000\"}";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, corpo);
        var httpClient = CriarHttpClient(handler);
        var gerador = new GeradorDeCheckoutMercadoPago(httpClient, CriarConfiguracao(), NullLogger<GeradorDeCheckoutMercadoPago>.Instance);

        var dto = await gerador.ObterPagamentoAsync("123456789", CancellationToken.None);

        dto.Should().NotBeNull();
        dto!.Id.Should().Be("123456789");
        dto.Status.Should().Be("approved");
        dto.ExternalReference.Should().Be("66a6e4c8-0000-0000-0000-000000000000");

        // Reutiliza o MESMO HttpClient/Bearer do checkout: GET para o endpoint
        // de payment, com o Authorization vindo dos DefaultRequestHeaders do
        // contêiner (não adicionado manualmente pelo método, mas presente na
        // requisição enviada — prova que reusa o token já configurado).
        handler.RequisicaoCapturada!.Method.Should().Be(HttpMethod.Get);
        handler.RequisicaoCapturada.RequestUri!.ToString().Should().EndWith("/v1/payments/123456789");
        handler.RequisicaoCapturada.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.RequisicaoCapturada.Headers.Authorization.Parameter.Should().Be(AccessToken);
    }

    [Fact]
    public async Task ObterPagamentoAsync_RespostaNao2xx_RetornaNull()
    {
        // 401 (token master não autorizado no marketplace mode) ou 404
        // (pagamento inexistente) — sem pagamento utilizável, o método devolve
        // null e o chamador do webhook decide como tratar (ver implementation.md).
        var handler = new FakeHttpMessageHandler(HttpStatusCode.NotFound, "{\"message\":\"Payment not found\"}");
        var httpClient = CriarHttpClient(handler);
        var gerador = new GeradorDeCheckoutMercadoPago(httpClient, CriarConfiguracao(), NullLogger<GeradorDeCheckoutMercadoPago>.Instance);

        var dto = await gerador.ObterPagamentoAsync("123456789", CancellationToken.None);

        dto.Should().BeNull();
    }

    /// <summary>
    /// Monta o <see cref="HttpClient"/> como o <c>AddHttpClient</c> do
    /// Program.cs faz: <c>BaseAddress</c> na api do Mercado Pago e header
    /// <c>Authorization: Bearer</c> com o AccessToken da aplicação (o mesmo
    /// token que criou o OAuth em #203, não o token do Professor).
    /// </summary>
    private static HttpClient CriarHttpClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = BaseAddress };
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        return httpClient;
    }

    private static IConfiguration CriarConfiguracao()
    {
        return new FakeConfiguration(new Dictionary<string, string?>
        {
            ["MercadoPago:AccessToken"] = AccessToken,
            ["MercadoPago:UrlBaseApp"] = UrlBaseApp,
            ["MercadoPago:ApiBaseUrlPublica"] = ApiBaseUrlPublica,
        });
    }

    /// <summary>
    /// Implementação mínima de <see cref="IConfiguration"/> para teste — só o
    /// indexador (usado pelo ctor de <see cref="GeradorDeCheckoutMercadoPago"/>).
    /// Mesmo padrão de WhatsAppHttpClientTests.FakeConfiguration
    /// (docs/spec/code-style.md#dependências): evita depender do pacote
    /// Microsoft.Extensions.Configuration neste projeto de teste.
    /// </summary>
    private sealed class FakeConfiguration : IConfiguration
    {
        private readonly IReadOnlyDictionary<string, string?> _valores;

        public FakeConfiguration(IReadOnlyDictionary<string, string?> valores)
        {
            _valores = valores;
        }

        public string? this[string key]
        {
            get => _valores.TryGetValue(key, out var valor) ? valor : null;
            set => throw new NotSupportedException();
        }

        public IEnumerable<IConfigurationSection> GetChildren() => Enumerable.Empty<IConfigurationSection>();

        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => throw new NotSupportedException();

        public IConfigurationSection GetSection(string key) => throw new NotSupportedException();
    }

    /// <summary>
    /// Handler HTTP fake que devolve a resposta com o corpo configurado e
    /// captura a requisição. O corpo da requisição (<see cref="PayloadCapturado"/>)
    /// é lido aqui, dentro do <c>SendAsync</c>, antes de o request ser
    /// disposto pelo chamador — ler depois via
    /// <c>RequisicaoCapturada.Content</c> falharia com
    /// <c>ObjectDisposedException</c> (o <c>using</c> no envio já o descartou).
    /// O <c>Content</c> pode ser nulo (GET sem body, ex:
    /// <see cref="GeradorDeCheckoutMercadoPago.ObterPagamentoAsync"/>) — nesse
    /// caso o payload fica nulo.
    /// </summary>
    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _corpo;

        public FakeHttpMessageHandler(HttpStatusCode statusCode, string corpo)
        {
            _statusCode = statusCode;
            _corpo = corpo;
        }

        public HttpRequestMessage? RequisicaoCapturada { get; private set; }

        public string? PayloadCapturado { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequisicaoCapturada = request;
            PayloadCapturado = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_corpo, Encoding.UTF8, "application/json"),
            };
        }
    }
}
