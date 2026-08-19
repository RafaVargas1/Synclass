using Synclass.Domain.Alocacoes;
using Synclass.Domain.Aulas;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Common;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Frequencias;

/// <summary>
/// Orquestra os 3 casos de uso do card (issues #14/#15/#16): o Professor
/// registra, em lote, a presença/ausência de cada Aluno alocado num
/// horário/data (<see cref="RegistrarAsync"/>); o Aluno confirma a própria
/// presença (<see cref="ConfirmarPresencaAsync"/>); o Aluno consulta o
/// próprio histórico (<see cref="ListarHistoricoAsync"/>). Reaproveita
/// <see cref="AulaService.ObterOuCriarAulaAsync"/> (instanciação sob
/// demanda, issue #10) e <see cref="HorarioService.BuscarDoProfessorAsync"/>
/// (checagem de posse), mesmo padrão de <see cref="AulaService.CancelarAsync"/>
/// — ver docs/specs/14-registro-frequencia/implementation.md e
/// docs/specs/15-aluno-confirma-presenca/implementation.md.
/// </summary>
public sealed class FrequenciaService
{
    private readonly IRegistroFrequenciaRepository _registros;
    private readonly IAlocacaoHorarioRepository _alocacoes;
    private readonly AulaService _aulaService;
    private readonly AlocacaoHorarioService _alocacaoHorarioService;
    private readonly ICancelamentoAulaRepository _cancelamentos;
    private readonly HorarioService _horarioService;
    private readonly IClock _clock;
    private readonly IMatriculaRepository _matriculas;
    private readonly IUsuarioRepository _usuarios;
    private readonly IHorarioRepository _horarios;
    private readonly IAulaRepository _aulas;

    public FrequenciaService(
        IRegistroFrequenciaRepository registros,
        IAlocacaoHorarioRepository alocacoes,
        AulaService aulaService,
        AlocacaoHorarioService alocacaoHorarioService,
        ICancelamentoAulaRepository cancelamentos,
        HorarioService horarioService,
        IClock clock,
        IMatriculaRepository matriculas,
        IUsuarioRepository usuarios,
        IHorarioRepository horarios,
        IAulaRepository aulas)
    {
        _registros = registros;
        _alocacoes = alocacoes;
        _aulaService = aulaService;
        _alocacaoHorarioService = alocacaoHorarioService;
        _cancelamentos = cancelamentos;
        _horarioService = horarioService;
        _clock = clock;
        _matriculas = matriculas;
        _usuarios = usuarios;
        _horarios = horarios;
        _aulas = aulas;
    }

    public async Task<IReadOnlyCollection<RegistroFrequencia>> RegistrarAsync(
        Guid professorId,
        Guid horarioId,
        DateOnly data,
        IReadOnlyDictionary<Guid, StatusFrequencia> statusPorMatricula,
        CancellationToken cancellationToken)
    {
        await _horarioService.BuscarDoProfessorAsync(professorId, horarioId, cancellationToken);
        await GarantirMatriculasAlocadasAsync(horarioId, statusPorMatricula.Keys, cancellationToken);

        var aula = await _aulaService.ObterOuCriarAulaAsync(horarioId, data, cancellationToken);

        var registros = new List<RegistroFrequencia>();
        foreach (var (matriculaId, status) in statusPorMatricula)
        {
            var registro = await RegistrarUmAsync(aula.Id, matriculaId, status, cancellationToken);
            registros.Add(registro);
        }

        await _registros.SalvarAsync(cancellationToken);
        return registros;
    }

    /// <summary>
    /// O Aluno confirma a própria presença numa aula (issue #15) — upsert
    /// idempotente na mesma linha de <see cref="RegistrarAsync"/> (AC2:
    /// confirmar de novo não duplica). Rejeita se o próprio Aluno já
    /// cancelou esta ocorrência (issue #10): confirmar presença numa aula
    /// que ele mesmo disse que não vai deixaria um estado inconsistente.
    /// </summary>
    public async Task<RegistroFrequencia> ConfirmarPresencaAsync(
        Guid professorId, Guid horarioId, DateOnly data, Guid alunoUsuarioId, CancellationToken cancellationToken)
    {
        await _horarioService.BuscarDoProfessorAsync(professorId, horarioId, cancellationToken);
        var matriculaId = await _alocacaoHorarioService.ResolverMatriculaDoAlunoAsync(
            professorId, alunoUsuarioId, cancellationToken);
        await GarantirMatriculasAlocadasAsync(horarioId, new[] { matriculaId }, cancellationToken);

        var aula = await _aulaService.ObterOuCriarAulaAsync(horarioId, data, cancellationToken);
        await GarantirNaoCanceladaPeloAlunoAsync(aula.Id, matriculaId, cancellationToken);

        var registro = await _registros.BuscarAsync(aula.Id, matriculaId, cancellationToken);
        if (registro is null)
        {
            registro = RegistroFrequencia.Criar(aula.Id, matriculaId, _clock);
            await _registros.AdicionarAsync(registro, cancellationToken);
        }

        registro.ConfirmarAluno(_clock);
        await _registros.SalvarAsync(cancellationToken);
        return registro;
    }

    /// <summary>
    /// Histórico de frequência do Aluno autenticado (issue #16), agrupado
    /// por Professor — nunca soma/mistura entre Professores (mesma RN de
    /// <see cref="Synclass.Domain.Cobrancas.ConsultaCobrancaService.ConsultarPorAlunoAsync"/>,
    /// issue #13). 100% leitura: nunca chama
    /// <see cref="AulaService.ObterOuCriarAulaAsync"/> — uma data sem
    /// <see cref="Aula"/> instanciada é simplesmente
    /// <see cref="StatusHistoricoFrequencia.NaoRegistrada"/>, sem criar
    /// side-effect no banco por causa de uma consulta.
    /// </summary>
    public async Task<IReadOnlyCollection<HistoricoFrequenciaPorProfessor>> ListarHistoricoAsync(
        Guid alunoUsuarioId, PeriodoConsulta periodo, CancellationToken cancellationToken)
    {
        var matriculas = await _matriculas.ListarPorAlunoAsync(alunoUsuarioId, cancellationToken);
        var resultado = new List<HistoricoFrequenciaPorProfessor>();
        foreach (var matricula in matriculas)
        {
            resultado.Add(await MontarHistoricoDaMatriculaAsync(matricula, periodo, cancellationToken));
        }

        return resultado;
    }

    private async Task<HistoricoFrequenciaPorProfessor> MontarHistoricoDaMatriculaAsync(
        Matricula matricula, PeriodoConsulta periodo, CancellationToken cancellationToken)
    {
        var professor = await _usuarios.BuscarPorIdAsync(matricula.ProfessorId, cancellationToken);
        var alocacoes = await _alocacoes.ListarPorMatriculaAsync(matricula.Id, cancellationToken);
        var aulas = new List<AulaFrequenciaHistorico>();
        foreach (var alocacao in alocacoes)
        {
            aulas.AddRange(await MontarHistoricoDaAlocacaoAsync(alocacao, matricula.Id, periodo, cancellationToken));
        }

        return new HistoricoFrequenciaPorProfessor(matricula.ProfessorId, professor?.Nome ?? string.Empty, aulas);
    }

    private async Task<IReadOnlyCollection<AulaFrequenciaHistorico>> MontarHistoricoDaAlocacaoAsync(
        AlocacaoHorario alocacao, Guid matriculaId, PeriodoConsulta periodo, CancellationToken cancellationToken)
    {
        var horario = await _horarios.BuscarPorIdAsync(alocacao.HorarioId, cancellationToken);
        if (horario is null)
        {
            return Array.Empty<AulaFrequenciaHistorico>();
        }

        var aulas = new List<AulaFrequenciaHistorico>();
        foreach (var data in periodo.GerarDatas(horario.DiaSemana))
        {
            var status = await CalcularStatusAsync(horario.Id, data, matriculaId, cancellationToken);
            aulas.Add(new AulaFrequenciaHistorico(horario.Id, data, horario.DiaSemana, horario.HoraInicio, status));
        }

        return aulas;
    }

    /// <summary>
    /// Leitura pura, nunca chama <see cref="AulaService.ObterOuCriarAulaAsync"/>
    /// (ver doc-comment de <see cref="ListarHistoricoAsync"/>). Cancelamento
    /// tem prioridade sobre qualquer outro status (edge point do card,
    /// checado antes de olhar <see cref="RegistroFrequencia"/>).
    /// </summary>
    private async Task<StatusHistoricoFrequencia> CalcularStatusAsync(
        Guid horarioId, DateOnly data, Guid matriculaId, CancellationToken cancellationToken)
    {
        var aula = await _aulas.BuscarPorHorarioEDataAsync(horarioId, data, cancellationToken);
        if (aula is null)
        {
            return StatusHistoricoFrequencia.NaoRegistrada;
        }

        var cancelamento = await _cancelamentos.BuscarAsync(aula.Id, matriculaId, cancellationToken);
        if (cancelamento is not null)
        {
            return StatusHistoricoFrequencia.Cancelada;
        }

        var registro = await _registros.BuscarAsync(aula.Id, matriculaId, cancellationToken);
        return registro?.StatusProfessor switch
        {
            null => StatusHistoricoFrequencia.NaoRegistrada,
            StatusFrequencia.Presente => StatusHistoricoFrequencia.Presente,
            StatusFrequencia.Ausente => StatusHistoricoFrequencia.Ausente,
            _ => StatusHistoricoFrequencia.NaoRegistrada,
        };
    }

    private async Task GarantirNaoCanceladaPeloAlunoAsync(Guid aulaId, Guid matriculaId, CancellationToken cancellationToken)
    {
        var cancelamento = await _cancelamentos.BuscarAsync(aulaId, matriculaId, cancellationToken);
        if (cancelamento is not null)
        {
            throw new AulaCanceladaPeloAlunoException(aulaId, matriculaId);
        }
    }

    private async Task<RegistroFrequencia> RegistrarUmAsync(
        Guid aulaId, Guid matriculaId, StatusFrequencia status, CancellationToken cancellationToken)
    {
        var registro = await _registros.BuscarAsync(aulaId, matriculaId, cancellationToken);
        if (registro is null)
        {
            registro = RegistroFrequencia.Criar(aulaId, matriculaId, _clock);
            await _registros.AdicionarAsync(registro, cancellationToken);
        }

        registro.RegistrarProfessor(status, _clock);
        return registro;
    }

    private async Task GarantirMatriculasAlocadasAsync(
        Guid horarioId, IEnumerable<Guid> matriculaIds, CancellationToken cancellationToken)
    {
        foreach (var matriculaId in matriculaIds)
        {
            var alocacao = await _alocacoes.BuscarAsync(horarioId, matriculaId, cancellationToken);
            if (alocacao is null)
            {
                // Nome ambíguo com Synclass.Domain.Alocacoes.AlocacaoNaoEncontradaException
                // (mesmo namespace importado neste arquivo) — reaproveita a
                // exceção de Synclass.Domain.Aulas, mesmo padrão de
                // AulaService.GarantirMatriculaAlocadaAsync (issue #10).
                throw new Synclass.Domain.Aulas.AlocacaoNaoEncontradaException(horarioId, matriculaId);
            }
        }
    }
}
