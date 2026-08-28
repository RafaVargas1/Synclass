using Synclass.Domain.Cobrancas;

namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Passo de desconto de pagamentos já confirmados na leitura do valor
/// devido (issue #199): remove da lista recebida as matrículas que já têm
/// um <see cref="Pagamento"/> <c>Confirmado</c> para o mesmo
/// (MatriculaId, período). É leitura pura — chamado a partir do nível da
/// Api sobre o resultado de <see cref="ConsultaCobrancaService"/>, que não
/// é alterado (Professor usa o mesmo serviço, sem desconto nesta Task — ver
/// implementation.md#desconto-de-pagamentos-confirmados-no-get-alunos-valor-devido).
/// </summary>
public sealed class ValorDevidoService
{
    private readonly IPagamentoRepository _pagamentos;

    public ValorDevidoService(IPagamentoRepository pagamentos)
    {
        _pagamentos = pagamentos;
    }

    /// <summary>
    /// Remove de <paramref name="valores"/> as matrículas com um
    /// <see cref="Pagamento"/> <c>Confirmado</c> no período exato
    /// (<paramref name="inicio"/>, <paramref name="fimExclusivo"/>). O match
    /// é exato: um confirmado de outro período não desconta esta matrícula.
    /// <paramref name="alunoUsuarioId"/> fica aqui para escopo futuro de
    /// repositório, mas a busca atual já é por matrícula (proveniente da
    /// consulta do Aluno) — ver
    /// implementation.md#ipagamentorepository.
    /// </summary>
    public async Task<List<ValorDevidoPorMatricula>> DescontarPagamentosConfirmadosAsync(
        List<ValorDevidoPorMatricula> valores,
        Guid alunoUsuarioId,
        DateOnly inicio,
        DateOnly fimExclusivo,
        CancellationToken ct)
    {
        var resultado = new List<ValorDevidoPorMatricula>();
        foreach (var valor in valores)
        {
            var confirmado = await _pagamentos.BuscarConfirmadoPorMatriculaEPeriodoAsync(
                valor.MatriculaId, inicio, fimExclusivo, ct);
            if (confirmado is null)
            {
                resultado.Add(valor);
            }
        }

        return resultado;
    }
}
