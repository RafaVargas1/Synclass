namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Falha no envio do código OTP pelo provedor de WhatsApp (issue #193).
/// <see cref="Motivo"/> é a mensagem amigável exibida ao usuário, sem detalhe
/// técnico do provedor; <see cref="CausaOriginal"/> é a exceção interna
/// (timeout, falha de rede etc), utilizável apenas para diagnóstico e nunca
/// serializada em log ou resposta HTTP.
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
