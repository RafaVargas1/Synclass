namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Lançada quando <see cref="IRegraDeCobrancaRepository.SalvarAsync"/> não
/// consegue persistir a regra por causa de uma escrita concorrente para a
/// mesma <c>MatriculaId</c> (índice único violado — duas requisições
/// quase simultâneas que ambas leram "sem regra anterior" antes de
/// qualquer uma commitar). Tratado como 409 pela Api: quem chamou deve
/// tentar de novo, não é um erro de dado inválido (400) nem de matrícula
/// inexistente (404). Achado do dev-review, rodada 2 do PR #31.
/// </summary>
public sealed class RegraDeCobrancaConflitanteException : Exception
{
    public RegraDeCobrancaConflitanteException(Guid matriculaId, Exception innerException)
        : base(
            $"Conflito ao salvar a regra de cobrança da matrícula {matriculaId}: " +
                "outra atualização concorrente já foi aplicada. Tente novamente.",
            innerException)
    {
    }
}
