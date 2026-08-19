namespace Synclass.Domain.Aulas;

/// <summary>
/// Base para as rejeições de negócio do fluxo de cancelamento de aula
/// (issue #10) — mesmo papel de
/// <c>Synclass.Domain.Alocacoes.AlocacaoRejeitadaException</c>, permite ao
/// controller capturar um único tipo para as respostas 400.
/// </summary>
public abstract class AulaRejeitadaException : Exception
{
    protected AulaRejeitadaException(string message)
        : base(message)
    {
    }
}
