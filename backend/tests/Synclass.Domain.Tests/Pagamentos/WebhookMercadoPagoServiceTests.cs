using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Synclass.Domain.Pagamentos;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Pagamentos;

/// <summary>
/// Cobre <see cref="WebhookMercadoPagoService"/> (issue #200): verificação
/// de assinatura HMAC-SHA256 do webhook do Mercado Pago e o processamento
/// dos eventos (<c>approved</c>/<c>refunded</c>/<c>rejected</c>) com
/// idempotência por <c>EventoId</c> — ver implementation.md#fluxo-completo-do-endpoint-sequência.
/// </summary>
public sealed class WebhookMercadoPagoServiceTests
{
    private const string WebhookSecret = "segredo-de-webhook-teste";
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero));

    private const string RequestId = "req-abc-123";
    private const string Timestamp = "1747353600";

    private static WebhookMercadoPagoService CriarServico(
        FakePagamentoRepository? pagamentos = null,
        string webhookSecret = WebhookSecret)
    {
        return new WebhookMercadoPagoService(
            webhookSecret,
            pagamentos ?? new FakePagamentoRepository(),
            Clock);
    }

    private static Pagamento CriarPagamento(
        Guid? id = null,
        decimal valor = 120m,
        Guid? matriculaId = null)
    {
        return new Pagamento(
            id ?? Guid.NewGuid(),
            matriculaId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            valor,
            new DateOnly(2026, 9, 1),
            new DateOnly(2026, 10, 1),
            "https://checkout.mercadopago.com/abc",
            "pref-id-teste",
            Clock);
    }

    private static PagamentoMercadoPagoDto CriarDtoMercadoPago(
        string dataId, string status, Guid pagamentoId) =>
        new()
        {
            Id = dataId,
            Status = status,
            ExternalReference = pagamentoId.ToString(),
        };

    /// <summary>
    /// Monta o header <c>x-signature</c> no formato documentado do Mercado
    /// Pago (<c>ts=&lt;timestamp&gt;,v1=&lt;hmac&gt;</c>), com o HMAC-SHA256
    /// (chave <c>MercadoPago:WebhookSecret</c>) sobre o manifest
    /// <c>id:{data.id};request-id:{xRequestId};ts:{ts};</c> — ver
    /// implementation.md#verificação-de-assinatura.
    /// </summary>
    private static string MontarXSignature(string dataId, string requestId, string timestamp)
    {
        var manifest = $"id:{dataId};request-id:{requestId};ts:{timestamp};";
        return $"ts={timestamp},v1={CalcularHmacSha256Hex(WebhookSecret, manifest)}";
    }

    private static string CalcularHmacSha256Hex(string chave, string valor)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(chave));
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(valor));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string MontarPayloadJson(string dataId) =>
        $"{{\"type\":\"payment\",\"data\":{{\"id\":\"{dataId}\"}},\"action\":\"payment.created\"}}";

    [Fact]
    public async Task VerificarAssinaturaAsync_AssinaturaValida_RetornaTrue()
    {
        var dataId = "123456789";
        var payloadJson = MontarPayloadJson(dataId);
        var xSignature = MontarXSignature(dataId, RequestId, Timestamp);

        var servico = CriarServico();

        var aceito = await servico.VerificarAssinaturaAsync(
            payloadJson, xSignature, RequestId, CancellationToken.None);

        aceito.Should().BeTrue();
    }

    [Fact]
    public async Task VerificarAssinaturaAsync_PayloadAdulterado_MudaDataId_RetornaFalse()
    {
        // Assinatura calculada para dataId original, mas o payload chega com
        // um data.id diferente (adulteração) — o manifest montado diverge, o
        // HMAC não confere.
        var dataIdAssinado = "123456789";
        var dataIdAdulterado = "987654321";
        var payloadJson = MontarPayloadJson(dataIdAdulterado);
        var xSignature = MontarXSignature(dataIdAssinado, RequestId, Timestamp);

        var servico = CriarServico();

        var aceito = await servico.VerificarAssinaturaAsync(
            payloadJson, xSignature, RequestId, CancellationToken.None);

        aceito.Should().BeFalse();
    }

    [Fact]
    public async Task VerificarAssinaturaAsync_SemXSignature_LancaAssinaturaInvalida()
    {
        var payloadJson = MontarPayloadJson("123456789");

        var servico = CriarServico();

        var acao = async () => await servico.VerificarAssinaturaAsync(
            payloadJson, "", RequestId, CancellationToken.None);

        await acao.Should().ThrowAsync<AssinaturaInvalidaException>();
    }

    [Fact]
    public async Task VerificarAssinaturaAsync_XSignatureMalformada_SemV1_LancaAssinaturaInvalida()
    {
        // Header com apenas ts= (sem o par v1=) — não dá para comparar o hash.
        var dataId = "123456789";
        var payloadJson = MontarPayloadJson(dataId);
        var xSignatureMalformada = $"ts={Timestamp}";

        var servico = CriarServico();

        var acao = async () => await servico.VerificarAssinaturaAsync(
            payloadJson, xSignatureMalformada, RequestId, CancellationToken.None);

        await acao.Should().ThrowAsync<AssinaturaInvalidaException>();
    }

    [Fact]
    public async Task VerificarAssinaturaAsync_V1NaoEhHexValido_LancaAssinaturaInvalida()
    {
        // v1= presente mas com caractere fora de 0-9a-fA-F — Convert.FromHexString
        // lançaria FormatException sem esse tratamento (dev-review do PR #209).
        var dataId = "123456789";
        var payloadJson = MontarPayloadJson(dataId);
        var xSignatureComHexInvalido = $"ts={Timestamp},v1=zz";

        var servico = CriarServico();

        var acao = async () => await servico.VerificarAssinaturaAsync(
            payloadJson, xSignatureComHexInvalido, RequestId, CancellationToken.None);

        await acao.Should().ThrowAsync<AssinaturaInvalidaException>();
    }

    [Fact]
    public async Task ProcessarEventoAsync_Approved_ConfirmaESetaEventoId()
    {
        var pagamentoId = Guid.NewGuid();
        var pagamento = CriarPagamento(id: pagamentoId);
        var repo = new FakePagamentoRepository();
        await repo.AdicionarAsync(pagamento, CancellationToken.None);
        var servico = CriarServico(repo);

        var dataId = "123456789";
        var dto = CriarDtoMercadoPago(dataId, "approved", pagamentoId);

        var resultado = await servico.ProcessarEventoAsync(dto, CancellationToken.None);

        resultado.Tipo.Should().Be(TipoProcessamentoWebhook.Confirmado);
        resultado.PagamentoId.Should().Be(pagamentoId);
        pagamento.Status.Should().Be(StatusPagamento.Confirmado);
        pagamento.EventoId.Should().Be(dataId);
        repo.Atualizado.Should().Be(1);
    }

    [Fact]
    public async Task ProcessarEventoAsync_RefundedEmConfirmado_Estorna()
    {
        var pagamentoId = Guid.NewGuid();
        var pagamento = CriarPagamento(id: pagamentoId);
        pagamento.Confirmar(Clock);
        var repo = new FakePagamentoRepository();
        await repo.AdicionarAsync(pagamento, CancellationToken.None);
        var servico = CriarServico(repo);

        var dataId = "987654321";
        var dto = CriarDtoMercadoPago(dataId, "refunded", pagamentoId);

        var resultado = await servico.ProcessarEventoAsync(dto, CancellationToken.None);

        resultado.Tipo.Should().Be(TipoProcessamentoWebhook.Estornado);
        pagamento.Status.Should().Be(StatusPagamento.Estornado);
        pagamento.EventoId.Should().Be(dataId);
        repo.Atualizado.Should().Be(1);
    }

    [Fact]
    public async Task ProcessarEventoAsync_RejectedEmNaoConfirmado_NoOpMasAtualizaEventoId()
    {
        // Pagamento Pendente recebe rejected — sem transição de estado (não
        // estava Confirmado), mas EventoId é atualizado pra reentrega desse
        // mesmo evento não reprocessar (implementation.md#fluxo, passo 6).
        var pagamentoId = Guid.NewGuid();
        var pagamento = CriarPagamento(id: pagamentoId);
        var repo = new FakePagamentoRepository();
        await repo.AdicionarAsync(pagamento, CancellationToken.None);
        var servico = CriarServico(repo);

        var dataId = "555666777";
        var dto = CriarDtoMercadoPago(dataId, "rejected", pagamentoId);

        var resultado = await servico.ProcessarEventoAsync(dto, CancellationToken.None);

        resultado.Tipo.Should().Be(TipoProcessamentoWebhook.Ignorado);
        pagamento.Status.Should().Be(StatusPagamento.Pendente);
        pagamento.EventoId.Should().Be(dataId);
        repo.Atualizado.Should().Be(1);
    }

    [Fact]
    public async Task ProcessarEventoAsync_PagamentoInexistente_NaoLancaESinalizaNaoEncontrado()
    {
        var repo = new FakePagamentoRepository();
        var servico = CriarServico(repo);

        var dataId = "123456789";
        var dto = CriarDtoMercadoPago(dataId, "approved", Guid.NewGuid());

        var resultado = await servico.ProcessarEventoAsync(dto, CancellationToken.None);

        resultado.Tipo.Should().Be(TipoProcessamentoWebhook.NaoEncontrado);
        resultado.PagamentoId.Should().BeNull();
        repo.Atualizado.Should().Be(0);
    }

    [Fact]
    public async Task ProcessarEventoAsync_MesmoEventoDuasVezes_SegundaNaoAtualizaNemMudaEventoId()
    {
        var pagamentoId = Guid.NewGuid();
        var pagamento = CriarPagamento(id: pagamentoId);
        var repo = new FakePagamentoRepository();
        await repo.AdicionarAsync(pagamento, CancellationToken.None);
        var servico = CriarServico(repo);

        var dataId = "123456789";
        var dto = CriarDtoMercadoPago(dataId, "approved", pagamentoId);

        await servico.ProcessarEventoAsync(dto, CancellationToken.None);
        var eventoIdAposPrimeira = pagamento.EventoId;
        var atualizadoAposPrimeira = repo.Atualizado;

        // Reentrega do MESMO evento — o guard de EventoId bate antes de
        // aplicar qualquer transição: nem Confirmar, nem AtualizarAsync, nem
        // novo log (ver task.md#inconsistências-encontradas, itens 2 e 4).
        var resultado = await servico.ProcessarEventoAsync(dto, CancellationToken.None);

        resultado.Tipo.Should().Be(TipoProcessamentoWebhook.Ignorado);
        pagamento.Status.Should().Be(StatusPagamento.Confirmado);
        pagamento.EventoId.Should().Be(eventoIdAposPrimeira);
        repo.Atualizado.Should().Be(atualizadoAposPrimeira);
    }

    [Fact]
    public async Task ProcessarEventoAsync_DoisEventosDiferentes_SegundoAtualizaEventoIdParaUltimo()
    {
        var pagamentoId = Guid.NewGuid();
        var pagamento = CriarPagamento(id: pagamentoId);
        var repo = new FakePagamentoRepository();
        await repo.AdicionarAsync(pagamento, CancellationToken.None);
        var servico = CriarServico(repo);

        var dataIdApproved = "123456789";
        var dtoApproved = CriarDtoMercadoPago(dataIdApproved, "approved", pagamentoId);
        var dataIdRefunded = "987654321";

        await servico.ProcessarEventoAsync(dtoApproved, CancellationToken.None);
        var dtoRefunded = CriarDtoMercadoPago(dataIdRefunded, "refunded", pagamentoId);
        var resultado = await servico.ProcessarEventoAsync(dtoRefunded, CancellationToken.None);

        // EventoId reflete o ÚLTIMO evento processado, nunca trava no primeiro
        // (implementation.md#entidade-pagamento — o guard ??= quebraria isso).
        resultado.Tipo.Should().Be(TipoProcessamentoWebhook.Estornado);
        pagamento.Status.Should().Be(StatusPagamento.Estornado);
        pagamento.EventoId.Should().Be(dataIdRefunded);
        repo.Atualizado.Should().Be(2);
    }
}
