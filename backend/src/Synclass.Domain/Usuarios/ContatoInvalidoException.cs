namespace Synclass.Domain.Usuarios;

/// <summary>
/// Lançada quando o contato informado não é um e-mail nem um telefone
/// brasileiro (DDD + número) válido.
/// </summary>
public sealed class ContatoInvalidoException : CadastroRejeitadoException
{
    public ContatoInvalidoException(string contato, string formatoEsperado)
        : base($"Contato inválido: \"{contato}\". Esperado {formatoEsperado}.")
    {
    }
}
