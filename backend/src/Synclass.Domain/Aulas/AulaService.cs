using Synclass.Domain.Alocacoes;
using Synclass.Domain.Common;
using Synclass.Domain.Configuracoes;
using Synclass.Domain.Horarios;

namespace Synclass.Domain.Aulas;

/// <summary>
/// Orquestra os 2 casos de uso do card (issue #10): cancelar uma aula já
/// marcada e listar as próximas aulas de uma matrícula. Depende de
/// <see cref="HorarioService"/> (não de <c>IHorarioRepository</c>
/// diretamente) para reaproveitar a checagem de posse "horário pertence a
/// este Professor", mesmo padrão de <c>AlocacaoHorarioService</c> — ver
/// docs/specs/10-cancelamento-aula/implementation.md.
/// </summary>
public sealed class AulaService
{
    private readonly IAulaRepository _aulas;
    private readonly ICancelamentoAulaRepository _cancelamentos;
    private readonly IAlocacaoHorarioRepository _alocacoes;
    private readonly IConfiguracaoProfessorRepository _configuracoes;
    private readonly HorarioService _horarioService;
    private readonly IClock _clock;

    public AulaService(
        IAulaRepository aulas,
        ICancelamentoAulaRepository cancelamentos,
        IAlocacaoHorarioRepository alocacoes,
        IConfiguracaoProfessorRepository configuracoes,
        HorarioService horarioService,
        IClock clock)
    {
        _aulas = aulas;
        _cancelamentos = cancelamentos;
        _alocacoes = alocacoes;
        _configuracoes = configuracoes;
        _horarioService = horarioService;
        _clock = clock;
    }

    public async Task<CancelamentoAula> CancelarAsync(
        Guid professorId, Guid horarioId, DateOnly data, Guid matriculaId, CancellationToken cancellationToken)
    {
        var horario = await _horarioService.BuscarDoProfessorAsync(professorId, horarioId, cancellationToken);
        await GarantirMatriculaAlocadaAsync(horarioId, matriculaId, cancellationToken);

        var aula = await ObterOuCriarAulaAsync(horarioId, data, cancellationToken);
        var cancelamento = await _cancelamentos.BuscarAsync(aula.Id, matriculaId, cancellationToken);
        if (cancelamento is not null)
        {
            return cancelamento;
        }

        await GarantirDentroDoPrazoAsync(professorId, aula, horario.HoraInicio, cancellationToken);

        var novoCancelamento = CancelamentoAula.Criar(aula.Id, matriculaId, _clock);
        await _cancelamentos.AdicionarAsync(novoCancelamento, cancellationToken);
        await _cancelamentos.SalvarAsync(cancellationToken);
        return novoCancelamento;
    }

    /// <summary>
    /// Instancia a <see cref="Aula"/> (ocorrência datada) sob demanda na
    /// primeira vez que é referenciada — RN explícita da issue #10.
    /// </summary>
    private async Task<Aula> ObterOuCriarAulaAsync(Guid horarioId, DateOnly data, CancellationToken cancellationToken)
    {
        var aulaExistente = await _aulas.BuscarPorHorarioEDataAsync(horarioId, data, cancellationToken);
        if (aulaExistente is not null)
        {
            return aulaExistente;
        }

        var aula = Aula.Criar(horarioId, data, _clock);
        await _aulas.AdicionarAsync(aula, cancellationToken);
        await _aulas.SalvarAsync(cancellationToken);
        return aula;
    }

    /// <summary>
    /// Compara o instante atual com <c>Aula.Data + Horario.HoraInicio -
    /// PrazoCancelamentoMinutos</c> (issue #10, AC1/AC2) — lê a configuração
    /// vigente a cada chamada, nunca cacheia o prazo em <see cref="AlocacaoHorario"/>
    /// ou <see cref="Aula"/> (AC5: mudança de prazo não reavalia cancelamentos
    /// antigos, só afeta os próximos). Ausência de configuração usa 0
    /// minutos (mesmo default de <c>ConfiguracaoProfessor.Criar</c>).
    /// </summary>
    private async Task GarantirDentroDoPrazoAsync(
        Guid professorId, Aula aula, TimeOnly horaInicio, CancellationToken cancellationToken)
    {
        var configuracao = await _configuracoes.BuscarPorProfessorAsync(professorId, cancellationToken);
        var prazoCancelamentoMinutos = configuracao?.PrazoCancelamentoMinutos ?? 0;

        // Sem tratamento de fuso horário — mesma simplificação do resto do
        // domínio (ver docs/specs/10-cancelamento-aula/implementation.md#edge-points).
        var inicioAula = new DateTimeOffset(aula.Data.ToDateTime(horaInicio), TimeSpan.Zero);
        var limite = inicioAula.AddMinutes(-prazoCancelamentoMinutos);
        if (_clock.UtcNow > limite)
        {
            throw new PrazoCancelamentoExpiradoException(aula.Id, prazoCancelamentoMinutos, limite);
        }
    }

    /// <summary>
    /// A matrícula precisa estar alocada *neste horário específico* — checagem
    /// distinta de <c>AlocacaoHorarioService.GarantirMatriculaVinculadaAsync</c>,
    /// que só confere o vínculo Aluno-Professor, não Aluno-Horário.
    /// </summary>
    private async Task GarantirMatriculaAlocadaAsync(Guid horarioId, Guid matriculaId, CancellationToken cancellationToken)
    {
        var alocacao = await _alocacoes.BuscarAsync(horarioId, matriculaId, cancellationToken);
        if (alocacao is null)
        {
            throw new AlocacaoNaoEncontradaException(horarioId, matriculaId);
        }
    }
}
