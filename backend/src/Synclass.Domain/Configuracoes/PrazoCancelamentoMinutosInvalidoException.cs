namespace Synclass.Domain.Configuracoes;

/// <summary>
/// Lançada quando <see cref="ConfiguracaoProfessor.PrazoCancelamentoMinutos"/>
/// recebe um valor negativo (issue #10) — mesmo padrão de
/// <see cref="ModeloAgendamentoInvalidoException"/>.
/// </summary>
public sealed class PrazoCancelamentoMinutosInvalidoException : Exception
{
    public PrazoCancelamentoMinutosInvalidoException(int prazoCancelamentoMinutos)
        : base($"Prazo de cancelamento inválido: {prazoCancelamentoMinutos}. Esperado um valor maior ou igual a 0.")
    {
    }
}
