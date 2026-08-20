namespace Synclass.Domain.Usuarios;

/// <summary>
/// Lançada quando o nome informado é vazio, só espaços, ou excede o tamanho
/// máximo aceito (<see cref="NomeUsuario.TamanhoMaximo"/>).
/// </summary>
public sealed class NomeInvalidoException : CadastroRejeitadoException
{
    public NomeInvalidoException(string nome, string motivo)
        : base($"Nome inválido: \"{nome}\". {motivo}")
    {
    }
}
