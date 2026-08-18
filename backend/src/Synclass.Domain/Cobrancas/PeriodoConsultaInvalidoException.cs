namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Rejeita um <see cref="PeriodoConsulta"/> cujo <c>inicio</c> não é
/// estritamente anterior ao <c>fim</c> — sem isso, um período invertido ou
/// de duração zero produziria uma contagem de ocorrências sempre igual a
/// zero em silêncio, em vez de um erro explícito.
/// </summary>
public sealed class PeriodoConsultaInvalidoException : Exception
{
    public PeriodoConsultaInvalidoException(DateOnly inicio, DateOnly fim)
        : base($"Período inválido: inicio={inicio:yyyy-MM-dd}, fim={fim:yyyy-MM-dd}. Esperado inicio estritamente anterior a fim.")
    {
    }
}
