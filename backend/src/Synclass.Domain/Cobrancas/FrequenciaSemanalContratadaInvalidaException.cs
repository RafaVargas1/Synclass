namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Lançada quando <see cref="RegraValorPorAula"/> é construída com uma
/// frequência semanal contratada fora do intervalo válido de 1 a 7 (issue
/// #11) — não confundir com o registro real de frequência (presença/
/// ausência, item 14), que esta regra não valida.
/// </summary>
public sealed class FrequenciaSemanalContratadaInvalidaException : Exception
{
    public FrequenciaSemanalContratadaInvalidaException(int frequenciaSemanalContratada)
        : base($"Frequência semanal contratada inválida: {frequenciaSemanalContratada}. Esperado um valor entre 1 e 7.")
    {
    }
}
