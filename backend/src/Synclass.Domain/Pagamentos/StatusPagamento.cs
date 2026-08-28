namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Ciclo de vida de um <see cref="Pagamento"/> (issue #199). Persistido como
/// string via <c>ToString()</c>, nunca como inteiro — o nome é mais estável
/// que a posição ordinal diante de novos estados (ver
/// implementation.md#entidade-pagamento).
/// </summary>
public enum StatusPagamento
{
    /// <summary>
    /// Checkout criado, aguardando o webhook confirmar/falhar (issue #200).
    /// </summary>
    Pendente,

    /// <summary>
    /// Pagamento confirmado pelo Mercado Pago — desconta do valor devido.
    /// </summary>
    Confirmado,

    /// <summary>
    /// Pagamento falhou/cancelado — o Aluno pode tentar de novo.
    /// </summary>
    Falhou,
}
