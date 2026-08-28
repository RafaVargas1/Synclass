using Synclass.Domain.Autenticacao;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Implementação de sucesso de <see cref="IWhatsAppHttpClient"/> para os
/// testes de unidade de <c>WhatsAppNotificador</c> (issue #193): registra o
/// destino e a mensagem enviados e conclui sem erro — sem rede real.
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
/// Implementação de falha de <see cref="IWhatsAppHttpClient"/> para os
/// testes de unidade de <c>WhatsAppNotificador</c> (issue #193): lança
/// <see cref="OtpEnvioException"/> com motivo amigável, simulando o timeout
/// ou a rejeição do provedor.
/// </summary>
public sealed class FakeWhatsAppHttpClientFalha : IWhatsAppHttpClient
{
    public OtpEnvioException Excecao { get; } = new("Não foi possível enviar o código. Tente novamente em instantes.");

    public Task EnviarMensagemAsync(string numeroE164, string mensagem, CancellationToken cancellationToken)
    {
        throw Excecao;
    }
}
