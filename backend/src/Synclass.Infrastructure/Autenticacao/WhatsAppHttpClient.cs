using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Synclass.Domain.Autenticacao;

namespace Synclass.Infrastructure.Autenticacao;

/// <summary>
/// Wrapper do transporte HTTP do WhatsApp via Twilio Messages API (issue
/// #193, provedor Twilio por padrão — ver questão aberta do card).
/// Timeout de 3s para não prender o usuário (RN); <c>BaseAddress</c>
/// (<c>https://api.twilio.com/2010-04-01/Accounts/{AccountSid}/Messages.json</c>)
/// montado na fábrica em <c>Program.cs</c> a partir de <c>WhatsApp:AccountSid</c>,
/// não fixado aqui. A Basic Auth do Twilio exige <c>AccountSid:AuthToken</c>
/// (dois valores separados por <c>:</c>, não um único token) — ver
/// <see cref="_accountSid"/>/<see cref="_authToken"/>. Converte falha de
/// transporte (<c>HttpRequestException</c>/<c>TaskCanceledException</c>) em
/// <see cref="OtpEnvioException"/> com motivo amigável.
/// </summary>
public sealed class WhatsAppHttpClient : IWhatsAppHttpClient
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3); // RN: não prender o usuário

    private readonly HttpClient _httpClient;
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _numeroRemetente;

    public WhatsAppHttpClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = Timeout;
        _accountSid = config["WhatsApp:AccountSid"]!; // validado no startup (Program.cs)
        _authToken = config["WhatsApp:AuthToken"]!; // validado no startup (Program.cs)
        _numeroRemetente = config["WhatsApp:NumeroRemetente"]!; // validado no startup (Program.cs)
    }

    public async Task EnviarMensagemAsync(string numeroE164, string mensagem, CancellationToken cancellationToken)
    {
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = $"whatsapp:{numeroE164}",
            ["From"] = $"whatsapp:{_numeroRemetente}",
            ["Body"] = mensagem,
        });

        using var request = MontarRequisicao(content);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            throw new OtpEnvioException("Não foi possível enviar o código. Tente novamente em instantes.", ex);
        }
    }

    private HttpRequestMessage MontarRequisicao(HttpContent content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _httpClient.BaseAddress)
        {
            Content = content,
        };
        var credenciais = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_accountSid}:{_authToken}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credenciais);
        return request;
    }
}
