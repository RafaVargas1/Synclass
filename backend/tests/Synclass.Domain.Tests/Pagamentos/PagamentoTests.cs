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

    [Fact]
    public void Confirmar_Pendente_MarcaConfirmadoComConfirmadoEm()
    {
        var pagamento = CriarPagamento();

        pagamento.Confirmar();

        pagamento.Status.Should().Be(StatusPagamento.Confirmado);
        pagamento.ConfirmadoEm.Should().NotBeNull();
        pagamento.FalhouEm.Should().BeNull();
    }

    [Fact]
    public void Falhar_Pendente_MarcaFalhouComFalhouEm()
    {
        var pagamento = CriarPagamento();

        pagamento.Falhar();

        pagamento.Status.Should().Be(StatusPagamento.Falhou);
        pagamento.FalhouEm.Should().NotBeNull();
        pagamento.ConfirmadoEm.Should().BeNull();
    }

    [Fact]
    public void Confirmar_JaConfirmado_NoOpNaoMudaTimestamp()
    {
        var pagamento = CriarPagamento();
        pagamento.Confirmar();
        var confirmadoEmOriginal = pagamento.ConfirmadoEm;

        pagamento.Confirmar();

        pagamento.Status.Should().Be(StatusPagamento.Confirmado);
        pagamento.ConfirmadoEm.Should().Be(confirmadoEmOriginal);
    }

    [Fact]
    public void Falhar_JaConfirmado_NoOpNaoSobrescreveConfirmadoEm()
    {
        var pagamento = CriarPagamento();
        pagamento.Confirmar();

        pagamento.Falhar();

        pagamento.Status.Should().Be(StatusPagamento.Confirmado);
        pagamento.ConfirmadoEm.Should().NotBeNull();
        pagamento.FalhouEm.Should().BeNull();
    }

    [Fact]
    public void Falhar_JaFalhou_NoOpNaoMudaTimestamp()
    {
        var pagamento = CriarPagamento();
        pagamento.Falhar();
        var falhouEmOriginal = pagamento.FalhouEm;

        pagamento.Falhar();

        pagamento.Status.Should().Be(StatusPagamento.Falhou);
        pagamento.FalhouEm.Should().Be(falhouEmOriginal);
    }

    [Fact]
    public void CriarPagamento_ValorZero_RejeitaComArgumentException()
    {
        var acao = () => CriarPagamento(valor: 0m);

        acao.Should()
            .Throw<ArgumentException>()
            .WithMessage("*Valor deve ser maior que zero*");
    }
}
