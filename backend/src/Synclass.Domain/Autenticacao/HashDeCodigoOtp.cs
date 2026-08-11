using System.Security.Cryptography;
using System.Text;

namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Calcula o hash do código OTP para persistência (ver Critérios técnicos da
/// issue #18: o código nunca é armazenado em texto puro). Sem sal — um
/// código de 6 dígitos já expira em minutos e é de uso único, diferente de
/// uma senha de longo prazo, então SHA-256 simples é suficiente para este
/// caso de uso.
/// </summary>
public static class HashDeCodigoOtp
{
    public static string Gerar(string codigo)
    {
        var bytes = Encoding.UTF8.GetBytes(codigo);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
