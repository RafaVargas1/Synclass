using System.Text.RegularExpressions;

namespace Synclass.Domain.Usuarios;

/// <summary>
/// Tipo do contato após normalização — usado por consumidores que precisam
/// persistir o tipo separadamente da string normalizada (ex: coluna
/// <c>ContatoTipo</c> de <c>Convites</c>, issue #2), sem reimplementar a
/// distinção que <see cref="Contato.Normalizar"/> já faz internamente.
/// </summary>
public enum TipoContato
{
    Email,
    Telefone,
}

/// <summary>
/// Normaliza e valida um contato (e-mail ou telefone) antes de qualquer
/// comparação de duplicidade, conforme a Regra de Negócio da issue #1:
/// e-mail em minúsculas e sem espaços nas pontas; telefone assumindo formato
/// brasileiro (DDD + número, apenas dígitos após remover máscara). Formato
/// internacional de telefone está fora de escopo.
/// </summary>
public static class Contato
{
    /// <summary>
    /// Tamanho máximo aceito para o contato normalizado, usado também pelo
    /// mapeamento EF Core em Synclass.Infrastructure (coluna `Contato`).
    /// Limita apenas o e-mail: telefone já é limitado a 10-11 dígitos.
    /// </summary>
    public const int TamanhoMaximo = 320;

    private static readonly Regex EmailRegex = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);
    private const string FormatoEmailEsperado = "e-mail no formato nome@dominio.com";
    private const string FormatoTelefoneEsperado = "telefone BR com DDD (10 ou 11 dígitos, ex: 11987654321)";

    /// <summary>
    /// Normaliza um contato bruto (como digitado pelo usuário) para a forma
    /// canônica usada na comparação de duplicidade e persistida no banco.
    /// Ex: <c>Normalizar(" Maria@Exemplo.com ")</c> retorna
    /// <c>"maria@exemplo.com"</c>; <c>Normalizar("(11) 98765-4321")</c>
    /// retorna <c>"11987654321"</c>.
    /// </summary>
    public static string Normalizar(string contatoBruto)
    {
        if (string.IsNullOrWhiteSpace(contatoBruto))
        {
            throw new ContatoInvalidoException(contatoBruto, FormatoEmailEsperado);
        }

        var contato = contatoBruto.Trim();
        return contato.Contains('@') ? NormalizarEmail(contato) : NormalizarTelefone(contato);
    }

    /// <summary>
    /// Classifica um contato já normalizado (ver <see cref="Normalizar"/>)
    /// como e-mail ou telefone, para persistência separada (ex: coluna
    /// <c>ContatoTipo</c> de <c>Convites</c>, issue #2).
    /// </summary>
    public static TipoContato IdentificarTipo(string contatoNormalizado)
    {
        return contatoNormalizado.Contains('@') ? TipoContato.Email : TipoContato.Telefone;
    }

    private static string NormalizarEmail(string email)
    {
        var normalizado = email.ToLowerInvariant();
        if (!EmailRegex.IsMatch(normalizado))
        {
            throw new ContatoInvalidoException(email, FormatoEmailEsperado);
        }

        if (normalizado.Length > TamanhoMaximo)
        {
            throw new ContatoInvalidoException(email, $"e-mail de até {TamanhoMaximo} caracteres");
        }

        return normalizado;
    }

    private static string NormalizarTelefone(string telefone)
    {
        var apenasDigitos = new string(telefone.Where(char.IsDigit).ToArray());
        if (apenasDigitos.Length is not (10 or 11))
        {
            throw new ContatoInvalidoException(telefone, FormatoTelefoneEsperado);
        }

        return apenasDigitos;
    }
}
