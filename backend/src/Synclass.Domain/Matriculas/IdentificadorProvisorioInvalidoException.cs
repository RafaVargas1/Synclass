namespace Synclass.Domain.Matriculas;

/// <summary>
/// Lançada quando o identificador provisório é vazio, só espaços, ou excede
/// o tamanho máximo aceito (<see cref="IdentificadorProvisorio.TamanhoMaximo"/>).
/// </summary>
public sealed class IdentificadorProvisorioInvalidoException : MatriculaRejeitadaException
{
    public IdentificadorProvisorioInvalidoException(string identificador, string motivo)
        : base($"Identificador inválido: \"{identificador}\". {motivo}")
    {
    }
}
