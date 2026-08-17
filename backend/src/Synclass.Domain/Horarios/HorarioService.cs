using Synclass.Domain.Common;
using Synclass.Domain.Configuracoes;

namespace Synclass.Domain.Horarios;

/// <summary>
/// Orquestra os 3 casos de uso do card (issue #6): cadastrar, listar e
/// remover horários disponíveis de um Professor. Cadastro exige que o
/// Professor já tenha definido um modelo de agendamento (issue #7), valida
/// duração e rejeita sobreposição com horários já existentes no mesmo dia;
/// remoção rejeita quando existem Alunos alocados (ver implementation.md).
/// </summary>
public sealed class HorarioService
{
    private readonly IHorarioRepository _horarios;
    private readonly IConfiguracaoProfessorRepository _configuracoes;
    private readonly IClock _clock;

    public HorarioService(IHorarioRepository horarios, IConfiguracaoProfessorRepository configuracoes, IClock clock)
    {
        _horarios = horarios;
        _configuracoes = configuracoes;
        _clock = clock;
    }

    public async Task<Horario> CadastrarAsync(
        Guid professorId,
        DiaSemana diaSemana,
        TimeOnly horaInicio,
        int duracaoMinutos,
        CancellationToken cancellationToken,
        int? limiteAlunos = null)
    {
        await GarantirConfiguracaoDefinidaAsync(professorId, cancellationToken);

        var horario = Horario.Criar(professorId, diaSemana, horaInicio, duracaoMinutos, _clock, limiteAlunos);
        var horariosDoDia = await _horarios.ListarPorProfessorEDiaAsync(professorId, diaSemana, cancellationToken);
        var conflitante = horariosDoDia.FirstOrDefault(existente => horario.Sobrepoe(existente));
        if (conflitante is not null)
        {
            throw new HorarioConflitanteException(conflitante);
        }

        await _horarios.AdicionarAsync(horario, cancellationToken);
        await _horarios.SalvarAsync(cancellationToken);
        return horario;
    }

    public Task<IReadOnlyCollection<Horario>> ListarAsync(Guid professorId, CancellationToken cancellationToken)
    {
        return _horarios.ListarPorProfessorAsync(professorId, cancellationToken);
    }

    public async Task RemoverAsync(Guid professorId, Guid horarioId, CancellationToken cancellationToken)
    {
        var horario = await BuscarDoProfessorAsync(professorId, horarioId, cancellationToken);

        var possuiAlunosAlocados = await _horarios.PossuiAlunosAlocadosAsync(horarioId, cancellationToken);
        if (possuiAlunosAlocados)
        {
            throw new HorarioComAlunosAlocadosException(horarioId);
        }

        await _horarios.RemoverAsync(horario, cancellationToken);
        await _horarios.SalvarAsync(cancellationToken);
    }

    /// <summary>
    /// <c>internal</c> (não <c>private</c>) para ser reaproveitado por
    /// <see cref="Synclass.Domain.Alocacoes.AlocacaoHorarioService"/> (issue
    /// #8), que precisa da mesma checagem de posse "horário pertence a este
    /// Professor" sem duplicar a lógica (docs/spec/code-style.md — sem
    /// duplicação de código). Seguro porque ambos vivem no assembly
    /// Synclass.Domain.
    /// </summary>
    internal async Task<Horario> BuscarDoProfessorAsync(Guid professorId, Guid horarioId, CancellationToken cancellationToken)
    {
        var horario = await _horarios.BuscarPorIdAsync(horarioId, cancellationToken);
        if (horario is null || horario.ProfessorId != professorId)
        {
            throw new HorarioNaoEncontradoException(horarioId);
        }

        return horario;
    }

    /// <summary>
    /// Exige configuração definida só no cadastro (issue #7) — <see cref="ListarAsync"/>
    /// e <see cref="RemoverAsync"/> não checam, conforme Critérios técnicos do
    /// card (ver implementation.md#edge-points).
    /// </summary>
    private async Task GarantirConfiguracaoDefinidaAsync(Guid professorId, CancellationToken cancellationToken)
    {
        var configuracao = await _configuracoes.BuscarPorProfessorAsync(professorId, cancellationToken);
        if (configuracao is null)
        {
            throw new ModeloAgendamentoNaoDefinidoException(professorId);
        }
    }
}
