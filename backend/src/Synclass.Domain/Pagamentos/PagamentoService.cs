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
    /// no período. A checagem de posse (Matrícula existe e
    /// <see cref="Matricula.AlunoUsuarioId"/> é o Aluno autenticado) acontece
    /// antes de qualquer integração externa — mesma exceção pros dois casos
    /// (matrícula inexistente e de outro Aluno), mapeada pelo controller como
    /// 404 sem vazar existência do recurso. Professor sem conta conectada
    /// (passo 4) e sem valor devido no período (passo 5) param antes de
    /// criar/reaproveitar qualquer pagamento ou gerar checkout.
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

        // As etapas subseqüentes (reaproveitar pendente ou criar preferência
        // de checkout) serão implementadas nos itens seguintes do task.md.
        throw new NotImplementedException();
    }
}
