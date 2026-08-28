namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Fronteira do transporte HTTP do WhatsApp (issue #193): separa o envio de
/// mensagem (provedor escolhido: Twilio por padrão) do negócio de
/// notificação (<see cref="INotificador"/>). Trocar de provedor exige uma
/// nova implementação desta interface e ajuste na fábrica de
/// <c>Program.cs</c>, sem tocar no Domain.
/// </summary>
public interface IWhatsAppHttpClient
{
    Task EnviarMensagemAsync(string numeroE164, string mensagem, CancellationToken cancellationToken);
}
