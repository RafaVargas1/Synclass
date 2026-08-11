namespace Synclass.Domain.Horarios;

/// <summary>
/// Lançada quando a duração informada é zero, negativa, ou de alguma outra
/// forma menor que <see cref="DuracaoAula.MinimoMinutos"/>.
/// </summary>
public sealed class DuracaoInvalidaException : HorarioRejeitadoException
{
    public DuracaoInvalidaException(int duracaoMinutos)
        : base($"Duração inválida: {duracaoMinutos}. Esperado um valor de pelo menos {DuracaoAula.MinimoMinutos} minuto.")
    {
    }
}
