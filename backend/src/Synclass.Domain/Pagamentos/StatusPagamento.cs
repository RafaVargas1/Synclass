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

    /// <summary>
    /// Pagamento que foi <c>Confirmado</c> e depois estornado/reembolsado pelo
    /// Mercado Pago (issue #200, evento <c>refunded</c>/<c>rejected</c>
    /// depois de <c>approved</c>). Nenhuma query muda por causa deste estado:
    /// a busca de pendente (só <c>Pendente</c>) e o desconto do valor devido
    /// (<c>ValorDevidoService.DescontarPagamentosConfirmadosAsync</c>, só
    /// <c>Confirmado</c>) continuam como estão — assim que o status sai de
    /// <c>Confirmado</c>, o valor devido volta a aparecer sozinho (ver
    /// implementation.md#entidade-pagamento).
    /// </summary>
    Estornado,
}
