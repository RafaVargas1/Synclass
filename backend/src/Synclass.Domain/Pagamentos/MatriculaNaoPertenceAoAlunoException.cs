namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Lançada quando <see cref="PagamentoService.IniciarAsync"/> recebe um
/// <c>matriculaId</c> de uma Matrícula que não pertence ao Aluno autenticado
/// (issue #199) — tratado como 404 pelo controller, mesma decisão de
/// <see cref="Alocacoes.AlunoNaoVinculadoAoProfessorException"/>: não
/// distingue "não é sua" de "não existe" pra não vazar existência do
/// recurso (ver implementation.md#contrato-de-api).
/// </summary>
public sealed class MatriculaNaoPertenceAoAlunoException : Exception
{
    public MatriculaNaoPertenceAoAlunoException(Guid matriculaId, Guid alunoUsuarioId)
        : base($"A Matrícula {matriculaId} não pertence ao Aluno {alunoUsuarioId}.")
    {
    }
}
