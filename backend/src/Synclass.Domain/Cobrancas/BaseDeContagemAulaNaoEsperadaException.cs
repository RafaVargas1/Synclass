namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Lançada quando <c>baseDeContagemAula</c> é informada para
/// <see cref="TipoRegraDeCobranca.FixoMensal"/> — essa regra não conta aula,
/// então a escolha de base não tem efeito (issue #186, ver
/// implementation.md#edge-points).
/// </summary>
public sealed class BaseDeContagemAulaNaoEsperadaException : Exception
{
    public BaseDeContagemAulaNaoEsperadaException(TipoRegraDeCobranca tipo)
        : base($"Base de contagem de aula não é esperada para o tipo {tipo}. Esperado null/omitido.")
    {
    }
}
