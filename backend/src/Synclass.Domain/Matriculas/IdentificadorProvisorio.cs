namespace Synclass.Domain.Matriculas;

/// <summary>
/// Valida o identificador provisório informado no cadastro de Aluno
/// provisório (issue #3) — um código estável escolhido pelo Professor (ex:
/// número de matrícula), não um contato de autenticação. Vazio ou só espaços
/// é inválido, mesma regra de <c>NomeUsuario.Validar</c>.
/// </summary>
public static class IdentificadorProvisorio
{
    /// <summary>
    /// Tamanho máximo aceito, usado também pelo mapeamento EF Core em
    /// Synclass.Infrastructure (coluna `IdentificadorProvisorio`).
    /// </summary>
    public const int TamanhoMaximo = 60;

    public static string Validar(string identificadorBruto)
    {
        if (string.IsNullOrWhiteSpace(identificadorBruto))
        {
            throw new IdentificadorProvisorioInvalidoException(identificadorBruto, "Esperado um identificador não vazio.");
        }

        var identificador = identificadorBruto.Trim();
        if (identificador.Length > TamanhoMaximo)
        {
            throw new IdentificadorProvisorioInvalidoException(identificador, $"Esperado no máximo {TamanhoMaximo} caracteres.");
        }

        return identificador;
    }
}
