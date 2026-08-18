namespace Synclass.Domain.Alocacoes;

/// <summary>
/// Lançada quando o Aluno tenta se marcar livremente em um horário cujo
/// modelo de agendamento não permite (Fixo, ou Híbrido com este horário já
/// atribuído fixamente pelo Professor — issue #9). Distinta de
/// <see cref="ModeloNaoPermiteAlocacaoException"/> (issue #8): as regras são
/// opostas, reaproveitar uma para o outro caso produziria mensagem de erro
/// enganosa para quem lê o log/resposta 400. Também lançada,
/// defensivamente, se o Professor ainda não tem <c>ConfiguracaoProfessor</c>
/// definida — mesma política conservadora de <c>ModeloNaoPermiteAlocacaoException</c>,
/// mas aqui tratando ausência como "não permite" (oposto do default de #8),
/// ver docs/specs/9-aluno-marca-horario-vago/implementation.md#edge-points.
/// </summary>
public sealed class ModeloNaoPermiteMarcacaoLivreException : AlocacaoRejeitadaException
{
    public ModeloNaoPermiteMarcacaoLivreException(Guid professorId)
        : base($"O modelo de agendamento do Professor {professorId} não permite marcação livre pelo Aluno.")
    {
    }
}
