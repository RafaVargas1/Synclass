using Synclass.Domain.Alocacoes;
using Synclass.Domain.Common;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;

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
    private readonly IMatriculaRepository _matriculas;
    private readonly HorarioService _horarioService;
    private readonly IClock _clock;

    public AulaService(
        IAulaRepository aulas,
        ICancelamentoAulaRepository cancelamentos,
        IAlocacaoHorarioRepository alocacoes,
        IMatriculaRepository matriculas,
        HorarioService horarioService,
        IClock clock)
    {
        _aulas = aulas;
        _cancelamentos = cancelamentos;
        _alocacoes = alocacoes;
        _matriculas = matriculas;
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

        GarantirDentroDoPrazo(aula, horario);

        var novoCancelamento = CancelamentoAula.Criar(aula.Id, matriculaId, _clock);
        await _cancelamentos.AdicionarAsync(novoCancelamento, cancellationToken);
        await _cancelamentos.SalvarAsync(cancellationToken);
        return novoCancelamento;
    }

    /// <summary>
    /// Instancia a <see cref="Aula"/> (ocorrência datada) sob demanda na
    /// primeira vez que é referenciada — RN explícita da issue #10.
    /// Promovido de <c>private</c> para <c>internal</c> na issue #14 para
    /// ser reaproveitado por <c>Synclass.Domain.Frequencias.FrequenciaService</c>
    /// sem duplicar a lógica de instanciação sob demanda.
    /// </summary>
    internal async Task<Aula> ObterOuCriarAulaAsync(Guid horarioId, DateOnly data, CancellationToken cancellationToken)
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
    /// Horario.PrazoCancelamentoMinutos</c> (issue #10, AC1/AC2; prazo
    /// migrado de <c>ConfiguracaoProfessor</c> para <see cref="Horario"/> na
    /// issue #187, para ser configurável por Horário em vez de único por
    /// Professor) — lê o valor vigente do <see cref="Horario"/> a cada
    /// chamada, nunca cacheia em <see cref="AlocacaoHorario"/> ou
    /// <see cref="Aula"/> (AC5: mudança de prazo não reavalia cancelamentos
    /// antigos, só afeta os próximos).
    /// </summary>
    private void GarantirDentroDoPrazo(Aula aula, Horario horario)
    {
        var prazoCancelamentoMinutos = horario.PrazoCancelamentoMinutos;

        var limite = CalcularLimiteCancelamento(aula.Data, horario.HoraInicio, prazoCancelamentoMinutos);
        if (_clock.UtcNow > limite)
        {
            throw new PrazoCancelamentoExpiradoException(aula.Id, prazoCancelamentoMinutos, limite);
        }
    }

    /// <summary>
    /// Regra pura (sem I/O) compartilhada por <see cref="GarantirDentroDoPrazoAsync"/>
    /// e <see cref="ListarProximasAsync"/> — até quando é possível cancelar
    /// uma ocorrência em <paramref name="data"/>/<paramref name="horaInicio"/>
    /// dado o prazo vigente. Sem tratamento de fuso horário — mesma
    /// simplificação do resto do domínio (ver
    /// docs/specs/10-cancelamento-aula/implementation.md#edge-points).
    /// </summary>
    private static DateTimeOffset CalcularLimiteCancelamento(DateOnly data, TimeOnly horaInicio, int prazoCancelamentoMinutos)
    {
        var inicioAula = new DateTimeOffset(data.ToDateTime(horaInicio), TimeSpan.Zero);
        return inicioAula.AddMinutes(-prazoCancelamentoMinutos);
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

    /// <summary>
    /// Lista, para cada horário em que a matrícula está alocada, a próxima
    /// ocorrência futura ainda não cancelada — issue #10. Ver
    /// docs/specs/10-cancelamento-aula/implementation.md#decisão-de-implementação
    /// sobre mostrar só 1 ocorrência por alocação, não uma janela de semanas.
    /// </summary>
    public async Task<IReadOnlyCollection<AulaProxima>> ListarProximasAsync(
        Guid professorId, Guid matriculaId, CancellationToken cancellationToken)
    {
        await GarantirMatriculaVinculadaAsync(professorId, matriculaId, cancellationToken);

        var alocacoes = await _alocacoes.ListarPorMatriculaAsync(matriculaId, cancellationToken);
        var proximas = new List<AulaProxima>();
        foreach (var alocacao in alocacoes)
        {
            var horario = await _horarioService.BuscarDoProfessorAsync(professorId, alocacao.HorarioId, cancellationToken);
            var proxima = await CalcularProximaOcorrenciaAsync(horario, matriculaId, cancellationToken);
            proximas.Add(proxima);
        }

        return proximas;
    }

    /// <summary>
    /// Avança semana a semana a partir de hoje até achar uma ocorrência de
    /// <paramref name="horario"/> que ainda não tem <see cref="CancelamentoAula"/>
    /// desta matrícula — não mostra uma aula já cancelada como "próxima".
    /// </summary>
    private async Task<AulaProxima> CalcularProximaOcorrenciaAsync(
        Horario horario, Guid matriculaId, CancellationToken cancellationToken)
    {
        var data = ProximaDataDoDiaSemana(horario.DiaSemana, horario.HoraInicio);
        while (await ExisteCancelamentoAsync(horario.Id, data, matriculaId, cancellationToken))
        {
            data = data.AddDays(7);
        }

        var prazoCancelamentoMinutos = horario.PrazoCancelamentoMinutos;
        var cancelavelAte = CalcularLimiteCancelamento(data, horario.HoraInicio, prazoCancelamentoMinutos);
        var podeCancelar = _clock.UtcNow <= cancelavelAte;
        return new AulaProxima(
            horario.Id, data, horario.DiaSemana, horario.HoraInicio, horario.DuracaoMinutos,
            podeCancelar, cancelavelAte, prazoCancelamentoMinutos);
    }

    private async Task<bool> ExisteCancelamentoAsync(
        Guid horarioId, DateOnly data, Guid matriculaId, CancellationToken cancellationToken)
    {
        var aula = await _aulas.BuscarPorHorarioEDataAsync(horarioId, data, cancellationToken);
        if (aula is null)
        {
            return false;
        }

        var cancelamento = await _cancelamentos.BuscarAsync(aula.Id, matriculaId, cancellationToken);
        return cancelamento is not null;
    }

    /// <summary>
    /// Primeira data ≥ hoje cujo dia da semana bate com <paramref name="diaSemana"/>;
    /// se hoje bate mas <paramref name="horaInicio"/> já passou, pula para a
    /// semana seguinte (sem tratamento de fuso horário, mesma simplificação
    /// do resto do domínio).
    /// </summary>
    private DateOnly ProximaDataDoDiaSemana(DiaSemana diaSemana, TimeOnly horaInicio)
    {
        var agora = _clock.UtcNow;
        var hoje = DateOnly.FromDateTime(agora.UtcDateTime);
        var diasAteODia = ((int)diaSemana - (int)hoje.DayOfWeek + 7) % 7;
        var data = hoje.AddDays(diasAteODia);

        var horaJaPassouHoje = diasAteODia == 0 && TimeOnly.FromDateTime(agora.UtcDateTime) >= horaInicio;
        return horaJaPassouHoje ? data.AddDays(7) : data;
    }

    /// <summary>
    /// Mesma checagem de <c>AlocacaoHorarioService.GarantirMatriculaVinculadaAsync</c>
    /// (issue #8) — não reaproveitada diretamente por ser <c>private</c> em
    /// outra classe; duplicação mínima e intencional, ver
    /// docs/spec/code-style.md#sem-duplicação-de-código.
    /// </summary>
    private async Task GarantirMatriculaVinculadaAsync(Guid professorId, Guid matriculaId, CancellationToken cancellationToken)
    {
        var matricula = await _matriculas.BuscarPorIdAsync(matriculaId, cancellationToken);
        if (matricula is null || matricula.ProfessorId != professorId)
        {
            throw new MatriculaNaoVinculadaAoProfessorException(matriculaId, professorId);
        }
    }
}
