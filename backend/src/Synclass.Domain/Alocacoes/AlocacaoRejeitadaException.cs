namespace Synclass.Domain.Alocacoes;

/// <summary>
/// Base para as rejeições de negócio do fluxo de alocação (mesmo papel de
/// <c>HorarioRejeitadoException</c>/<c>MatriculaRejeitadaException</c>) —
/// permite ao controller capturar um único tipo para as respostas 400.
/// </summary>
public abstract class AlocacaoRejeitadaException : Exception
{
    protected AlocacaoRejeitadaException(string message)
        : base(message)
    {
    }
}
