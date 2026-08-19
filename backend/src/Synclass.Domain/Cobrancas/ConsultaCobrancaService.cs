using Synclass.Domain.Alocacoes;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Calcula o valor devido por vínculo (<see cref="Matricula"/>) num
/// <see cref="PeriodoConsulta"/> (issue #12). Não é nomeado nem estruturado
/// em torno do Professor de propósito: a issue #13 adiciona
/// <see cref="ConsultarPorAlunoAsync"/> reaproveitando os métodos privados
/// desta classe, só trocando a lista de <see cref="Matricula"/> e a
/// resolução de nome (ver implementation.md#reaproveitamento-pela-issue-13
/// da #12 e implementation.md da #13).
/// </summary>
public sealed class ConsultaCobrancaService
{
    private readonly IMatriculaRepository _matriculas;
    private readonly IRegraDeCobrancaRepository _regras;
    private readonly IAlocacaoHorarioRepository _alocacoes;
    private readonly IHorarioRepository _horarios;
    private readonly IUsuarioRepository _usuarios;

    public ConsultaCobrancaService(
        IMatriculaRepository matriculas,
        IRegraDeCobrancaRepository regras,
        IAlocacaoHorarioRepository alocacoes,
        IHorarioRepository horarios,
        IUsuarioRepository usuarios)
    {
        _matriculas = matriculas;
        _regras = regras;
        _alocacoes = alocacoes;
        _horarios = horarios;
        _usuarios = usuarios;
    }

    public async Task<IReadOnlyCollection<ValorDevidoPorMatricula>> ConsultarPorProfessorAsync(
        Guid professorId, PeriodoConsulta periodo, CancellationToken cancellationToken)
    {
        var matriculas = await _matriculas.ListarPorProfessorAsync(professorId, cancellationToken);
        return await CalcularParaMatriculasAsync(matriculas, ResolverNomeAlunoAsync, periodo, cancellationToken);
    }

    /// <summary>
    /// Visão do Aluno (issue #13): uma entrada por Professor vinculado,
    /// cada uma calculada pela regra do próprio vínculo — nunca somada num
    /// total único (RN da #13). <see cref="IMatriculaRepository.ListarPorAlunoAsync"/>
    /// só devolve matrícula plena, então um Aluno provisório sem vínculo
    /// pleno cai naturalmente na lista vazia.
    /// </summary>
    public async Task<IReadOnlyCollection<ValorDevidoPorMatricula>> ConsultarPorAlunoAsync(
        Guid alunoUsuarioId, PeriodoConsulta periodo, CancellationToken cancellationToken)
    {
        var matriculas = await _matriculas.ListarPorAlunoAsync(alunoUsuarioId, cancellationToken);
        return await CalcularParaMatriculasAsync(matriculas, ResolverNomeProfessorAsync, periodo, cancellationToken);
    }

    private async Task<IReadOnlyCollection<ValorDevidoPorMatricula>> CalcularParaMatriculasAsync(
        IReadOnlyCollection<Matricula> matriculas,
        Func<Matricula, CancellationToken, Task<string>> resolverNomeAsync,
        PeriodoConsulta periodo,
        CancellationToken cancellationToken)
    {
        var resultado = new List<ValorDevidoPorMatricula>();
        foreach (var matricula in matriculas)
        {
            resultado.Add(await CalcularParaMatriculaAsync(matricula, resolverNomeAsync, periodo, cancellationToken));
        }

        return resultado;
    }

    /// <summary>
    /// Vínculo sem <see cref="RegraDeCobranca"/> nunca chega a contar
    /// aulas do período — só retorna <c>SemRegraDefinida = true</c> e
    /// <c>Valor = null</c> (edge point da issue #12, nunca <c>0</c>).
    /// </summary>
    private async Task<ValorDevidoPorMatricula> CalcularParaMatriculaAsync(
        Matricula matricula,
        Func<Matricula, CancellationToken, Task<string>> resolverNomeAsync,
        PeriodoConsulta periodo,
        CancellationToken cancellationToken)
    {
        var nome = await resolverNomeAsync(matricula, cancellationToken);
        var regra = await _regras.BuscarPorMatriculaAsync(matricula.Id, cancellationToken);
        if (regra is null)
        {
            return new ValorDevidoPorMatricula(matricula.Id, matricula.AlunoUsuarioId, nome, Valor: null, SemRegraDefinida: true);
        }

        var quantidadeDeAulasNoPeriodo = await ContarAulasNoPeriodoAsync(matricula.Id, periodo, cancellationToken);
        var valor = regra.CalcularValorDevido(quantidadeDeAulasNoPeriodo);
        return new ValorDevidoPorMatricula(matricula.Id, matricula.AlunoUsuarioId, nome, valor, SemRegraDefinida: false);
    }

    private static Task<string> ResolverNomeAlunoAsync(Matricula matricula, CancellationToken cancellationToken)
    {
        return Task.FromResult(matricula.NomeProvisorio ?? string.Empty);
    }

    /// <summary>
    /// O Professor sempre tem identidade de <see cref="Usuario"/> completa
    /// (nunca "provisório" como o Aluno pode ser) — o nome certo vem de
    /// <see cref="Usuario.Nome"/>, não de um campo equivalente a
    /// <c>NomeProvisorio</c>. <c>Usuario</c> nulo (sem exclusão hoje no
    /// domínio) cai no mesmo "melhor esforço" de string vazia da #12.
    /// </summary>
    private async Task<string> ResolverNomeProfessorAsync(Matricula matricula, CancellationToken cancellationToken)
    {
        var professor = await _usuarios.BuscarPorIdAsync(matricula.ProfessorId, cancellationToken);
        return professor?.Nome ?? string.Empty;
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
