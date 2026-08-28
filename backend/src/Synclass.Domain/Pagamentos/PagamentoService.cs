using Synclass.Domain.Cobrancas;
using Synclass.Domain.Common;
using Synclass.Domain.Matriculas;

namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Inicia o pagamento do valor devido de uma Matrícula no período (issue
/// #199): checa a posse da Matrícula, resolve o collector_id do Professor,
/// recalcula o valor devido, reaproveita um <c>Pendente</c> existente ou
/// cria uma nova preferência de checkout no Mercado Pago — ver
/// implementation.md#fluxo-de-pagamentoserviceiniciarasync.
/// </summary>
public sealed class PagamentoService
{
    private readonly IMatriculaRepository _matriculas;
    private readonly ConexaoMercadoPagoService _conexaoMercadoPago;
    private readonly ConsultaCobrancaService _consultaCobranca;
    private readonly IPagamentoRepository _pagamentos;
    private readonly IGeradorDeCheckout _geradorDeCheckout;
    private readonly IClock _clock;

    public PagamentoService(
        IMatriculaRepository matriculas,
        ConexaoMercadoPagoService conexaoMercadoPago,
        ConsultaCobrancaService consultaCobranca,
        IPagamentoRepository pagamentos,
        IGeradorDeCheckout geradorDeCheckout,
        IClock clock)
    {
        _matriculas = matriculas;
        _conexaoMercadoPago = conexaoMercadoPago;
        _consultaCobranca = consultaCobranca;
        _pagamentos = pagamentos;
        _geradorDeCheckout = geradorDeCheckout;
        _clock = clock;
    }

    /// <summary>
    /// Inicia o pagamento do valor devido da <paramref name="matriculaId"/>
    /// no período — orquestra os passos de
    /// implementation.md#fluxo-de-pagamentoserviceiniciarasync delegados aos
    /// métodos privados abaixo. Um <c>Pendente</c> existente da mesma
    /// (MatriculaId, período) é reaproveitado devolvendo a URL de checkout já
    /// gravada, sem nova chamada ao Mercado Pago.
    /// </summary>
    public async Task<ResultadoInicioPagamento> IniciarAsync(
        Guid matriculaId, Guid alunoUsuarioId, DateOnly inicio, DateOnly fim, CancellationToken ct)
    {
        var periodo = PeriodoConsulta.Criar(inicio, fim);
        var matricula = await BuscarMatriculaDoAlunoAsync(matriculaId, alunoUsuarioId, ct);
        var collectorId = await ObterCollectorIdOuFalharAsync(matricula.ProfessorId, ct);
        var valor = await ObterValorDevidoOuFalharAsync(matriculaId, alunoUsuarioId, periodo, ct);

        var pendente = await _pagamentos.BuscarPendentePorMatriculaEPeriodoAsync(
            matriculaId, periodo.Inicio, periodo.FimExclusivo, ct);
        if (pendente is not null)
        {
            return ParaResultado(pendente, matricula.ProfessorId);
        }

        var pagamento = await CriarPagamentoComCheckoutAsync(
            matricula, alunoUsuarioId, periodo, valor, collectorId, ct);
        return ParaResultado(pagamento, pagamento.ProfessorId);
    }

    /// <summary>
    /// Checa a posse da Matrícula (existe e pertence ao Aluno autenticado)
    /// antes de qualquer integração externa — mesma exceção pros dois casos
    /// (matrícula inexistente e de outro Aluno), mapeada pelo controller
    /// como 404 sem vazar existência do recurso.
    /// </summary>
    private async Task<Matricula> BuscarMatriculaDoAlunoAsync(Guid matriculaId, Guid alunoUsuarioId, CancellationToken ct)
    {
        var matricula = await _matriculas.BuscarPorIdAsync(matriculaId, ct);
        if (matricula is null || matricula.AlunoUsuarioId != alunoUsuarioId)
        {
            throw new MatriculaNaoPertenceAoAlunoException(matriculaId, alunoUsuarioId);
        }

        return matricula;
    }

    private async Task<string> ObterCollectorIdOuFalharAsync(Guid professorId, CancellationToken ct)
    {
        var collectorId = await _conexaoMercadoPago.ObterCollectorIdAsync(professorId, ct);
        if (collectorId is null)
        {
            throw new ProfessorSemContaConectadaException(professorId);
        }

        return collectorId;
    }

    /// <summary>
    /// Recalcula o valor devido da Matrícula no período e rejeita tanto
    /// "nada devido" quanto "já pago": <see cref="ConsultaCobrancaService"/>
    /// não sabe de <see cref="Pagamento"/> (não é alterado por esta Task —
    /// ver implementation.md#desconto-de-pagamentos-confirmados-no-get-alunos-valor-devido),
    /// então sem esta checagem um <c>Pagamento</c> já <c>Confirmado</c> no
    /// mesmo (MatriculaId, período) não impediria uma segunda cobrança.
    /// </summary>
    private async Task<decimal> ObterValorDevidoOuFalharAsync(
        Guid matriculaId, Guid alunoUsuarioId, PeriodoConsulta periodo, CancellationToken ct)
    {
        var valoresDevidos = await _consultaCobranca.ConsultarPorAlunoAsync(alunoUsuarioId, periodo, ct);
        var valorDaMatricula = valoresDevidos.FirstOrDefault(v => v.MatriculaId == matriculaId);
        var jaConfirmado = await _pagamentos.BuscarConfirmadoPorMatriculaEPeriodoAsync(
            matriculaId, periodo.Inicio, periodo.FimExclusivo, ct);
        if (valorDaMatricula is null || valorDaMatricula.Valor is null or <= 0 || jaConfirmado is not null)
        {
            throw new SemValorDevidoException(matriculaId);
        }

        return valorDaMatricula.Valor.Value;
    }

    /// <summary>
    /// O id do pagamento é gerado ANTES da preferência (vira o
    /// <c>external_reference</c> do payload de checkout), a preferência é
    /// criada e o <see cref="Pagamento"/> é persistido com o valor congelado
    /// da criação.
    /// </summary>
    private async Task<Pagamento> CriarPagamentoComCheckoutAsync(
        Matricula matricula, Guid alunoUsuarioId, PeriodoConsulta periodo, decimal valor, string collectorId, CancellationToken ct)
    {
        var pagamentoId = Guid.NewGuid();
        var resultadoCheckout = await _geradorDeCheckout.CriarPreferenciaAsync(
            matricula.ProfessorId,
            collectorId,
            valor,
            $"Aula particular — {periodo.Inicio:yyyy-MM-dd} a {periodo.FimExclusivo:yyyy-MM-dd}",
            pagamentoId.ToString(),
            ct);

        var pagamento = new Pagamento(
            pagamentoId,
            matricula.Id,
            alunoUsuarioId,
            matricula.ProfessorId,
            valor,
            periodo.Inicio,
            periodo.FimExclusivo,
            resultadoCheckout.UrlCheckout,
            resultadoCheckout.ReferenciaExterna,
            _clock);
        await _pagamentos.AdicionarAsync(pagamento, ct);
        return pagamento;
    }

    private static ResultadoInicioPagamento ParaResultado(Pagamento pagamento, Guid professorId)
    {
        return new ResultadoInicioPagamento(
            pagamento.Id, pagamento.UrlCheckout, pagamento.Valor, professorId, pagamento.ReferenciaExterna);
    }
}
