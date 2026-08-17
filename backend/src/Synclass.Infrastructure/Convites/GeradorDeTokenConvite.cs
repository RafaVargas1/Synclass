using System.Security.Cryptography;
using Synclass.Domain.Convites;

namespace Synclass.Infrastructure.Convites;

/// <summary>
/// Gera o token do link de aceite de convite usando uma fonte de
/// aleatoriedade criptograficamente segura, com muito mais entropia que
/// <see cref="Synclass.Infrastructure.Autenticacao.GeradorDeCodigoOtp"/>: o
/// token vira parte de uma URL pública, não um código digitado (ver
/// docs/specs/2-convite-whatsapp/implementation.md, edge points).
/// </summary>
public sealed class GeradorDeTokenConvite : IGeradorDeTokenConvite
{
    private const int TamanhoEmBytes = 32;

    public string Gerar()
    {
        var bytesAleatorios = RandomNumberGenerator.GetBytes(TamanhoEmBytes);
        return Convert.ToBase64String(bytesAleatorios).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
