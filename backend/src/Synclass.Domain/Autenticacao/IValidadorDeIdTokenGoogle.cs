namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Informações extraídas de um idToken do Google após validação
/// criptográfica (assinatura/issuer). Nomeado como record porque é um
/// resultado puro de validação, sem comportamento — mesmo padrão dos
/// records de resultado de <c>Synclass.Domain.Autenticacao</c>.
/// </summary>
public sealed record InformacoesIdTokenGoogle(string Email, bool EmailVerificado);

/// <summary>
/// Envolve a validação criptográfica de um idToken do Google
/// (Google.Apis.Auth, em Synclass.Infrastructure) atrás de uma interface
/// fina, conforme docs/spec/code-style.md#dependências — mesmo padrão de
/// <see cref="IGeradorDeTokenSessao"/> e <see cref="IClock"/>. Retorna
/// <c>null</c> quando a assinatura/issuer não valida e nunca lança para esse
/// caso (token malformado é esperado vindo de um client não confiável), para
/// o domínio decidir como tratar conforme a Regra de Negócio do card #65.
/// </summary>
public interface IValidadorDeIdTokenGoogle
{
    Task<InformacoesIdTokenGoogle?> ValidarAsync(string idToken, CancellationToken cancellationToken);
}
