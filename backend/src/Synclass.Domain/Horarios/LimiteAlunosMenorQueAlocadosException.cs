namespace Synclass.Domain.Horarios;

/// <summary>
/// Lançada ao tentar reduzir <see cref="Horario.LimiteAlunos"/> para um
/// valor menor que a quantidade de Alunos já alocados no horário (issue #8)
/// — deixaria o horário em estado inconsistente (mais Alunos que vagas). Ver
/// dependência documentada em
/// docs/specs/17-limite-alunos-horario/implementation.md#dependência-da-issue-8.
/// </summary>
public sealed class LimiteAlunosMenorQueAlocadosException : HorarioRejeitadoException
{
    public LimiteAlunosMenorQueAlocadosException(int limiteTentado, int quantidadeAlunosAlocados)
        : base(
            $"Não é possível reduzir o limite de alunos para {limiteTentado}: " +
            $"existem {quantidadeAlunosAlocados} alunos já alocados neste horário. " +
            "Desaloque alunos antes de reduzir o limite.")
    {
    }
}
