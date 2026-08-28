namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Resultado da troca de <c>code</c> por token ou da renovação via
/// <c>refresh_token</c> na Api do Mercado Pago (issue #203) — as credenciais
/// que <see cref="ConexaoMercadoPago.RegistrarConexao"/> e <see
/// cref="ConexaoMercadoPago.AtualizarCredenciais"/> persistem.
/// </summary>
public sealed record TrocaCodePorTokenResultado(
    string AccessToken,
    string RefreshToken,
    string CollectorId,
    DateTimeOffset ExpiraEm);
