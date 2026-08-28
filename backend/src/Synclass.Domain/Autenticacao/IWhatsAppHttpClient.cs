namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Fronteira de transporte do canal WhatsApp (issue #193): separa a chamada
/// HTTP ao provedor (hoje Twilio) do negócio de envio de OTP em
/// <see cref="INotificador"/>. Trocar de provedor exige nova implementação
/// desta interface e ajuste da fábrica em <c>Program.cs</c>, sem tocar o
/// Domain.
/// </summary>
public interface IWhatsAppHttpClient
{
    Task EnviarMensagemAsync(string numeroE164, string mensagem, CancellationToken cancellationToken);
}
