using Synclass.Domain.Common;

namespace Synclass.Domain.Horarios;

/// <summary>
/// Orquestra os 3 casos de uso do card (issue #6): cadastrar, listar e
/// remover horários disponíveis de um Professor. Cadastro valida duração e
/// rejeita sobreposição com horários já existentes no mesmo dia; remoção
/// rejeita quando existem Alunos alocados (ver implementation.md). Não exige
/// mais nenhuma configuração prévia do Professor — cada horário carrega sua
/// própria política de marcação (issue #73, <see cref="TipoMarcacao"/>);
/// a exigência de <c>ConfiguracaoProfessor</c> definida antes do cadastro
/// (issue #7) ficou obsoleta com isso e foi removida na issue #76, achado do
/// dev-review no PR #85 (sem essa remoção, um Professor sem configuração
/// prévia não conseguia mais cadastrar horário nenhum, já que a Tela deixou
/// de oferecer como defini-la). A alteração da política (issue #71) é o 4º
/// caso de uso: <see cref="AlterarPoliticaAsync"/>.
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
        Guid professorId,
        DiaSemana diaSemana,
        TimeOnly horaInicio,
        int duracaoMinutos,
        TipoMarcacao tipoMarcacao,
        CancellationToken cancellationToken,
        int? limiteAlunos = null)
    {
        var horario = Horario.Criar(professorId, diaSemana, horaInicio, duracaoMinutos, tipoMarcacao, _clock, limiteAlunos);
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

    /// <summary>
    /// Altera a política de marcação (<see cref="TipoMarcacao"/>) de um
    /// horário já cadastrado do Professor (issue #71), persistindo a mudança
    /// quando o horário pertence a ele — rejeita com
    /// <see cref="HorarioNaoEncontradoException"/> quando o horário não
    /// existe ou é de outro Professor (via
    /// <see cref="BuscarDoProfessorAsync"/>) e com
    /// <see cref="TipoMarcacaoInvalidoException"/> quando o valor está fora
    /// do enum (via <see cref="Horario.AlterarTipoMarcacao"/>).
    /// </summary>
    public async Task<Horario> AlterarPoliticaAsync(
        Guid professorId,
        Guid horarioId,
        TipoMarcacao novoTipo,
        CancellationToken cancellationToken)
    {
        var horario = await BuscarDoProfessorAsync(professorId, horarioId, cancellationToken);
        horario.AlterarTipoMarcacao(novoTipo);
        await _horarios.SalvarAsync(cancellationToken);
        return horario;
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
}
