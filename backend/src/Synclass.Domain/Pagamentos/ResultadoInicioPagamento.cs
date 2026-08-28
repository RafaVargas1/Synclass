namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Retorno de <see cref="PagamentoService.IniciarAsync"/>: o id do pagamento
/// criado ou reaproveitado, a URL do checkout pra onde o Aluno navega e o
/// valor congelado na criação.
/// </summary>
public sealed record ResultadoInicioPagamento(Guid PagamentoId, string UrlCheckout, decimal Valor);
