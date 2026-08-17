namespace Synclass.Domain.Alocacoes;

/// <summary>
/// Lançada quando o Professor tenta atribuir um Aluno a um horário com o
/// modelo de agendamento Vago — nesse modelo qualquer horário é marcado
/// livremente pelo Aluno, o Professor nunca atribui (AC2 da issue #8).
/// Também lançada, defensivamente, se o Professor ainda não tem
/// <c>ConfiguracaoProfessor</c> definida (não deveria acontecer, já que
/// criar um horário já exige essa configuração — ver
/// docs/specs/8-aluno-horario/implementation.md#edge-points).
/// </summary>
public sealed class ModeloNaoPermiteAlocacaoException : AlocacaoRejeitadaException
{
    public ModeloNaoPermiteAlocacaoException(Guid professorId)
        : base($"O modelo de agendamento do Professor {professorId} não permite atribuição fixa de Alunos.")
    {
    }
}
