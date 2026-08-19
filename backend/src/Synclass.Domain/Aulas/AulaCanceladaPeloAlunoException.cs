namespace Synclass.Domain.Aulas;

/// <summary>
/// Lançada quando o Aluno tenta confirmar presença (issue #15) numa
/// ocorrência que ele mesmo já cancelou (issue #10) — evita o estado
/// inconsistente de aparecer como "confirmado presente" numa aula que ele
/// próprio disse que não vai. Mesma família semântica de
/// <see cref="AulaRejeitadaException"/> usada por
/// <see cref="PrazoCancelamentoExpiradoException"/>, mapeada para 400.
/// </summary>
public sealed class AulaCanceladaPeloAlunoException : AulaRejeitadaException
{
    public AulaCanceladaPeloAlunoException(Guid aulaId, Guid matriculaId)
        : base($"A matrícula {matriculaId} já cancelou a aula {aulaId} e não pode confirmar presença nela.")
    {
    }
}
