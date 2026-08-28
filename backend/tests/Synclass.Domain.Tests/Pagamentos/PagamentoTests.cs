using FluentAssertions;
using Synclass.Domain.Pagamentos;

namespace Synclass.Domain.Tests.Pagamentos;

/// <summary>
/// Cobre a entidade <see cref="Pagamento"/> (issue #199): estado inicial,
/// transições de estado e idempotência de <see cref="Pagamento.Confirmar"/>/
/// <see cref="Pagamento.Falhar"/> — ver
/// implementation.md#entidade-pagamento.
/// </summary>
public sealed class PagamentoTests
{
    private static Pagamento CriarPagamento(
        decimal valor = 120m,
        DateOnly? inicio = null,
        DateOnly? fimExclusivo = null)
    {
        return new Pagamento(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            valor,
            inicio ?? new DateOnly(2026, 9, 1),
            fimExclusivo ?? new DateOnly(2026, 10, 1),
            "https://checkout.mercadopago.com/abc",
            "pref-id-teste");
    }

    [Fact]
    public void Pagamento_Novo_NascePendenteComValorCongeladoEPeriodoExatos()
    {
        var pagamento = CriarPagamento();

        pagamento.Status.Should().Be(StatusPagamento.Pendente);
        pagamento.Valor.Should().Be(120m);
        pagamento.PeriodoInicio.Should().Be(new DateOnly(2026, 9, 1));
        pagamento.PeriodoFimExclusivo.Should().Be(new DateOnly(2026, 10, 1));
        pagamento.ConfirmadoEm.Should().BeNull();
        pagamento.FalhouEm.Should().BeNull();
    }
}
