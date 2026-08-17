namespace Synclass.Domain.Horarios;

/// <summary>
/// Valida o limite de Alunos de um <see cref="Horario"/>. A Regra de Negócio
/// da issue #17 só impõe um mínimo (1 — aula individual, e também o default
/// quando o Professor não informa nada); não há máximo imposto pelo sistema,
/// mesma decisão já tomada para <see cref="DuracaoAula"/> na issue #6.
/// </summary>
public static class LimiteAlunosHorario
{
    public const int Padrao = 1;

    public const int MinimoAlunos = 1;

    public static void Validar(int limiteAlunos)
    {
        if (limiteAlunos < MinimoAlunos)
        {
            throw new LimiteAlunosInvalidoException(limiteAlunos);
        }
    }
}
