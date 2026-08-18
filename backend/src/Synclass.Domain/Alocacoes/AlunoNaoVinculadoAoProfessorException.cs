namespace Synclass.Domain.Alocacoes;

/// <summary>
/// Lançada quando o Aluno autenticado não tem nenhuma Matrícula (provisória
/// ou plena) vinculada ao Professor da rota (issue #23) — tratado como 404
/// pelo controller, mesma decisão de <see cref="Horarios.HorarioNaoEncontradoException"/>
/// (não é uma rejeição de negócio corrigível reenviando os mesmos dados,
/// por isso não herda de <see cref="AlocacaoRejeitadaException"/>).
/// </summary>
public sealed class AlunoNaoVinculadoAoProfessorException : Exception
{
    public AlunoNaoVinculadoAoProfessorException(Guid alunoUsuarioId, Guid professorId)
        : base($"O Aluno {alunoUsuarioId} não está vinculado ao Professor {professorId}.")
    {
    }
}
