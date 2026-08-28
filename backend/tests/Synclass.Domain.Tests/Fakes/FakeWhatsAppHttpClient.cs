using Synclass.Domain.Autenticacao;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Substitui o transporte HTTP do WhatsApp em testes de unidade: registra o
/// que teria sido enviado e retorna sucesso, sem rede real (issue #193).
/// </summary>
public sealed class FakeWhatsAppHttpClientSucesso : IWhatsAppHttpClient
{
    public List<(string NumeroE164, string Mensagem)> MensagensEnviadas { get; } = new();

    public Task EnviarMensagemAsync(string numeroE164, string mensagem, CancellationToken cancellationToken)
    {
        MensagensEnviadas.Add((numeroE164, mensagem));
        return Task.CompletedTask;
    }
}

/// <summary>
/// Substitui o transporte HTTP do WhatsApp em testes de unidade de falha:
/// lança <see cref="OtpEnvioException"/> com a causa técnica do provedor
/// preservada (mesmo caminho que o wrapper real <c>WhatsAppHttpClient</c>
/// converte HttpRequestException/TaskCanceledException), sem rede real.
/// </summary>
public sealed class FakeWhatsAppHttpClientFalha : IWhatsAppHttpClient
{
    public Task EnviarMensagemAsync(string numeroE164, string mensagem, CancellationToken cancellationToken)
    {
        throw new OtpEnvioException(
            "Não foi possível enviar o código. Tente novamente em instantes.",
            new HttpRequestException("Falha do provedor Twilio"));
    }
}
