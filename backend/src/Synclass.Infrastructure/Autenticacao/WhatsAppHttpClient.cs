using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Synclass.Domain.Autenticacao;

namespace Synclass.Infrastructure.Autenticacao;

/// <summary>
/// Wrapper do transporte HTTP do WhatsApp via Twilio Messages API (issue
/// #193, provedor Twilio por padrão — ver questão aberta do card).
/// Timeout de 3s para não prender o usuário (RN); <c>BaseAddress</c>
/// configurado na fábrica em <c>Program.cs</c>, não fixado aqui. Conversa
/// falha de transporte (<c>HttpRequestException</c>/<c>TaskCanceledException</c>)
/// em <see cref="OtpEnvioException"/> com motivo amigável.
/// </summary>
public sealed class WhatsAppHttpClient : IWhatsAppHttpClient
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3); // RN: não prender o usuário

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _numeroRemetente;

    public WhatsAppHttpClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = Timeout;
        _apiKey = config["WhatsApp:ApiKey"]!; // validado no startup (Program.cs)
        _numeroRemetente = config["WhatsApp:NumeroRemetente"]!; // validado no startup (Program.cs)
    }

    public async Task EnviarMensagemAsync(string numeroE164, string mensagem, CancellationToken cancellationToken)
    {
        // Endpoint Twilio Messages API: POST /2010-04-01/Accounts/{AccountSid}/Messages.json
        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["To"] = $"whatsapp:{numeroE164}",
            ["From"] = $"whatsapp:{_numeroRemetente}",
            ["Body"] = mensagem,
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, _httpClient.BaseAddress)
        {
            Content = content,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(_apiKey)));

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
}
