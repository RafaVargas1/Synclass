namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Lançada quando <see cref="PagamentoService.IniciarAsync"/> não encontra
/// valor devido &gt; 0 pra matrícula no período (issue #199) — nada a pagar.
/// Mapeada pelo controller como 400 (<c>tipo: sem-valor-devido</c>), com a
/// mensagem pensada pro usuário (ver implementation.md#contrato-de-api).
/// </summary>
public sealed class SemValorDevidoException : Exception
{
    public SemValorDevidoException(Guid matriculaId)
        : base($"Não há valor devido para a Matrícula {matriculaId} no período selecionado.")
    {
        MatriculaId = matriculaId;
    }

    public Guid MatriculaId { get; }
}
