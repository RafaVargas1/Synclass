using Synclass.Domain.Pagamentos;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Fake de <see cref="IGeradorDeCheckout"/> usado nos testes de unidade do
/// Domain (issue #199) — não faz rede, só registra as chamadas e devolve um
/// <see cref="ResultadoCheckout"/> controlável (ver
/// docs/spec/code-style.md#testes — fakes manuais nomeados, sem Moq).
/// </summary>
public sealed class FakeGeradorDeCheckout : IGeradorDeCheckout
{
    /// <summary>
    /// Número de vezes que <see cref="CriarPreferenciaAsync"/> foi chamado —
    /// usado para provar que o reaproveitamento de um <c>Pendente</c> não
    /// gera nova preferência de checkout.
    /// </summary>
    public int Chamadas { get; private set; }

    public Guid? UltimoProfessorId { get; private set; }
    public string? UltimoCollectorId { get; private set; }
    public decimal UltimoValor { get; private set; }
    public string? UltimoExternalReference { get; private set; }

    public ResultadoCheckout Resultado { get; set; } =
        new("https://checkout.mercadopago.com/pref-teste", "pref-teste");

    public Task<ResultadoCheckout> CriarPreferenciaAsync(
        Guid professorId,
        string collectorId,
        decimal valor,
        string descricao,
        string externalReference,
        CancellationToken ct)
    {
        Chamadas++;
        UltimoProfessorId = professorId;
        UltimoCollectorId = collectorId;
        UltimoValor = valor;
        UltimoExternalReference = externalReference;
        return Task.FromResult(Resultado);
    }
}
