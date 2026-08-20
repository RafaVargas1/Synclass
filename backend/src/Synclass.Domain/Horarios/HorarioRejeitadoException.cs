namespace Synclass.Domain.Horarios;

/// <summary>
/// Base para as rejeições de negócio do fluxo de horários (mesmo padrão de
/// <c>CadastroRejeitadoException</c> em Synclass.Domain.Usuarios) —
/// permite ao controller capturar um único tipo para as respostas 400.
/// </summary>
public abstract class HorarioRejeitadoException : Exception
{
    protected HorarioRejeitadoException(string message)
        : base(message)
    {
    }
}
