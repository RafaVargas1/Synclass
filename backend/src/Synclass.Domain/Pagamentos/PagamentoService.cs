using Synclass.Domain.Cobrancas;
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

    public PagamentoService(
        IMatriculaRepository matriculas,
        ConexaoMercadoPagoService conexaoMercadoPago,
        ConsultaCobrancaService consultaCobranca,
        IPagamentoRepository pagamentos,
        IGeradorDeCheckout geradorDeCheckout)
    {
        _matriculas = matriculas;
        _conexaoMercadoPago = conexaoMercadoPago;
        _consultaCobranca = consultaCobranca;
        _pagamentos = pagamentos;
        _geradorDeCheckout = geradorDeCheckout;
    }

    /// <summary>
    /// Inicia o pagamento do valor devido da <paramref name="matriculaId"/>
    /// no período. A checagem de posse (Matrícula existe e
    /// <see cref="Matricula.AlunoUsuarioId"/> é o Aluno autenticado) acontece
    /// antes de qualquer integração externa — mesma exceção pros dois casos
    /// (matrícula inexistente e de outro Aluno), mapeada pelo controller como
    /// 404 sem vazar existência do recurso. Professor sem conta conectada
    /// (passo 4) e sem valor devido no período (passo 5) param antes de
    /// criar/reaproveitar qualquer pagamento ou gerar checkout. Um
    /// <c>Pendente</c> existente da mesma (MatriculaId, período) é
    /// reaproveitado devolvendo a URL de checkout já gravada, sem nova
    /// chamada ao Mercado Pago (passo 6). Quando não há pendente, o id do
    /// pagamento é gerado ANTES da preferência (vira o
    /// <c>external_reference</c> do payload), a preferência é criada e o
    /// <see cref="Pagamento"/> é persistido com o valor congelado da criação
    /// (passos 7-9).
    /// </summary>
    public async Task<ResultadoInicioPagamento> IniciarAsync(
        Guid matriculaId, Guid alunoUsuarioId, DateOnly inicio, DateOnly fim, CancellationToken ct)
    {
        var periodo = PeriodoConsulta.Criar(inicio, fim);

        var matricula = await _matriculas.BuscarPorIdAsync(matriculaId, ct);
        if (matricula is null)
        {
            throw new MatriculaNaoPertenceAoAlunoException(matriculaId, alunoUsuarioId);
        }

        if (matricula.AlunoUsuarioId != alunoUsuarioId)
        {
            throw new MatriculaNaoPertenceAoAlunoException(matriculaId, alunoUsuarioId);
        }

        var collectorId = await _conexaoMercadoPago.ObterCollectorIdAsync(matricula.ProfessorId, ct);
        if (collectorId is null)
        {
            throw new ProfessorSemContaConectadaException(matricula.ProfessorId);
        }

        var valoresDevidos = await _consultaCobranca.ConsultarPorAlunoAsync(alunoUsuarioId, periodo, ct);
        var valorDaMatricula = valoresDevidos.FirstOrDefault(v => v.MatriculaId == matriculaId);
        if (valorDaMatricula is null || valorDaMatricula.Valor is null or <= 0)
        {
            throw new SemValorDevidoException(matriculaId);
        }

        var pendente = await _pagamentos.BuscarPendentePorMatriculaEPeriodoAsync(
            matriculaId, periodo.Inicio, periodo.FimExclusivo, ct);
        if (pendente is not null)
        {
            return new ResultadoInicioPagamento(pendente.Id, pendente.UrlCheckout, pendente.Valor);
        }

        var pagamentoId = Guid.NewGuid();
        var resultadoCheckout = await _geradorDeCheckout.CriarPreferenciaAsync(
            matricula.ProfessorId,
            collectorId,
            valorDaMatricula.Valor.Value,
            $"Aula particular — {periodo.Inicio:yyyy-MM-dd} a {periodo.FimExclusivo:yyyy-MM-dd}",
            pagamentoId.ToString(),
            ct);

        var pagamento = new Pagamento(
            pagamentoId,
            matriculaId,
            alunoUsuarioId,
            matricula.ProfessorId,
            valorDaMatricula.Valor.Value,
            periodo.Inicio,
            periodo.FimExclusivo,
            resultadoCheckout.UrlCheckout,
            resultadoCheckout.ReferenciaExterna);
        await _pagamentos.AdicionarAsync(pagamento, ct);

        return new ResultadoInicioPagamento(pagamento.Id, pagamento.UrlCheckout, pagamento.Valor);
    }
}
