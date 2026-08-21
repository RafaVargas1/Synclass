using System.Security.Cryptography;
using Synclass.Domain.Convites;

namespace Synclass.Infrastructure.Convites;

/// <summary>
/// Gera o código curto de 5 dígitos numéricos usando uma fonte de
/// aleatoriedade criptograficamente segura — mesma técnica de
/// <see cref="Synclass.Infrastructure.Autenticacao.GeradorDeCodigoOtp"/>
/// (ver docs/specs/62-codigo-convite-curto/implementation.md).
/// </summary>
public sealed class GeradorDeCodigoConvite : IGeradorDeCodigoConvite
{
    private const int LimiteExclusivo = 100_000;

    public string Gerar()
    {
        var numero = RandomNumberGenerator.GetInt32(0, LimiteExclusivo);
        return numero.ToString("D5");
    }
}
