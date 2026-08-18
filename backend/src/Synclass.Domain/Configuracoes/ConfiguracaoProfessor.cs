using Synclass.Domain.Common;

namespace Synclass.Domain.Configuracoes;

/// <summary>
/// Configuração de agendamento de um Professor (issue #7) — 1 por Professor
/// (ver <c>ConfiguracaoProfessorConfiguration</c> para o índice único em
/// <see cref="ProfessorId"/>). Construtor privado + <see cref="Criar"/>
/// estático, mesmo padrão de <c>Horario</c> (issue #6).
/// </summary>
public sealed class ConfiguracaoProfessor
{
    private ConfiguracaoProfessor(
        Guid id,
        Guid professorId,
        ModeloAgendamento modeloAgendamento,
        int prazoCancelamentoMinutos,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        Id = id;
        ProfessorId = professorId;
        ModeloAgendamento = modeloAgendamento;
        PrazoCancelamentoMinutos = prazoCancelamentoMinutos;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid ProfessorId { get; private set; }

    public ModeloAgendamento ModeloAgendamento { get; private set; }

    /// <summary>
    /// Antecedência mínima, em minutos, exigida do Aluno para cancelar uma
    /// aula (issue #10) — <c>0</c> significa "sem antecedência mínima
    /// exigida", default para Professores cadastrados antes desta issue (ver
    /// docs/specs/10-cancelamento-aula/implementation.md).
    /// </summary>
    public int PrazoCancelamentoMinutos { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ConfiguracaoProfessor Criar(
        Guid professorId, ModeloAgendamento modeloAgendamento, IClock clock, int prazoCancelamentoMinutos = 0)
    {
        ValidarModeloAgendamento(modeloAgendamento);
        ValidarPrazoCancelamentoMinutos(prazoCancelamentoMinutos);
        return new ConfiguracaoProfessor(
            Guid.NewGuid(), professorId, modeloAgendamento, prazoCancelamentoMinutos, clock.UtcNow, clock.UtcNow);
    }

    /// <summary>
    /// Altera <see cref="PrazoCancelamentoMinutos"/> (issue #10, AC5) — não
    /// afeta cancelamentos já feitos, só os próximos: <c>AulaService</c>
    /// sempre lê o valor vigente no momento do cancelamento, nunca cacheia.
    /// </summary>
    public void AlterarPrazoCancelamento(int novoPrazoCancelamentoMinutos, IClock clock)
    {
        ValidarPrazoCancelamentoMinutos(novoPrazoCancelamentoMinutos);
        PrazoCancelamentoMinutos = novoPrazoCancelamentoMinutos;
        UpdatedAt = clock.UtcNow;
    }

    private static void ValidarPrazoCancelamentoMinutos(int prazoCancelamentoMinutos)
    {
        if (prazoCancelamentoMinutos < 0)
        {
            throw new PrazoCancelamentoMinutosInvalidoException(prazoCancelamentoMinutos);
        }
    }

    /// <summary>
    /// Sobrescreve o modelo e <see cref="UpdatedAt"/> — não guarda o modelo
    /// anterior (troca não é versionada, conforme Regra de Negócio da issue
    /// #7). Não bloqueia a troca por existirem alunos já alocados: a RN é
    /// explícita que a mudança não é retroativa nem desfaz vínculo (ver
    /// implementation.md#edge-points).
    /// </summary>
    public void AlterarModelo(ModeloAgendamento novoModelo, IClock clock)
    {
        ValidarModeloAgendamento(novoModelo);
        ModeloAgendamento = novoModelo;
        UpdatedAt = clock.UtcNow;
    }

    /// <summary>
    /// Rejeita valores fora do enum <see cref="ModeloAgendamento"/> — o
    /// contrato de Api trafega o modelo como inteiro (ver
    /// implementation.md#contrato-de-api), então um valor fora do range
    /// (ex: 99) chega até aqui sem checagem de tipo do compilador. Mesmo
    /// padrão de <c>Horario.ValidarDiaSemana</c> (issue #6).
    /// </summary>
    private static void ValidarModeloAgendamento(ModeloAgendamento modeloAgendamento)
    {
        if (!Enum.IsDefined(modeloAgendamento))
        {
            throw new ModeloAgendamentoInvalidoException((int)modeloAgendamento);
        }
    }

    /// <summary>
    /// Regra pura (sem I/O) usada pelas issues #8/#9 para decidir se um
    /// horário específico pode ser marcado livremente por um Aluno: Vago
    /// sempre permite; Fixo nunca permite; Híbrido permite só quando o
    /// horário consultado não tem atribuição fixa. Ver
    /// implementation.md#dependência-das-issues-8-e-9.
    /// </summary>
    public bool PermiteMarcacaoLivre(bool horarioPossuiAtribuicaoFixa)
    {
        return ModeloAgendamento switch
        {
            ModeloAgendamento.Vago => true,
            ModeloAgendamento.Fixo => false,
            ModeloAgendamento.Hibrido => !horarioPossuiAtribuicaoFixa,
            _ => throw new ArgumentOutOfRangeException(
                nameof(ModeloAgendamento), ModeloAgendamento,
                $"Modelo de agendamento inválido: {ModeloAgendamento}. Esperado um valor entre 0 e 2."),
        };
    }
}
