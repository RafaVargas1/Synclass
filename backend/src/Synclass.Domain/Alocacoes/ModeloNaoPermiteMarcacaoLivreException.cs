namespace Synclass.Domain.Alocacoes;

/// <summary>
/// Lançada quando o Aluno tenta se marcar livremente em um horário com
/// <c>TipoMarcacao.Fixo</c> (issue #9). Distinta de
/// <see cref="ModeloNaoPermiteAlocacaoException"/> (issue #8): as regras são
/// opostas, reaproveitar uma para o outro caso produziria mensagem de erro
/// enganosa para quem lê o log/resposta 400. Decisão passou a ser por
/// horário, não mais por Professor (issue #74) — Híbrido deixou de bloquear
/// este caso quando já há atribuição fixa (AC3).
/// </summary>
public sealed class ModeloNaoPermiteMarcacaoLivreException : AlocacaoRejeitadaException
{
    public ModeloNaoPermiteMarcacaoLivreException(Guid horarioId)
        : base($"O horário {horarioId} não permite marcação livre pelo Aluno.")
    {
    }
}
