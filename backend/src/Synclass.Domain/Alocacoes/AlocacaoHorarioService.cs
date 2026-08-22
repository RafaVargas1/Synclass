using Synclass.Domain.Common;
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
        var horario = await _horarioService.BuscarDoProfessorAsync(professorId, horarioId, cancellationToken);
        GarantirModeloPermiteAlocacao(horario);
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
            throw new HorarioLotadoException(horario, horario.LimiteAlunos);
        }
    }

    /// <summary>
    /// TipoMarcacao Livre não usa atribuição fixa pelo Professor (AC2) —
    /// Fixo ou Híbrido permitem. Checagem síncrona (issue #74): o
    /// <see cref="Horario"/> já foi buscado por quem chama, decisão passou a
    /// ser por horário, não mais por <c>ConfiguracaoProfessor</c>.
    /// </summary>
    private static void GarantirModeloPermiteAlocacao(Horario horario)
    {
        if (horario.TipoMarcacao == TipoMarcacao.Livre)
        {
            throw new ModeloNaoPermiteAlocacaoException(horario.Id);
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
    /// Aluno se marca livremente em um horário vago (issue #9) — mesma
    /// orquestração de <see cref="AlocarAsync"/>, mas usando a regra oposta
    /// de modelo (<see cref="GarantirModeloPermiteMarcacao"/>) e
    /// gravando <see cref="OrigemAlocacao.Aluno"/>.
    /// </summary>
    public async Task<AlocacaoHorario> MarcarAsync(
        Guid professorId, Guid horarioId, Guid matriculaId, CancellationToken cancellationToken)
    {
        var horario = await _horarioService.BuscarDoProfessorAsync(professorId, horarioId, cancellationToken);
        GarantirModeloPermiteMarcacao(horario);
        await GarantirVagaDisponivelAsync(horario, cancellationToken);
        await GarantirMatriculaVinculadaAsync(professorId, matriculaId, cancellationToken);
        await GarantirAindaNaoAlocadoAsync(horarioId, matriculaId, cancellationToken);

        var alocacao = AlocacaoHorario.Criar(horarioId, matriculaId, OrigemAlocacao.Aluno, _clock);
        await _alocacoes.AdicionarAsync(alocacao, cancellationToken);
        await _alocacoes.SalvarAsync(cancellationToken);
        return alocacao;
    }

    /// <summary>
    /// Regra oposta a <see cref="GarantirModeloPermiteAlocacao"/> — Livre e
    /// Híbrido sempre permitem, Fixo nunca permite (issue #74, AC3): o
    /// Híbrido deixou de olhar se este horário já tem atribuição fixa do
    /// Professor, aceita as duas coisas simultaneamente até a capacidade
    /// esgotar. Checagem síncrona — o <see cref="Horario"/> já foi buscado
    /// por quem chama.
    /// </summary>
    private static void GarantirModeloPermiteMarcacao(Horario horario)
    {
        if (horario.TipoMarcacao == TipoMarcacao.Fixo)
        {
            throw new ModeloNaoPermiteMarcacaoLivreException(horario.Id);
        }
    }

    /// <summary>
    /// Lista os horários deste Professor que o Aluno pode marcar livremente
    /// agora (issue #9) — GET é consulta, não ação: horário `Fixo` só
    /// devolve lista vazia (filtrado), sem lançar (diferente de
    /// <see cref="MarcarAsync"/>, que rejeita com exceção porque é uma
    /// tentativa de ação). Decisão passou a ser por
    /// <see cref="Horario.TipoMarcacao"/>, não mais por
    /// <c>ConfiguracaoProfessor</c> (issue #74).
    /// </summary>
    public async Task<IReadOnlyCollection<HorarioVago>> ListarVagosAsync(
        Guid professorId, Guid matriculaId, CancellationToken cancellationToken)
    {
        await GarantirMatriculaVinculadaAsync(professorId, matriculaId, cancellationToken);

        var horarios = await _horarioService.ListarAsync(professorId, cancellationToken);
        var vagos = new List<HorarioVago>();
        foreach (var horario in horarios)
        {
            var horarioVago = await ParaHorarioVagoSeElegivelAsync(horario, matriculaId, cancellationToken);
            if (horarioVago is not null)
            {
                vagos.Add(horarioVago);
            }
        }

        return vagos;
    }

    private async Task<HorarioVago?> ParaHorarioVagoSeElegivelAsync(
        Horario horario, Guid matriculaId, CancellationToken cancellationToken)
    {
        var alocacaoExistente = await _alocacoes.BuscarAsync(horario.Id, matriculaId, cancellationToken);
        if (alocacaoExistente is not null)
        {
            return null;
        }

        var quantidadeAlocada = await _alocacoes.ContarPorHorarioAsync(horario.Id, cancellationToken);
        var vagasRestantes = horario.LimiteAlunos - quantidadeAlocada;
        var elegivel = horario.TipoMarcacao is TipoMarcacao.Livre or TipoMarcacao.Hibrido && vagasRestantes > 0;
        return elegivel ? new HorarioVago(horario, vagasRestantes) : null;
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
    /// Resolve a Matrícula do Aluno autenticado com este Professor (issue
    /// #23) — usado por <c>MarcacoesHorarioController</c> para deixar de
    /// confiar em <c>matriculaId</c> enviado pelo cliente: o Aluno nunca
    /// escolhe a própria matrícula, ela é derivada do vínculo real
    /// (<see cref="IMatriculaRepository.BuscarVinculoAsync"/>, já usado
    /// desde o aceite de convite — issue #2/#5) entre a identidade do token
    /// e o Professor da rota.
    /// </summary>
    public async Task<Guid> ResolverMatriculaDoAlunoAsync(
        Guid professorId, Guid alunoUsuarioId, CancellationToken cancellationToken)
    {
        var matricula = await _matriculas.BuscarVinculoAsync(professorId, alunoUsuarioId, cancellationToken);
        if (matricula is null)
        {
            throw new AlunoNaoVinculadoAoProfessorException(alunoUsuarioId, professorId);
        }

        return matricula.Id;
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
