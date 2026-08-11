namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Envia o código OTP ao usuário pelo canal correspondente ao seu contato
/// (WhatsApp/SMS para telefone, e-mail para e-mail) — a integração externa
/// real fica fora do escopo da issue #18; a implementação inicial
/// (Synclass.Infrastructure) apenas loga o código em ambiente de
/// desenvolvimento, conforme autorizado explicitamente pelos Critérios
/// técnicos do card.
/// </summary>
public interface INotificador
{
    Task EnviarCodigoOtpAsync(string contatoNormalizado, string codigo, CancellationToken cancellationToken);
}
