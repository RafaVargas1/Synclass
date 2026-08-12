using System.Security.Cryptography;
using Synclass.Domain.Autenticacao;

namespace Synclass.Infrastructure.Autenticacao;

/// <summary>
/// Gera o código OTP de 6 dígitos numéricos usando uma fonte de
/// aleatoriedade criptograficamente segura (ver Critérios técnicos da
/// issue #18).
/// </summary>
public sealed class GeradorDeCodigoOtp : IGeradorDeCodigoOtp
{
    private const int LimiteExclusivo = 1_000_000;

    public string Gerar()
    {
        var numero = RandomNumberGenerator.GetInt32(0, LimiteExclusivo);
        return numero.ToString("D6");
    }
}
