namespace Synclass.Domain.Horarios;

/// <summary>
/// Lançada quando o horário informado não existe ou não pertence ao
/// Professor da rota — tratado como 404 pelo controller, não 400 (não é uma
/// rejeição de negócio, por isso não herda de <see cref="HorarioRejeitadoException"/>).
/// </summary>
public sealed class HorarioNaoEncontradoException : Exception
{
    public HorarioNaoEncontradoException(Guid horarioId)
        : base($"Horário não encontrado: {horarioId}.")
    {
    }
}
