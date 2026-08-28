namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Retorno de <see cref="PagamentoService.IniciarAsync"/>: o id do pagamento
/// criado ou reaproveitado, a URL do checkout pra onde o Aluno navega e o
/// valor congelado na criação. <see cref="ProfessorId"/> e
/// <see cref="ReferenciaExterna"/> são carregados pra o controller logar o
/// evento <c>PagamentoIniciado</c> com os campos do
/// implementation.md#logs-estruturados (o Domain não injeta ILogger — ver
/// implementation.md#decisão-de-design-logging-sem-violar-camadas).
/// </summary>
public sealed record ResultadoInicioPagamento(
    Guid PagamentoId,
    string UrlCheckout,
    decimal Valor,
    Guid ProfessorId,
    string? ReferenciaExterna);
