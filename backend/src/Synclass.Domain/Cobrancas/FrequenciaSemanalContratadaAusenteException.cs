namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Lançada quando <see cref="TipoRegraDeCobranca.ValorPorAula"/> é escolhido
/// sem informar <c>frequenciaSemanalContratada</c> — campo obrigatório só
/// para este tipo (ver implementation.md#edge-points). Distinta de
/// <see cref="FrequenciaSemanalContratadaInvalidaException"/>, que cobre o
/// valor informado mas fora do intervalo 1-7.
/// </summary>
public sealed class FrequenciaSemanalContratadaAusenteException : Exception
{
    public FrequenciaSemanalContratadaAusenteException()
        : base("Frequência semanal contratada ausente. Esperada quando o tipo da regra é ValorPorAula.")
    {
    }
}
