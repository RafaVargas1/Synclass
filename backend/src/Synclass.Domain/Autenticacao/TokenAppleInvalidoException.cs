namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Lançada pelo login Apple (issue #212) quando o idToken não passa na
/// validação criptográfica (<see cref="IValidadorDeIdTokenApple.ValidarAsync"/>
/// retorna <c>null</c> — assinatura/issuer/audience/lifetime inválidos ou
/// e-mail ausente na claim dos logins subsequentes da Apple). Herda
/// <see cref="LoginRejeitadoException"/> — mesma família das demais
/// rejeições de login — para a Api tratar como HTTP 400 de forma uniforme,
/// mesmo padrão de <see cref="TokenGoogleInvalidoException"/>.
/// </summary>
public sealed class TokenAppleInvalidoException : LoginRejeitadoException
{
    public TokenAppleInvalidoException()
        : base("Token da Apple inválido ou expirado. Faça login novamente.")
    {
    }
}
