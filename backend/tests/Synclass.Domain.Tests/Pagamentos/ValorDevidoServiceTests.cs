using FluentAssertions;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Pagamentos;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Pagamentos;

/// <summary>
/// Cobre <see cref="ValorDevidoService.DescontarPagamentosConfirmadosAsync"/>
/// (issue #199): remove da lista de <see cref="ValorDevidoPorMatricula"/> as
/// matrículas que já têm um <see cref="Pagamento"/> <c>Confirmado</c> para o
/// mesmo (MatriculaId, Inicio, FimExclusivo) — leitura pura sobre
/// <see cref="ConsultaCobrancaService"/>, que não é alterado (ver
/// implementation.md#desconto-de-pagamentos-confirmados-no-get-alunos-valor-devido).
/// </summary>
public sealed class ValorDevidoServiceTests
{
    private static readonly DateOnly Inicio = new(2026, 8, 1);
    private static readonly DateOnly FimExclusivo = new(2026, 9, 1);
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero));

    private static ValorDevidoService CriarServico(FakePagamentoRepository pagamentos)
    {
        return new ValorDevidoService(pagamentos);
    }

    private static ValorDevidoPorMatricula CriarValor(Guid matriculaId, decimal valor)
    {
        return new ValorDevidoPorMatricula(matriculaId, Guid.NewGuid(), "Nome", valor, SemRegraDefinida: false);
    }

    private static Pagamento CriarConfirmado(Guid matriculaId)
    {
        var pagamento = new Pagamento(
            Guid.NewGuid(), matriculaId, Guid.NewGuid(), Guid.NewGuid(), 300m,
            Inicio, FimExclusivo, "https://checkout.mercadopago.com/x", "pref-x", Clock);
        pagamento.Confirmar(Clock);
        return pagamento;
    }

    /// <summary>
    /// Matrícula com pagamento <c>Confirmado</c> no mesmo período é removida
    /// da lista; as demais permanecem intactas.
    /// </summary>
    [Fact]
    public async Task Descontar_MatriculaComConfirmadoNoPeriodo_RemoveDaListaEMantemAsDemais()
    {
        var matriculaPaga = Guid.NewGuid();
        var matriculaNaoPaga = Guid.NewGuid();
        var valores = new List<ValorDevidoPorMatricula>
        {
            CriarValor(matriculaPaga, 300m),
            CriarValor(matriculaNaoPaga, 150m),
        };

        var pagamentos = new FakePagamentoRepository();
        await pagamentos.AdicionarAsync(CriarConfirmado(matriculaPaga), CancellationToken.None);
        var servico = CriarServico(pagamentos);

        var resultado = await servico.DescontarPagamentosConfirmadosAsync(
            valores, Guid.NewGuid(), Inicio, FimExclusivo, CancellationToken.None);

        resultado.Should().ContainSingle(v => v.MatriculaId == matriculaNaoPaga);
        resultado.Should().NotContain(v => v.MatriculaId == matriculaPaga);
    }

    /// <summary>
    /// Matrícula com pagamento <c>Confirmado</c> em OUTRO período não é
    /// atingida pelo desconto — o match é exato em Inicio/FimExclusivo.
    /// </summary>
    [Fact]
    public async Task Descontar_ConfirmadoDeOutroPeriodo_NaoRemoveDaLista()
    {
        var matriculaId = Guid.NewGuid();
        var valores = new List<ValorDevidoPorMatricula> { CriarValor(matriculaId, 300m) };

        var pagamentos = new FakePagamentoRepository();
        var pagamentoDeOutroPeriodo = new Pagamento(
            Guid.NewGuid(), matriculaId, Guid.NewGuid(), Guid.NewGuid(), 300m,
            new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1), "https://checkout.mercadopago.com/x", "pref-x", Clock);
        pagamentoDeOutroPeriodo.Confirmar(Clock);
        await pagamentos.AdicionarAsync(pagamentoDeOutroPeriodo, CancellationToken.None);
        var servico = CriarServico(pagamentos);

        var resultado = await servico.DescontarPagamentosConfirmadosAsync(
            valores, Guid.NewGuid(), Inicio, FimExclusivo, CancellationToken.None);

        resultado.Should().ContainSingle(v => v.MatriculaId == matriculaId);
    }

    /// <summary>
    /// Matrícula com pagamento <c>Pendente</c> (ainda não confirmado pelo
    /// webhook de #200) NÃO é descontada — o vale só sai do valor devido
    /// quando o status avança para <c>Confirmado</c>.
    /// </summary>
    [Fact]
    public async Task Descontar_PagamentoPendente_NaoRemoveDaLista()
    {
        var matriculaId = Guid.NewGuid();
        var valores = new List<ValorDevidoPorMatricula> { CriarValor(matriculaId, 300m) };

        var pagamentos = new FakePagamentoRepository();
        var pagamentoPendente = new Pagamento(
            Guid.NewGuid(), matriculaId, Guid.NewGuid(), Guid.NewGuid(), 300m,
            Inicio, FimExclusivo, "https://checkout.mercadopago.com/x", "pref-x", Clock);
        await pagamentos.AdicionarAsync(pagamentoPendente, CancellationToken.None);
        var servico = CriarServico(pagamentos);

        var resultado = await servico.DescontarPagamentosConfirmadosAsync(
            valores, Guid.NewGuid(), Inicio, FimExclusivo, CancellationToken.None);

        resultado.Should().ContainSingle(v => v.MatriculaId == matriculaId);
    }
}
