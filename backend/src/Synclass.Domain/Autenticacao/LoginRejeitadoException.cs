namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Base para toda rejeição de login (contato sem identidade plena, ou
/// código OTP inválido/expirado/já usado). Permite à Api tratar qualquer
/// rejeição de forma uniforme (HTTP 400 + log de <c>LoginRejeitado</c>) sem
/// conhecer cada subtipo individualmente — mesmo padrão de
/// <see cref="Usuarios.CadastroRejeitadoException"/>.
/// </summary>
public abstract class LoginRejeitadoException : Exception
{
    protected LoginRejeitadoException(string message)
        : base(message)
    {
    }
}
