using FluentAssertions;
using Synclass.Domain.Pagamentos;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Pagamentos;

/// <summary>
/// Cobre a entidade <see cref="Pagamento"/> (issue #199): estado inicial,
/// transições de estado e idempotência de <see cref="Pagamento.Confirmar"/>/
/// <see cref="Pagamento.Falhar"/> — ver
/// implementation.md#entidade-pagamento. Desde #200, também cobre
/// <see cref="Pagamento.Estornar"/>: a transição <c>Confirmado → Estornado</c>
/// (estorno de um pagamento já confirmado pelo webhook), no-op nos demais
/// estados — a regra é "o valor volta a aparecer como devido", que só faz
/// sentido se o pagamento tinha sido de fato confirmado antes (ver
/// implementation.md#entidade-pagamento).
/// </summary>
public sealed class PagamentoTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero));

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
            "pref-id-teste",
            Clock);
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

        pagamento.Confirmar(Clock);

        pagamento.Status.Should().Be(StatusPagamento.Confirmado);
        pagamento.ConfirmadoEm.Should().NotBeNull();
        pagamento.FalhouEm.Should().BeNull();
    }

    [Fact]
    public void Falhar_Pendente_MarcaFalhouComFalhouEm()
    {
        var pagamento = CriarPagamento();

        pagamento.Falhar(Clock);

        pagamento.Status.Should().Be(StatusPagamento.Falhou);
        pagamento.FalhouEm.Should().NotBeNull();
        pagamento.ConfirmadoEm.Should().BeNull();
    }

    [Fact]
    public void Confirmar_JaConfirmado_NoOpNaoMudaTimestamp()
    {
        var pagamento = CriarPagamento();
        pagamento.Confirmar(Clock);
        var confirmadoEmOriginal = pagamento.ConfirmadoEm;

        pagamento.Confirmar(Clock);

        pagamento.Status.Should().Be(StatusPagamento.Confirmado);
        pagamento.ConfirmadoEm.Should().Be(confirmadoEmOriginal);
    }

    [Fact]
    public void Falhar_JaConfirmado_NoOpNaoSobrescreveConfirmadoEm()
    {
        var pagamento = CriarPagamento();
        pagamento.Confirmar(Clock);

        pagamento.Falhar(Clock);

        pagamento.Status.Should().Be(StatusPagamento.Confirmado);
        pagamento.ConfirmadoEm.Should().NotBeNull();
        pagamento.FalhouEm.Should().BeNull();
    }

    [Fact]
    public void Falhar_JaFalhou_NoOpNaoMudaTimestamp()
    {
        var pagamento = CriarPagamento();
        pagamento.Falhar(Clock);
        var falhouEmOriginal = pagamento.FalhouEm;

        pagamento.Falhar(Clock);

        pagamento.Status.Should().Be(StatusPagamento.Falhou);
        pagamento.FalhouEm.Should().Be(falhouEmOriginal);
    }

    [Fact]
    public void Estornar_Confirmado_ViraEstornado()
    {
        var pagamento = CriarPagamento();
        pagamento.Confirmar(Clock);

        pagamento.Estornar(Clock);

        pagamento.Status.Should().Be(StatusPagamento.Estornado);
        pagamento.ConfirmadoEm.Should().NotBeNull();
        pagamento.FalhouEm.Should().BeNull();
    }

    [Fact]
    public void Estornar_Pendente_NoOp()
    {
        var pagamento = CriarPagamento();

        pagamento.Estornar(Clock);

        pagamento.Status.Should().Be(StatusPagamento.Pendente);
        pagamento.ConfirmadoEm.Should().BeNull();
    }

    [Fact]
    public void Estornar_Falhou_NoOp()
    {
        var pagamento = CriarPagamento();
        pagamento.Falhar(Clock);

        pagamento.Estornar(Clock);

        pagamento.Status.Should().Be(StatusPagamento.Falhou);
        pagamento.FalhouEm.Should().NotBeNull();
    }

    [Fact]
    public void Estornar_JaEstornado_NoOp()
    {
        var pagamento = CriarPagamento();
        pagamento.Confirmar(Clock);
        pagamento.Estornar(Clock);

        pagamento.Estornar(Clock);

        pagamento.Status.Should().Be(StatusPagamento.Estornado);
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
