namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Lançada quando <see cref="RegraDeCobrancaService.DefinirAsync"/> recebe
/// um <c>matriculaId</c> que não corresponde a nenhuma <c>Matricula</c>
/// existente — tratado como 404 pela Api, não 400 (não é uma rejeição de
/// negócio dos dados da regra em si).
/// </summary>
public sealed class MatriculaNaoEncontradaException : Exception
{
    public MatriculaNaoEncontradaException(Guid matriculaId)
        : base($"Matrícula não encontrada: {matriculaId}.")
    {
    }
}
