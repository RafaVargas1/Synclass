namespace Synclass.Domain.Convites;

/// <summary>
/// Lançada quando o token do convite não corresponde a nenhum convite
/// existente, ou quando o convite já foi usado (uso único — edge point dos
/// Critérios técnicos da issue #2).
/// </summary>
public sealed class ConviteInvalidoException : ConviteRejeitadoException
{
    public ConviteInvalidoException()
        : base("Convite inválido ou já utilizado.")
    {
    }
}
