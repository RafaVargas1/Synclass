namespace Synclass.Domain.Alocacoes;

/// <summary>
/// Lançada quando o Professor tenta atribuir um Aluno a um horário com
/// <c>TipoMarcacao.Livre</c> — nesse tipo o horário é marcado livremente
/// pelo Aluno, o Professor nunca atribui (AC2 da issue #8). Decisão passou a
/// ser por horário, não mais por Professor (issue #74).
/// </summary>
public sealed class ModeloNaoPermiteAlocacaoException : AlocacaoRejeitadaException
{
    public ModeloNaoPermiteAlocacaoException(Guid horarioId)
        : base($"O horário {horarioId} não permite atribuição fixa de Alunos.")
    {
    }
}
