namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Lançada quando <c>frequenciaSemanalContratada</c> é informada para um
/// <see cref="TipoRegraDeCobranca"/> diferente de <c>ValorPorAula</c> — só
/// essa regra usa o parâmetro (ver implementation.md#edge-points).
/// </summary>
public sealed class FrequenciaSemanalContratadaNaoEsperadaException : Exception
{
    public FrequenciaSemanalContratadaNaoEsperadaException(TipoRegraDeCobranca tipo)
        : base($"Frequência semanal contratada não é esperada para o tipo {tipo}. Esperado null/omitido.")
    {
    }
}
