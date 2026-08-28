using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Autenticacao;

/// <summary>
/// Normaliza um telefone para o formato E.164 exigido pelo provedor de
/// WhatsApp (issue #193). A normalização de contato de
/// <see cref="Contato.Normalizar"/> (usada pelo login) devolve apenas os
/// dígitos do número BR (DDD + número, 10-11 dígitos) sem o DDI — é preciso
/// converter para E.164 (com <c>+55</c>) antes de enviar ao provedor.
/// </summary>
public static class TelefoneUtils
{
    private const string FormatoEsperado = "telefone em formato E.164 (ex: +5511987654321) ou BR com DDD (10 ou 11 dígitos)";

    public static string NormalizarParaE164(string contato)
    {
        var digitos = new string(contato.Where(char.IsDigit).ToArray());

        // Já tem DDI 55 + DDD + número (8 ou 9 dígitos) = 12 ou 13 dígitos
        if (digitos.StartsWith("55") && digitos.Length is 12 or 13)
        {
            return $"+{digitos}";
        }

        if (digitos.StartsWith('0'))
        {
            digitos = digitos[1..];
        }

        // DDD + número (8 ou 9 dígitos), sem DDI — assume Brasil
        if (digitos.Length is 10 or 11)
        {
            return $"+55{digitos}";
        }

        throw new ContatoInvalidoException(contato, FormatoEsperado);
    }
}
