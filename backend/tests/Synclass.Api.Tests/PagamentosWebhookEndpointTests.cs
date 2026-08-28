using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synclass.Api.Tests.Fakes;
using Synclass.Domain.Common;
using Synclass.Domain.Pagamentos;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Api.Tests;

/// <summary>
/// Teste de fumaça do endpoint de webhook de confirmação de pagamento
/// (issue #200): <c>POST /webhooks/mercado-pago</c>. Usa EF Core InMemory e o
/// <see cref="FakeGeradorDeCheckout"/> no lugar do cliente HTTP real do
/// Mercado Pago (não chamar a Api do MP num teste), mesmo padrão de
/// <see cref="PagamentosControllerTests"/>. O <c>MercadoPago:WebhookSecret</c>
/// é fixado via <c>UseSetting</c> para o teste montar a assinatura HMAC-SHA256
/// com o MESMO secret que o <see cref="WebhookMercadoPagoService"/> usa.
/// </summary>
public sealed class PagamentosWebhookEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string WebhookSecret = "segredo-de-webhook-integracao";
    private const string RequestId = "req-webhook-integracao";

    private readonly WebApplicationFactory<Program> _factory;

    public PagamentosWebhookEndpointTests(WebApplicationFactory<Program> factory)
    {
        var nomeDoBancoEmMemoria = Guid.NewGuid().ToString();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RunMigrationsOnStartup", "false");
            builder.UseSetting("MercadoPago:WebhookSecret", WebhookSecret);
            builder.ConfigureServices(services =>
            {
                UseInMemoryDatabase(services, nomeDoBancoEmMemoria);
                UsarGeradorDeCheckoutFixo(services);
            });
        });
    }

    private static void UseInMemoryDatabase(IServiceCollection services, string nomeDoBanco)
    {
        services.RemoveAll<DbContextOptions<SynclassDbContext>>();
        services.AddDbContext<SynclassDbContext>(options =>
            options.UseInMemoryDatabase(nomeDoBanco));
    }

    private static void UsarGeradorDeCheckoutFixo(IServiceCollection services)
    {
        services.RemoveAll<IGeradorDeCheckout>();
        services.AddSingleton<IGeradorDeCheckout>(new FakeGeradorDeCheckout());
    }

    private (HttpClient Client, FakeGeradorDeCheckout Gerador) CriarClienteEGerador()
    {
        var client = _factory.CreateClient();
        var gerador = _factory.Services.GetRequiredService<IGeradorDeCheckout>() as FakeGeradorDeCheckout
            ?? throw new InvalidOperationException("Esperava FakeGeradorDeCheckout registrado.");
        return (client, gerador);
    }

    /// <summary>
    /// Monta o payload JSON do webhook no formato do Mercado Pago IPN v2
    /// (<c>{type: payment, data: {id}}</c>) — o <c>data.id</c> deve casar com o
    /// da query para a assinatura conferir (o manifest assinado inclui
    /// <c>id:{data.id}</c> do payload).
    /// </summary>
    private static string MontarPayloadJson(string dataId) =>
        $"{{\"type\":\"payment\",\"data\":{{\"id\":\"{dataId}\"}}}}";

    private static string MontarXSignature(string payloadJson, string requestId)
    {
        // Extrai o data.id do payload para montar o manifest assinado, no
        // mesmo formato que WebhookMercadoPagoService usa (implementation.md,
        // passo 3): id:{data.id};request-id:{requestId};ts:{timestamp};
        using var documento = JsonDocument.Parse(payloadJson);
        var dataId = documento.RootElement.GetProperty("data").GetProperty("id").GetString()!;
        var timestamp = "1747353600";
        var manifest = $"id:{dataId};request-id:{requestId};ts:{timestamp};";
        return $"ts={timestamp},v1={CalcularHmacSha256Hex(WebhookSecret, manifest)}";
    }

    private static string CalcularHmacSha256Hex(string chave, string valor)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(chave));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(valor));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static async Task<Guid> CriarPagamentoPendenteAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var pagamento = new Pagamento(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            300m,
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 9, 1),
            "https://checkout.mercadopago.com/pref-teste",
            "pref-teste",
            clock);
        dbContext.Pagamentos.Add(pagamento);
        await dbContext.SaveChangesAsync();
        return pagamento.Id;
    }

    private static async Task<Pagamento?> BuscarPagamentoAsync(WebApplicationFactory<Program> factory, Guid pagamentoId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        return await dbContext.Pagamentos.FirstOrDefaultAsync(p => p.Id == pagamentoId);
    }

    private static HttpRequestMessage MontarRequestDaAssinatura(string dataId, string payloadJson, string? xSignature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/webhooks/mercado-pago?type=payment&data.id={dataId}")
        {
            Content = new StringContent(payloadJson, Encoding.UTF8, "application/json"),
        };
        if (xSignature is not null)
        {
            request.Headers.Add("x-signature", xSignature);
        }

        request.Headers.Add("x-request-id", RequestId);
        return request;
    }

    [Fact]
    public async Task Post_Webhook_ComAssinaturaValidaEApproved_Devolve200EConfirmaPagamento()
    {
        var (client, gerador) = CriarClienteEGerador();
        var pagamentoId = await CriarPagamentoPendenteAsync(_factory);
        var dataId = "897896789";
        var payloadJson = MontarPayloadJson(dataId);
        gerador.PagamentoMercadoPago = new PagamentoMercadoPagoDto
        {
            Id = dataId,
            Status = "approved",
            ExternalReference = pagamentoId.ToString(),
        };

        using var conteudo = new StringContent(payloadJson, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, $"/webhooks/mercado-pago?type=payment&data.id={dataId}")
        {
            Content = conteudo,
        };
        request.Headers.Add("x-signature", MontarXSignature(payloadJson, RequestId));
        request.Headers.Add("x-request-id", RequestId);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagamento = await BuscarPagamentoAsync(_factory, pagamentoId);
        pagamento!.Status.Should().Be(StatusPagamento.Confirmado);
        pagamento.EventoId.Should().Be(dataId);
    }

    [Fact]
    public async Task Post_Webhook_MesmoEventoDuasVezes_SegundaVezNaoReprocessaNemLogaDeNovo()
    {
        // Reentrega do MESMO data.id: o guard de EventoId (comparado ANTES de
        // aplicar transição) faz ProcessarEventoAsync retornar Ignorado, que o
        // controller não loga — o estado do Pagamento (EventoId inalterado)
        // prova que não houve reprocessamento, logo o log WebhookPagamentoRecebido
        // só saiu na primeira vez (task.md#inconsistências-encontradas, item 4).
        var (client, gerador) = CriarClienteEGerador();
        var pagamentoId = await CriarPagamentoPendenteAsync(_factory);
        var dataId = "777888999";
        var payloadJson = MontarPayloadJson(dataId);
        gerador.PagamentoMercadoPago = new PagamentoMercadoPagoDto
        {
            Id = dataId,
            Status = "approved",
            ExternalReference = pagamentoId.ToString(),
        };
        var xSignature = MontarXSignature(payloadJson, RequestId);

        using (var primeira = MontarRequestDaAssinatura(dataId, payloadJson, xSignature))
        {
            var resposta = await client.SendAsync(primeira);
            resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using (var segunda = MontarRequestDaAssinatura(dataId, payloadJson, xSignature))
        {
            var resposta = await client.SendAsync(segunda);
            resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var pagamento = await BuscarPagamentoAsync(_factory, pagamentoId);
        pagamento!.Status.Should().Be(StatusPagamento.Confirmado);
        pagamento.EventoId.Should().Be(dataId);
    }

    [Fact]
    public async Task Post_Webhook_ComAssinaturaInvalida_Devolve400EPagamentoNaoMuda()
    {
        var (client, _) = CriarClienteEGerador();
        var pagamentoId = await CriarPagamentoPendenteAsync(_factory);
        var dataId = "123456789";
        var payloadJson = MontarPayloadJson(dataId);
        // Assinatura calculada com segredo errado → não confere.
        using var request = MontarRequestDaAssinatura(
            dataId, payloadJson,
            $"ts=1747353600,v1={CalcularHmacSha256Hex("segredo-errado", $"id:{dataId};request-id:{RequestId};ts:1747353600;")}");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var pagamento = await BuscarPagamentoAsync(_factory, pagamentoId);
        pagamento!.Status.Should().Be(StatusPagamento.Pendente);
        pagamento.EventoId.Should().BeNull();
    }

    [Fact]
    public async Task Post_Webhook_SemAssinatura_Devolve400EPagamentoNaoMuda()
    {
        var (client, _) = CriarClienteEGerador();
        var pagamentoId = await CriarPagamentoPendenteAsync(_factory);
        var dataId = "123456789";
        var payloadJson = MontarPayloadJson(dataId);

        // Sem header x-signature.
        using var request = MontarRequestDaAssinatura(dataId, payloadJson, xSignature: null);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var pagamento = await BuscarPagamentoAsync(_factory, pagamentoId);
        pagamento!.Status.Should().Be(StatusPagamento.Pendente);
    }

    [Fact]
    public async Task Post_Webhook_FalhaDeRedeNoGetPayments_Devolve500EPagamentoNaoMuda()
    {
        // Exceção de rede/timeout do GET v1/payments (não resposta não-2xx,
        // que já vira null dentro do próprio gerador) — dev-review do PR
        // #209: o controller precisa tratar essa exceção e não propagar sem
        // logar (implementation.md, edge point 4).
        var (client, gerador) = CriarClienteEGerador();
        var pagamentoId = await CriarPagamentoPendenteAsync(_factory);
        var dataId = "555444333";
        var payloadJson = MontarPayloadJson(dataId);
        gerador.ExcecaoAoObterPagamento = new HttpRequestException("Falha simulada de rede.");
        using var request = MontarRequestDaAssinatura(dataId, payloadJson, MontarXSignature(payloadJson, RequestId));

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var pagamento = await BuscarPagamentoAsync(_factory, pagamentoId);
        pagamento!.Status.Should().Be(StatusPagamento.Pendente);
        pagamento.EventoId.Should().BeNull();
    }

    [Fact]
    public async Task Post_Webhook_ComDataIdAusenteNaQuery_Devolve400()
    {
        var client = _factory.CreateClient();

        // Payload com data.id no corpo, mas sem data.id na query — o controller
        // usa a query como fonte da verdade e rejeita com 400 (implementation.md,
        // edge point 2).
        var request = new HttpRequestMessage(
            HttpMethod.Post, "/webhooks/mercado-pago?type=payment")
        {
            Content = new StringContent(MontarPayloadJson("123456789"), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("x-signature", "ts=1747353600,v1=algumhash");
        request.Headers.Add("x-request-id", RequestId);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
