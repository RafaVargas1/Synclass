namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Lançada quando o código informado está incorreto, expirado, ou já foi
/// usado — as três causas recebem a mesma mensagem porque, do ponto de
/// vista do usuário, a ação correta é sempre a mesma: solicitar um código
/// novo (ver Critérios de aceite da issue #18).
/// </summary>
public sealed class CodigoOtpInvalidoException : LoginRejeitadoException
{
    public CodigoOtpInvalidoException()
        : base("Código inválido ou expirado. Solicite um novo código.")
    {
    }
}
