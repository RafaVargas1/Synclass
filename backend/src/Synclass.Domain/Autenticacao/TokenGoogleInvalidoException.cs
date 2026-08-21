namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Lançada pelo login Google (issue #65) quando o idToken não passa na
/// validação criptográfica (<see cref="IValidadorDeIdTokenGoogle.ValidarAsync"/>
/// retorna <c>null</c>). Herda <see cref="LoginRejeitadoException"/> — mesma
/// família das demais rejeições de login — para a Api tratar como HTTP 400
/// de forma uniforme, sem vazar detalhe do SDK para o Domain.
/// </summary>
public sealed class TokenGoogleInvalidoException : LoginRejeitadoException
{
    public TokenGoogleInvalidoException()
        : base("Token do Google inválido ou expirado. Faça login novamente.")
    {
    }
}
