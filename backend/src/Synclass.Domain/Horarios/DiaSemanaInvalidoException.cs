namespace Synclass.Domain.Horarios;

/// <summary>
/// Lançada quando o dia da semana informado está fora do intervalo válido
/// de <see cref="DiaSemana"/> (0 a 6 — ver Critérios técnicos da issue #6).
/// </summary>
public sealed class DiaSemanaInvalidoException : HorarioRejeitadoException
{
    public DiaSemanaInvalidoException(int diaSemana)
        : base($"Dia da semana inválido: {diaSemana}. Esperado um valor entre 0 e 6.")
    {
    }
}
