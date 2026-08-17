using Synclass.Domain.Common;
using Synclass.Domain.Configuracoes;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;

namespace Synclass.Domain.Alocacoes;

/// <summary>
/// Orquestra os 3 casos de uso do card (issue #8): alocar um Aluno a um
/// horário específico, desfazer essa alocação e listar os Alunos já
/// alocados em um horário. Depende de <see cref="HorarioService"/> (não de
/// <c>IHorarioRepository</c> diretamente) para reaproveitar a checagem de
/// posse "horário pertence a este Professor" já implementada ali — ver
/// docs/specs/8-aluno-horario/implementation.md.
/// </summary>
public sealed class AlocacaoHorarioService
{
    private readonly IAlocacaoHorarioRepository _alocacoes;
    private readonly IMatriculaRepository _matriculas;
    private readonly HorarioService _horarioService;
    private readonly IClock _clock;

    public AlocacaoHorarioService(
        IAlocacaoHorarioRepository alocacoes,
        IMatriculaRepository matriculas,
        IConfiguracaoProfessorRepository configuracoes,
        HorarioService horarioService,
        IClock clock)
    {
        _alocacoes = alocacoes;
        _matriculas = matriculas;
        _horarioService = horarioService;
        _clock = clock;
    }

    public async Task<AlocacaoHorario> AlocarAsync(
        Guid professorId, Guid horarioId, Guid matriculaId, CancellationToken cancellationToken)
    {
        await _horarioService.BuscarDoProfessorAsync(professorId, horarioId, cancellationToken);

        var alocacao = AlocacaoHorario.Criar(horarioId, matriculaId, _clock);
        await _alocacoes.AdicionarAsync(alocacao, cancellationToken);
        await _alocacoes.SalvarAsync(cancellationToken);
        return alocacao;
    }
}
