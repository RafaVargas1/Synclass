namespace Synclass.Domain.Horarios;

/// <summary>
/// Lançada quando o limite de Alunos informado é menor que o mínimo
/// permitido (1 — ver <see cref="LimiteAlunosHorario"/> e a Regra de Negócio
/// da issue #17).
/// </summary>
public sealed class LimiteAlunosInvalidoException : HorarioRejeitadoException
{
    public LimiteAlunosInvalidoException(int limiteAlunos)
        : base($"Limite de alunos inválido: {limiteAlunos}. Esperado um valor a partir de {LimiteAlunosHorario.MinimoAlunos}.")
    {
    }
}
