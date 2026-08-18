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
        await GarantirAindaNaoAlocadoAsync(horarioId, matriculaId, cancellationToken);

        var alocacao = AlocacaoHorario.Criar(horarioId, matriculaId, OrigemAlocacao.Professor, _clock);
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

    /// <summary>
    /// Checagem de aplicação — o índice único (HorarioId, MatriculaId) é o
    /// guard rail final contra a corrida concorrente (ver
    /// <see cref="AlocacaoJaExisteException"/>).
    /// </summary>
    private async Task GarantirAindaNaoAlocadoAsync(Guid horarioId, Guid matriculaId, CancellationToken cancellationToken)
    {
        var existente = await _alocacoes.BuscarAsync(horarioId, matriculaId, cancellationToken);
        if (existente is not null)
        {
            throw new AlocacaoJaExisteException(horarioId, matriculaId);
        }
    }

    /// <summary>
    /// Desfaz a alocação de um Aluno a um horário (AC4) — remove a linha de
    /// <see cref="AlocacaoHorario"/> inteira, diferente de um cancelamento
    /// pontual de uma data (issue #10, fora de escopo aqui). Alocações do
    /// mesmo Aluno em outros horários não são afetadas (independentes entre
    /// si).
    /// </summary>
    public async Task DesalocarAsync(Guid professorId, Guid horarioId, Guid matriculaId, CancellationToken cancellationToken)
    {
        await _horarioService.BuscarDoProfessorAsync(professorId, horarioId, cancellationToken);

        var alocacao = await _alocacoes.BuscarAsync(horarioId, matriculaId, cancellationToken);
        if (alocacao is null)
        {
            throw new AlocacaoNaoEncontradaException(horarioId, matriculaId);
        }

        await _alocacoes.RemoverAsync(alocacao, cancellationToken);
        await _alocacoes.SalvarAsync(cancellationToken);
    }

    /// <summary>
    /// Lista os Alunos já alocados em um horário — usado pelo <c>GET</c> do
    /// controller. Confere a posse do horário antes de listar, mesmo padrão
    /// de <see cref="AlocarAsync"/>/<see cref="DesalocarAsync"/>.
    /// </summary>
    public async Task<IReadOnlyCollection<AlocacaoHorario>> ListarPorHorarioAsync(
        Guid professorId, Guid horarioId, CancellationToken cancellationToken)
    {
        await _horarioService.BuscarDoProfessorAsync(professorId, horarioId, cancellationToken);
        return await _alocacoes.ListarPorHorarioAsync(horarioId, cancellationToken);
    }
}
