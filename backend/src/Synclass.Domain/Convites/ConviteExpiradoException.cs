namespace Synclass.Domain.Convites;

/// <summary>
/// Lançada quando um convite é usado (ou teria seu link acessado para
/// aceite) depois de <see cref="Convite.ExpiraEm"/> (critério de aceite 3
/// da issue #2).
/// </summary>
public sealed class ConviteExpiradoException : ConviteRejeitadoException
{
    public ConviteExpiradoException()
        : base("Este convite expirou. Peça ao Professor para gerar um novo link.")
    {
    }
}
