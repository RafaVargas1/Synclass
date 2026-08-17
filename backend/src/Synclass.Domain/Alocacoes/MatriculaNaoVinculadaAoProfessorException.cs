namespace Synclass.Domain.Alocacoes;

/// <summary>
/// Lançada quando a Matrícula informada não existe ou pertence a outro
/// Professor (AC5 da issue #8) — os dois casos não são distinguidos, mesma
/// decisão de <c>HorarioNaoEncontradoException</c> para "não existe ou não
/// pertence".
/// </summary>
public sealed class MatriculaNaoVinculadaAoProfessorException : AlocacaoRejeitadaException
{
    public MatriculaNaoVinculadaAoProfessorException(Guid matriculaId, Guid professorId)
        : base($"A matrícula {matriculaId} não está vinculada ao Professor {professorId}.")
    {
    }
}
