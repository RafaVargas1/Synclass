namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Lançada pelo <see cref="WebhookMercadoPagoService"/> quando o header
/// <c>x-signature</c> do webhook está ausente ou malformatado (sem o par
/// <c>ts=/v1=</c> esperado) — issue #200. A assinatura HMAC é a autenticação
/// do endpoint (público por design, ver
/// implementation.md#contrato-de-api); rejeitá-la impede que um webhook
/// forjado/não assinado toque o fluxo de confirmação de pagamento. A
/// mensagem não inclui detalhes do payload nem do hash para não logar dado
/// sensível (ver docs/spec/code-style.md#mensagens-de-exceção).
/// </summary>
public sealed class AssinaturaInvalidaException : Exception
{
    public AssinaturaInvalidaException()
        : base("Assinatura de webhook inválida ou ausente (x-signature ausente ou malformatada).")
    {
    }
}
