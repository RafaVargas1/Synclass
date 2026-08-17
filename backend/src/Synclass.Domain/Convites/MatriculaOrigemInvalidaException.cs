namespace Synclass.Domain.Convites;

/// <summary>
/// Lançada ao gerar um convite a partir de um <c>matriculaId</c> de origem
/// (Aluno provisório específico, item 3 do backlog) que não existe, não
/// pertence ao Professor informado, ou já foi promovida — edge point dos
/// Critérios técnicos da issue #2.
/// </summary>
public sealed class MatriculaOrigemInvalidaException : ConviteRejeitadoException
{
    public MatriculaOrigemInvalidaException(Guid matriculaId)
        : base($"Matrícula de origem inválida: {matriculaId}. Ela não existe, não pertence a este Professor, ou já foi promovida.")
    {
    }
}
