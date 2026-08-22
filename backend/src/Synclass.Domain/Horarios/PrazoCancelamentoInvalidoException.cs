namespace Synclass.Domain.Horarios;

/// <summary>
/// Lançada quando o prazo de cancelamento de um Horário recebe um valor
/// negativo (issue #187) — mesmo padrão de
/// <see cref="TipoMarcacaoInvalidoException"/>, com a mensagem incluindo o
/// valor problemático e o formato esperado
/// (docs/spec/code-style.md#estilo-de-código).
/// </summary>
public sealed class PrazoCancelamentoInvalidoException : HorarioRejeitadoException
{
    public PrazoCancelamentoInvalidoException(int prazoCancelamentoMinutos)
        : base($"Prazo de cancelamento inválido: {prazoCancelamentoMinutos}. Esperado um valor maior ou igual a 0.")
    {
    }
}
