namespace Synclass.Domain.Usuarios;

/// <summary>
/// Valida o nome informado no cadastro. Nome vazio ou só espaços é inválido
/// (edge point da Regra de Negócio da issue #1), assim como um nome maior
/// que a coluna do banco comporta (ver <see cref="TamanhoMaximo"/>, usado
/// também pelo mapeamento EF Core em Synclass.Infrastructure).
/// </summary>
public static class NomeUsuario
{
    public const int TamanhoMaximo = 200;

    public static string Validar(string nomeBruto)
    {
        if (string.IsNullOrWhiteSpace(nomeBruto))
        {
            throw new NomeInvalidoException(nomeBruto, "Esperado um nome não vazio.");
        }

        var nome = nomeBruto.Trim();
        if (nome.Length > TamanhoMaximo)
        {
            throw new NomeInvalidoException(nome, $"Esperado no máximo {TamanhoMaximo} caracteres.");
        }

        return nome;
    }
}
