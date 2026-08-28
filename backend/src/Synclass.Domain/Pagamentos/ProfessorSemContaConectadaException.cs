namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Lançada quando <see cref="PagamentoService.IniciarAsync"/> não consegue
/// resolver o <c>collector_id</c> do Professor (issue #199) — ele ainda não
/// conectou uma conta Mercado Pago para receber pagamentos. Mapeada pelo
/// controller como 400 (<c>tipo: professor-sem-conta-conectada</c>), com a
/// mensagem pensada pro usuário (ver implementation.md#contrato-de-api).
/// </summary>
public sealed class ProfessorSemContaConectadaException : Exception
{
    public ProfessorSemContaConectadaException(Guid professorId)
        : base($"O Professor {professorId} ainda não conectou uma conta para receber pagamentos.")
    {
        ProfessorId = professorId;
    }

    public Guid ProfessorId { get; }
}
