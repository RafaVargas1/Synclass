namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Gera o código OTP de 6 dígitos numéricos enviado ao usuário. Envolve a
/// fonte de aleatoriedade real (Synclass.Infrastructure) atrás de uma
/// interface fina, conforme docs/spec/code-style.md#dependências.
/// </summary>
public interface IGeradorDeCodigoOtp
{
    string Gerar();
}
