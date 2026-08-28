using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Synclass.Domain.Pagamentos;

namespace Synclass.Infrastructure.Checkout;

/// <summary>
/// Implementação real de <see cref="IGeradorDeCheckout"/> (issue #199): cria
/// a preferência de checkout no Mercado Pago via <see cref="HttpClient"/>
/// pure, POST /checkout/preferences. O <c>BaseAddress</c> e o header
/// <c>Authorization: Bearer</c> vêm do contêiner (Program.cs,
/// <c>AddHttpClient</c>). Sempre no marketplace mode (ADR-0005): o
/// <paramref name="collectorId"/> no payload direciona o pagamento à conta
/// do Professor que recebe, nunca à conta fixa do Synclass. Segue o padrão
/// de erro de <see cref="Synclass.Infrastructure.Http.ClienteOAuthMercadoPago"/>
/// (log estruturado + exception, sem vazar o corpo cru — o Checkout Pro é
/// redirecionamento hospedado, o Synclass nunca vê cartão). A partir de
/// #200 também expõe <see cref="ObterPagamentoAsync"/>, que busca o status
/// de um pagamento via <c>GET /v1/payments/{id}</c> no mesmo
/// <see cref="HttpClient"/>/Bearer configurado.
/// </summary>
public sealed class GeradorDeCheckoutMercadoPago : IGeradorDeCheckout
{
    private const string EndpointCheckoutPreferences = "/checkout/preferences";

    private readonly HttpClient _httpClient;
    private readonly string _urlBaseApp;
    private readonly string _apiBaseUrlPublica;
    private readonly ILogger<GeradorDeCheckoutMercadoPago> _logger;

    public GeradorDeCheckoutMercadoPago(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeradorDeCheckoutMercadoPago> logger)
    {
        _httpClient = httpClient;
        _urlBaseApp = configuration["MercadoPago:UrlBaseApp"]
            ?? throw new InvalidOperationException("Configuração ausente: MercadoPago:UrlBaseApp.");
        _apiBaseUrlPublica = configuration["MercadoPago:ApiBaseUrlPublica"]
            ?? throw new InvalidOperationException("Configuração ausente: MercadoPago:ApiBaseUrlPublica.");
        _logger = logger;
    }

    /// <summary>
    /// Cria uma preferência de checkout para o Aluno pagar o valor devido. O
    /// <paramref name="externalReference"/> é o id do
    /// <see cref="Synclass.Domain.Pagamentos.Pagamento"/> (gerado por
    /// <c>PagamentoService.IniciarAsync</c> antes da chamada) — é o que o
    /// webhook de #200 casa com o pagamento. Retorna o <c>init_point</c>
    /// (URL para onde o Aluno navega) e o <c>id</c> da preferência.
    /// </summary>
    public async Task<ResultadoCheckout> CriarPreferenciaAsync(
        Guid professorId,
        string collectorId,
        decimal valor,
        string descricao,
        string externalReference,
        CancellationToken ct)
    {
        var payload = new Dictionary<string, object>
        {
            ["items"] = new[]
            {
                new { title = descricao, quantity = 1L, unit_price = valor, currency_id = "BRL" },
            },
            ["back_urls"] = new
            {
                success = $"{_urlBaseApp}/aluno/pagamento/confirmado",
                failure = $"{_urlBaseApp}/aluno/pagamento/falhou",
                pending = $"{_urlBaseApp}/aluno/pagamento/pendente",
            },
            ["auto_return"] = "approved",
            ["external_reference"] = externalReference,
            ["notification_url"] = $"{_apiBaseUrlPublica}/webhooks/mercado-pago",
            // Sem bloco "payer": o email do Aluno não é transportado pelo
            // fluxo de IniciarAsync (ver task.md#inconsistências-encontradas,
            // item 7) e é campo opcional do Checkout Pro — o MP o coleta na
            // própria tela de pagamento.
            ["collector_id"] = collectorId,
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, EndpointCheckoutPreferences)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
            };

            using var resposta = await _httpClient.SendAsync(request, ct);
            var corpoResposta = await resposta.Content.ReadAsStringAsync(ct);

            if (!resposta.IsSuccessStatusCode)
            {
                throw new FalhaAoCriarCheckoutException(
                    $"Falha na criação da preferência de checkout no Mercado Pago. Status: {resposta.StatusCode}",
                    new HttpRequestException());
            }

            using var documento = JsonDocument.Parse(corpoResposta);
            var raiz = documento.RootElement;
            var initPoint = raiz.GetProperty("init_point").GetString();
            var id = raiz.GetProperty("id").GetString();
            if (initPoint is null || id is null)
            {
                throw new FalhaAoCriarCheckoutException(
                    "Resposta inesperada do Mercado Pago na criação de preferência: id ou init_point ausentes.",
                    new InvalidOperationException());
            }

            return new ResultadoCheckout(initPoint, id);
        }
        catch (FalhaAoCriarCheckoutException)
        {
            throw;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Cancelamento genuíno do chamador (ex: cliente HTTP desconectou) —
            // não é falha do Mercado Pago, deixa propagar como cancelamento
            // normal em vez de virar FalhaAoCriarCheckoutException.
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha de rede na criação de preferência de checkout para {ProfessorId}", professorId);
            throw new FalhaAoCriarCheckoutException("Falha na criação da preferência de checkout no Mercado Pago.", ex);
        }
        catch (OperationCanceledException ex)
        {
            // Chegou aqui só quando NÃO foi o ct do chamador que disparou —
            // é o timeout 30s do HttpClient (Program.cs), falha de rede com o
            // Mercado Pago, não cancelamento do cliente.
            _logger.LogError(ex, "Timeout na criação de preferência de checkout para {ProfessorId}", professorId);
            throw new FalhaAoCriarCheckoutException("Timeout na criação da preferência de checkout no Mercado Pago.", ex);
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            // Resposta 2xx mas corpo fora do formato esperado (JSON inválido
            // ou sem id/init_point) — contrato quebrado do Mercado Pago, não
            // deve vazar cru pro cliente.
            _logger.LogError(ex, "Resposta inesperada do Mercado Pago na criação de preferência para {ProfessorId}", professorId);
            throw new FalhaAoCriarCheckoutException("Resposta inesperada do Mercado Pago na criação da preferência de checkout.", ex);
        }
    }

    /// <summary>
    /// Busca o status de um pagamento no Mercado Pago (<c>GET
    /// /v1/payments/{paymentId}</c>) — o <c>data.id</c> do webhook de #200 —
    /// para descobrir o <c>status</c> e o <c>external_reference</c> (nosso
    /// <see cref="Pagamento.Id"/>). Reutiliza o MESMO <see cref="HttpClient"/>
    /// (BaseAddress + Bearer token) já configurado em Program.cs para o
    /// checkout — não cria cliente HTTP novo. Retorna <see langword="null"/>
    /// quando o Mercado Pago responde erro (404 pagamento inexistente, 401
    /// sem permissão, etc.) — sem pagamento utilizável, o chamador decide
    /// (ver implementation.md#fluxo-completo-do-endpoint-sequência, passo 5).
    /// Exceções de rede (timeout 30s do HttpClient) propagam para o chamador
    /// tratar como falha transitória (edge point 4).
    /// </summary>
    public async Task<PagamentoMercadoPagoDto?> ObterPagamentoAsync(string paymentId, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get, $"/v1/payments/{Uri.EscapeDataString(paymentId)}");
        using var resposta = await _httpClient.SendAsync(request, ct);
        if (!resposta.IsSuccessStatusCode)
        {
            // 404/401 etc — sem pagamento utilizável; o chamador do webhook
            // decide como tratar (não encontrado / não autorizado no
            // marketplace), ver implementation.md.
            return null;
        }

        return await DeserializarPagamentoOuNuloAsync(resposta, paymentId, ct);
    }

    /// <summary>
    /// Desserializa o corpo 2xx do <c>GET v1/payments</c> — <see langword="null"/>
    /// quando o corpo não é o JSON esperado (proxy error page, resposta
    /// truncada, bug transitório do Mercado Pago), mesmo contrato de
    /// "sem pagamento utilizável" do caminho não-2xx acima. Sem esse
    /// tratamento, <see cref="JsonSerializer.Deserialize"/> lançava
    /// <see cref="JsonException"/> sem tratamento pro chamador (dev-review
    /// do PR #209, rodada 2 — mesma classe de bug do commit e903ffc, mas
    /// pra corpo malformado em vez de exceção de rede).
    /// </summary>
    private async Task<PagamentoMercadoPagoDto?> DeserializarPagamentoOuNuloAsync(
        HttpResponseMessage resposta, string paymentId, CancellationToken ct)
    {
        var corpo = await resposta.Content.ReadAsStringAsync(ct);
        try
        {
            return JsonSerializer.Deserialize<PagamentoMercadoPagoDto>(corpo);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Corpo inesperado do GET v1/payments no Mercado Pago para {PaymentId}", paymentId);
            return null;
        }
    }
}
