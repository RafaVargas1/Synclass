using Synclass.Domain.Common;

namespace Synclass.Domain.Horarios;

/// <summary>
/// Orquestra os 3 casos de uso do card (issue #6): cadastrar, listar e
/// remover horários disponíveis de um Professor. Cadastro valida duração e
/// rejeita sobreposição com horários já existentes no mesmo dia; remoção
/// rejeita quando existem Alunos alocados (ver implementation.md).
/// </summary>
public sealed class HorarioService
{
    private readonly IHorarioRepository _horarios;
    private readonly IClock _clock;

    public HorarioService(IHorarioRepository horarios, IClock clock)
    {
        _horarios = horarios;
        _clock = clock;
    }

    public async Task<Horario> CadastrarAsync(
        Guid professorId, DiaSemana diaSemana, TimeOnly horaInicio, int duracaoMinutos, CancellationToken cancellationToken)
    {
        var horario = Horario.Criar(professorId, diaSemana, horaInicio, duracaoMinutos, _clock);
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

    private async Task<Horario> BuscarDoProfessorAsync(Guid professorId, Guid horarioId, CancellationToken cancellationToken)
    {
        var horario = await _horarios.BuscarPorIdAsync(horarioId, cancellationToken);
        if (horario is null || horario.ProfessorId != professorId)
        {
            throw new HorarioNaoEncontradoException(horarioId);
        }

        return horario;
    }
}
