using Synclass.Domain.Alocacoes;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;

namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Calcula o valor devido por vínculo (<see cref="Matricula"/>) num
/// <see cref="PeriodoConsulta"/> (issue #12). Não é nomeado nem estruturado
/// em torno do Professor de propósito: a issue #13 adiciona
/// <c>ConsultarPorAlunoAsync</c> reaproveitando os métodos privados desta
/// classe, só trocando a lista de <see cref="Matricula"/> que alimenta o
/// cálculo (ver implementation.md#reaproveitamento-pela-issue-13).
/// </summary>
public sealed class ConsultaCobrancaService
{
    private readonly IMatriculaRepository _matriculas;
    private readonly IRegraDeCobrancaRepository _regras;
    private readonly IAlocacaoHorarioRepository _alocacoes;
    private readonly IHorarioRepository _horarios;

    public ConsultaCobrancaService(
        IMatriculaRepository matriculas,
        IRegraDeCobrancaRepository regras,
        IAlocacaoHorarioRepository alocacoes,
        IHorarioRepository horarios)
    {
        _matriculas = matriculas;
        _regras = regras;
        _alocacoes = alocacoes;
        _horarios = horarios;
    }

    public async Task<IReadOnlyCollection<ValorDevidoPorMatricula>> ConsultarPorProfessorAsync(
        Guid professorId, PeriodoConsulta periodo, CancellationToken cancellationToken)
    {
        var matriculas = await _matriculas.ListarPorProfessorAsync(professorId, cancellationToken);
        return await CalcularParaMatriculasAsync(matriculas, periodo, cancellationToken);
    }

    private async Task<IReadOnlyCollection<ValorDevidoPorMatricula>> CalcularParaMatriculasAsync(
        IReadOnlyCollection<Matricula> matriculas, PeriodoConsulta periodo, CancellationToken cancellationToken)
    {
        var resultado = new List<ValorDevidoPorMatricula>();
        foreach (var matricula in matriculas)
        {
            resultado.Add(await CalcularParaMatriculaAsync(matricula, periodo, cancellationToken));
        }

        return resultado;
    }

    /// <summary>
    /// Vínculo sem <see cref="RegraDeCobranca"/> nunca chega a contar
    /// aulas do período — só retorna <c>SemRegraDefinida = true</c> e
    /// <c>Valor = null</c> (edge point da issue #12, nunca <c>0</c>).
    /// </summary>
    private async Task<ValorDevidoPorMatricula> CalcularParaMatriculaAsync(
        Matricula matricula, PeriodoConsulta periodo, CancellationToken cancellationToken)
    {
        var nome = matricula.NomeProvisorio ?? string.Empty;
        var regra = await _regras.BuscarPorMatriculaAsync(matricula.Id, cancellationToken);
        if (regra is null)
        {
            return new ValorDevidoPorMatricula(matricula.Id, matricula.AlunoUsuarioId, nome, Valor: null, SemRegraDefinida: true);
        }

        var quantidadeDeAulasNoPeriodo = await ContarAulasNoPeriodoAsync(matricula.Id, periodo, cancellationToken);
        var valor = regra.CalcularValorDevido(quantidadeDeAulasNoPeriodo);
        return new ValorDevidoPorMatricula(matricula.Id, matricula.AlunoUsuarioId, nome, valor, SemRegraDefinida: false);
    }

    /// <summary>
    /// Soma as ocorrências semanais de cada <see cref="Horario"/> alocado à
    /// matrícula que caem dentro do período — decisão de domínio da issue
    /// #12 (ver implementation.md), não depende de frequência real. N+1 em
    /// <see cref="IHorarioRepository.BuscarPorIdAsync"/>: aceitável na escala
    /// atual (ver implementation.md#edge-points).
    /// </summary>
    private async Task<int> ContarAulasNoPeriodoAsync(Guid matriculaId, PeriodoConsulta periodo, CancellationToken cancellationToken)
    {
        var alocacoes = await _alocacoes.ListarPorMatriculaAsync(matriculaId, cancellationToken);
        var total = 0;
        foreach (var alocacao in alocacoes)
        {
            var horario = await _horarios.BuscarPorIdAsync(alocacao.HorarioId, cancellationToken);
            if (horario is not null)
            {
                total += periodo.ContarOcorrencias(horario.DiaSemana);
            }
        }

        return total;
    }
}
