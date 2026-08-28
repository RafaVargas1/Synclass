using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Synclass.Domain.Autenticacao;
using Synclass.Infrastructure.Autenticacao;

namespace Synclass.Domain.Tests.Autenticacao;

/// <summary>
/// Cobre <see cref="WhatsAppHttpClient"/> (issue #193) contra um
/// <see cref="HttpMessageHandler"/> fake — sem rede real. Criado após o
/// dev-review do PR #206 apontar que a correção da Basic Auth do Twilio
/// (<c>AccountSid:AuthToken</c>, não um único token) não tinha nenhum teste
/// verificando o cabeçalho HTTP de fato enviado.
/// </summary>
public sealed class WhatsAppHttpClientTests
{
    private const string AccountSid = "ACcontaTeste";
    private const string AuthToken = "tokenSecretoDeTeste";
    private const string NumeroRemetente = "+15005550006";

    [Fact]
    public async Task EnviarMensagemAsync_MontaBasicAuthComAccountSidEAuthTokenSeparadosPorDoisPontos()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
        var httpClient = CriarHttpClient(handler);
        var whatsAppHttpClient = new WhatsAppHttpClient(httpClient, CriarConfiguracao());

        await whatsAppHttpClient.EnviarMensagemAsync("+5511999999999", "mensagem de teste", CancellationToken.None);

        var credenciaisEsperadas = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{AccountSid}:{AuthToken}"));
        handler.RequisicaoCapturada!.Headers.Authorization!.Scheme.Should().Be("Basic");
        handler.RequisicaoCapturada.Headers.Authorization.Parameter.Should().Be(credenciaisEsperadas);
    }

    [Fact]
    public async Task EnviarMensagemAsync_FalhaDeTransporte_LancaOtpEnvioExceptionSemDetalheTecnico()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized);
        var httpClient = CriarHttpClient(handler);
        var whatsAppHttpClient = new WhatsAppHttpClient(httpClient, CriarConfiguracao());

        var acao = () => whatsAppHttpClient.EnviarMensagemAsync("+5511999999999", "mensagem de teste", CancellationToken.None);

        var excecao = await acao.Should().ThrowAsync<OtpEnvioException>();
        excecao.Which.Motivo.Should().NotContain("Twilio").And.NotContain("Unauthorized");
    }

    private static HttpClient CriarHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler) { BaseAddress = new Uri($"https://api.twilio.com/2010-04-01/Accounts/{AccountSid}/Messages.json") };
    }

    private static IConfiguration CriarConfiguracao()
    {
        return new FakeConfiguration(new Dictionary<string, string?>
        {
            ["WhatsApp:AccountSid"] = AccountSid,
            ["WhatsApp:AuthToken"] = AuthToken,
            ["WhatsApp:NumeroRemetente"] = NumeroRemetente,
        });
    }

    /// <summary>
    /// Implementação mínima de <see cref="IConfiguration"/> para teste — só o
    /// indexador (usado pelo ctor de <see cref="WhatsAppHttpClient"/>).
    /// Evita depender do pacote <c>Microsoft.Extensions.Configuration</c>
    /// (que traria <c>ConfigurationBuilder</c>), não referenciado por este
    /// projeto de teste.
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

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;

        public FakeHttpMessageHandler(HttpStatusCode statusCode)
        {
            _statusCode = statusCode;
        }

        public HttpRequestMessage? RequisicaoCapturada { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequisicaoCapturada = request;
            return Task.FromResult(new HttpResponseMessage(_statusCode));
        }
    }
}
