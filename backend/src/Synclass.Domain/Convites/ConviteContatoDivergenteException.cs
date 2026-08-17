namespace Synclass.Domain.Convites;

/// <summary>
/// Lançada quando o contato informado no aceite não corresponde ao contato
/// para o qual o convite foi gerado (Regra de Negócio da issue #2: "o Aluno
/// completa o cadastro com o mesmo contato do convite"). Ver decisão
/// documentada em docs/specs/2-convite-whatsapp/implementation.md.
/// </summary>
public sealed class ConviteContatoDivergenteException : ConviteRejeitadoException
{
    public ConviteContatoDivergenteException()
        : base("O contato informado não corresponde a este convite. Peça um novo link ao Professor.")
    {
    }
}
