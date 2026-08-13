namespace Synclass.Domain.Configuracoes;

/// <summary>
/// Lançada por <c>HorarioService.CadastrarAsync</c> quando o Professor ainda
/// não tem <see cref="ConfiguracaoProfessor"/> — o modelo de agendamento é
/// obrigatório antes do primeiro horário ser criado (Critérios técnicos da
/// issue #7). Não herda de <c>HorarioRejeitadoException</c> (issue #6): o
/// controller ganha um catch próprio para este caso, mantendo o guard rail
/// isolado do vocabulário de erros de <c>Horario</c> — ver
/// implementation.md#contrato-de-api.
/// </summary>
public sealed class ModeloAgendamentoNaoDefinidoException : Exception
{
    public Guid ProfessorId { get; }

    public ModeloAgendamentoNaoDefinidoException(Guid professorId)
        : base($"Professor {professorId} ainda não definiu um modelo de agendamento.")
    {
        ProfessorId = professorId;
    }
}
