namespace Synclass.Domain.Horarios;

/// <summary>
/// Lançada ao tentar remover um horário que possui Alunos alocados (em
/// qualquer modelo de agendamento — issue #8). Ver dependência documentada
/// em docs/specs/6-horarios-disponiveis/implementation.md#dependência-da-issue-8.
/// </summary>
public sealed class HorarioComAlunosAlocadosException : HorarioRejeitadoException
{
    public HorarioComAlunosAlocadosException(Guid horarioId)
        : base($"Não é possível remover o horário {horarioId}: existem Alunos alocados nele.")
    {
    }
}
