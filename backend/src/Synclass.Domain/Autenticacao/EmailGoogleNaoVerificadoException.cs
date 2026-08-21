namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Lançada pelo login Google (issue #65) quando o idToken é válido
/// (assinatura/issuer ok) mas o e-mail não veio confirmado pelo Google
/// (<c>EmailVerificado = false</c>). Herda <see cref="LoginRejeitadoException"/>
/// — mesma família de <see cref="ContatoSemIdentidadePlenaException"/>/<see cref="CodigoOtpInvalidoException"/> —
/// para a Api tratar como HTTP 400 de forma uniforme. Carrega o e-mail
/// normalizado para a Api poder registrar o contato mascarado no log de
/// rejeição, mesmo padrão das demais rejeições de login.
/// </summary>
public sealed class EmailGoogleNaoVerificadoException : LoginRejeitadoException
{
    public EmailGoogleNaoVerificadoException(string emailNormalizado)
        : base("O e-mail da conta do Google não foi verificado. Use uma conta com e-mail verificado.")
    {
        EmailNormalizado = emailNormalizado;
    }

    /// <summary>
    /// E-mail normalizado do Google que falhou na verificação — gravado para
    /// correlação em log (sempre mascarado pela Api, nunca em texto claro).
    /// </summary>
    public string EmailNormalizado { get; }
}
