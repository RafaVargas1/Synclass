namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Lançada pelo login Apple (issue #212) quando o idToken é válido
/// (assinatura/issuer ok) mas o e-mail não veio confirmado pela Apple
/// (<c>EmailVerificado = false</c> — o claim <c>email_verified</c> é a
/// string "true"/"false", não booleano). Herda
/// <see cref="LoginRejeitadoException"/> — mesma família de
/// <see cref="EmailGoogleNaoVerificadoException"/> — para a Api tratar como
/// HTTP 400 de forma uniforme. Carrega o e-mail normalizado para a Api
/// poder registrar o contato mascarado no log de rejeição.
/// </summary>
public sealed class EmailAppleNaoVerificadoException : LoginRejeitadoException
{
    public EmailAppleNaoVerificadoException(string emailNormalizado)
        : base("O e-mail da conta da Apple não foi verificado. Use uma conta com e-mail verificado.")
    {
        EmailNormalizado = emailNormalizado;
    }

    /// <summary>
    /// E-mail normalizado da Apple que falhou na verificação — gravado para
    /// correlação em log (sempre mascarado pela Api, nunca em texto claro).
    /// </summary>
    public string EmailNormalizado { get; }
}
