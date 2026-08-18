namespace Synclass.Domain.Aulas;

/// <summary>
/// Lançada quando a matrícula que tenta cancelar não está alocada no
/// horário desta aula — distinta de
/// <c>Synclass.Domain.Alocacoes.MatriculaNaoVinculadaAoProfessorException</c>
/// (que valida vínculo Aluno-Professor, não Aluno-Horário) e de
/// <c>Synclass.Domain.Alocacoes.AlocacaoNaoEncontradaException</c> (mesmo
/// nome, namespace diferente: aquela é mapeada para 404 em
/// <c>AlocacoesHorarioController.Desalocar</c>; esta é mapeada para 400, ver
/// docs/specs/10-cancelamento-aula/implementation.md#contrato-de-api).
/// </summary>
public sealed class AlocacaoNaoEncontradaException : AulaRejeitadaException
{
    public AlocacaoNaoEncontradaException(Guid horarioId, Guid matriculaId)
        : base($"A matrícula {matriculaId} não está alocada no horário {horarioId}.")
    {
    }
}
