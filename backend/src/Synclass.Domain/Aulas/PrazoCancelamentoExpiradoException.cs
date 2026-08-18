namespace Synclass.Domain.Aulas;

/// <summary>
/// Lançada quando o Aluno tenta cancelar depois do prazo configurado pelo
/// Professor (issue #10, AC2) — a mensagem inclui até quando era possível
/// cancelar (<paramref name="limite"/>), conforme
/// docs/spec/code-style.md#estilo-de-código (mensagens de exceção incluem o
/// valor problemático e o formato esperado).
/// </summary>
public sealed class PrazoCancelamentoExpiradoException : AulaRejeitadaException
{
    public PrazoCancelamentoExpiradoException(Guid aulaId, int prazoCancelamentoMinutos, DateTimeOffset limite)
        : base(
            $"Prazo de cancelamento expirado para a aula {aulaId}: era exigida antecedência de " +
            $"{prazoCancelamentoMinutos} minutos, então só era possível cancelar até {limite:O}.")
    {
        AulaId = aulaId;
        PrazoCancelamentoMinutos = prazoCancelamentoMinutos;
        Limite = limite;
    }

    public Guid AulaId { get; }

    /// <summary>
    /// Prazo configurado (minutos) que a tentativa não respeitou — usado
    /// pelo log estruturado <c>CancelamentoRejeitadoPorPrazo</c> (Warning)
    /// em <c>AulasController</c>.
    /// </summary>
    public int PrazoCancelamentoMinutos { get; }

    /// <summary>
    /// Até quando era possível cancelar — mesmo valor exibido na mensagem,
    /// exposto para o log estruturado não precisar reparseá-la.
    /// </summary>
    public DateTimeOffset Limite { get; }
}
