namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Falha de envio de código OTP pelo provedor de mensageria (issue #193).
/// <see cref="Motivo"/> é a mensagem amigável exibida ao usuário/API — sem
/// detalhe técnico do provedor; <see cref="CausaOriginal"/> é a exceção
/// interna nunca serializada em log.
/// </summary>
public sealed class OtpEnvioException : Exception
{
    public string Motivo { get; }
    public Exception? CausaOriginal { get; }

    public OtpEnvioException(string motivo, Exception? causaOriginal = null)
        : base(motivo)
    {
        Motivo = motivo;
        CausaOriginal = causaOriginal;
    }
}
