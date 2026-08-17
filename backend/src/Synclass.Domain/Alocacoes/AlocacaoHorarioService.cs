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
    private readonly IConfiguracaoProfessorRepository _configuracoes;
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
        _configuracoes = configuracoes;
        _horarioService = horarioService;
        _clock = clock;
    }

    public async Task<AlocacaoHorario> AlocarAsync(
        Guid professorId, Guid horarioId, Guid matriculaId, CancellationToken cancellationToken)
    {
        var horario = await _horarioService.BuscarDoProfessorAsync(professorId, horarioId, cancellationToken);
        await GarantirModeloPermiteAlocacaoAsync(professorId, cancellationToken);
        await GarantirVagaDisponivelAsync(horario, cancellationToken);
        await GarantirMatriculaVinculadaAsync(professorId, matriculaId, cancellationToken);

        var alocacao = AlocacaoHorario.Criar(horarioId, matriculaId, _clock);
        await _alocacoes.AdicionarAsync(alocacao, cancellationToken);
        await _alocacoes.SalvarAsync(cancellationToken);
        return alocacao;
    }

    /// <summary>
    /// Checagem de aplicação (AC3) — não é atômica com o <c>INSERT</c>, sem
    /// guard rail equivalente a nível de banco (não é uma constraint de
    /// unicidade, é uma contagem); risco de corrida aceito, ver
    /// docs/specs/8-aluno-horario/implementation.md#edge-points.
    /// </summary>
    private async Task GarantirVagaDisponivelAsync(Horario horario, CancellationToken cancellationToken)
    {
        var quantidadeAlocada = await _alocacoes.ContarPorHorarioAsync(horario.Id, cancellationToken);
        if (quantidadeAlocada >= horario.LimiteAlunos)
        {
            throw new HorarioLotadoException(horario.Id, horario.LimiteAlunos);
        }
    }

    /// <summary>
    /// Modelo Vago não usa atribuição fixa pelo Professor (AC2) — qualquer
    /// outro modelo (Fixo ou Híbrido) permite. Ausência de configuração é
    /// tratada como Vago, defensivamente (ver
    /// docs/specs/8-aluno-horario/implementation.md#edge-points).
    /// </summary>
    private async Task GarantirModeloPermiteAlocacaoAsync(Guid professorId, CancellationToken cancellationToken)
    {
        var configuracao = await _configuracoes.BuscarPorProfessorAsync(professorId, cancellationToken);
        if (configuracao is null || configuracao.ModeloAgendamento == ModeloAgendamento.Vago)
        {
            throw new ModeloNaoPermiteAlocacaoException(professorId);
        }
    }

    /// <summary>
    /// AC5 — a Matrícula precisa existir e pertencer a este Professor; os
    /// dois casos rejeitam com a mesma exceção, sem distinguir na resposta.
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
