namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Lançada quando o código atingiu <see cref="CodigoOtp.MaxTentativasFalhas"/>
/// tentativas de confirmação incorretas (ver dev-review do PR #25, issue
/// #18): limita brute-force do OTP de 6 dígitos, mesmo que o código
/// informado na tentativa atual esteja correto.
/// </summary>
public sealed class CodigoOtpBloqueadoException : LoginRejeitadoException
{
    public CodigoOtpBloqueadoException()
        : base($"Número máximo de tentativas ({CodigoOtp.MaxTentativasFalhas}) excedido para este código. Solicite um novo código.")
    {
    }
}
