namespace Synclass.Domain.Matriculas;

/// <summary>
/// Lançada quando o índice único (ProfessorId, IdentificadorProvisorio) é
/// violado a nível de banco — duas requisições concorrentes passaram pela
/// checagem de duplicidade da aplicação antes de qualquer uma confirmar a
/// escrita. Preserva a causa raiz para diagnóstico, mesmo padrão de
/// <c>Synclass.Domain.Usuarios.CadastroConcorrenteException</c>.
/// </summary>
public sealed class MatriculaConcorrenteException : MatriculaRejeitadaException
{
    public MatriculaConcorrenteException(Exception causaRaiz)
        : base("Conflito ao salvar a matrícula. Tente novamente.", causaRaiz)
    {
    }
}
