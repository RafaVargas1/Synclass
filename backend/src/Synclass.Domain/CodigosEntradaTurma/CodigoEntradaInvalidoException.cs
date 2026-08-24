namespace Synclass.Domain.CodigosEntradaTurma;

/// <summary>
/// Lançada quando o código informado não corresponde a nenhum
/// <see cref="CodigoEntradaTurma"/> ativo — inexistente ou já expirado (a
/// mesma mensagem cobre os dois casos, sem vazar qual dos dois pra quem
/// tenta adivinhar códigos).
/// </summary>
public sealed class CodigoEntradaInvalidoException : Exception
{
    public CodigoEntradaInvalidoException()
        : base("Código inválido ou expirado.")
    {
    }
}
