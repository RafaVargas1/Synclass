namespace Synclass.Domain.Matriculas;

/// <summary>
/// Lançada quando o Professor já tem um Aluno provisório com o mesmo
/// identificador (critério de aceite 2 da issue #3). A unicidade é por
/// <c>(ProfessorId, IdentificadorProvisorio)</c>, nunca global — dois
/// Professores podem reusar o mesmo número.
/// </summary>
public sealed class IdentificadorProvisorioDuplicadoException : MatriculaRejeitadaException
{
    public IdentificadorProvisorioDuplicadoException(string identificador)
        : base($"Já existe um Aluno provisório com o identificador \"{identificador}\" para este Professor.")
    {
    }
}
