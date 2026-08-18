namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Lançada quando o <c>Valor</c> de uma <see cref="RegraDeCobranca"/> não é
/// maior que zero — contrato documentado em
/// docs/specs/11-regra-cobranca/implementation.md#contrato-de-api
/// ("valor: number, &gt; 0"), validado só no formulário do frontend até
/// este achado (dev-review, rodada 3 do PR #31): um <c>PUT</c> direto
/// (bypassando o form) com valor negativo ou zero era aceito com 200 OK e
/// persistia uma regra de cobrança corrompida.
/// </summary>
public sealed class ValorDeRegraDeCobrancaInvalidoException : Exception
{
    public ValorDeRegraDeCobrancaInvalidoException(decimal valor)
        : base($"Valor de regra de cobrança inválido: {valor}. Informe um valor maior que zero.")
    {
    }
}
