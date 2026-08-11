namespace Synclass.Domain.Usuarios;

/// <summary>
/// Valida o nome informado no cadastro. Nome vazio ou só espaços é inválido
/// (edge point da Regra de Negócio da issue #1).
/// </summary>
public static class NomeUsuario
{
    public static string Validar(string nomeBruto)
    {
        if (string.IsNullOrWhiteSpace(nomeBruto))
        {
            throw new NomeInvalidoException(nomeBruto);
        }

        return nomeBruto.Trim();
    }
}
