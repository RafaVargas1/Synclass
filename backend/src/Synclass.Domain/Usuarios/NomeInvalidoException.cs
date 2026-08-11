namespace Synclass.Domain.Usuarios;

/// <summary>
/// Lançada quando o nome informado é vazio ou contém apenas espaços.
/// </summary>
public sealed class NomeInvalidoException : CadastroProfessorRejeitadoException
{
    public NomeInvalidoException(string nome)
        : base($"Nome inválido: \"{nome}\". Esperado um nome não vazio.")
    {
    }
}
