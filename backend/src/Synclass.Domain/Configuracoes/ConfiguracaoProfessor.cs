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
        Guid id, Guid professorId, ModeloAgendamento modeloAgendamento, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        ProfessorId = professorId;
        ModeloAgendamento = modeloAgendamento;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public Guid Id { get; private set; }

    public Guid ProfessorId { get; private set; }

    public ModeloAgendamento ModeloAgendamento { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ConfiguracaoProfessor Criar(Guid professorId, ModeloAgendamento modeloAgendamento, IClock clock)
    {
        return new ConfiguracaoProfessor(Guid.NewGuid(), professorId, modeloAgendamento, clock.UtcNow, clock.UtcNow);
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
        ModeloAgendamento = novoModelo;
        UpdatedAt = clock.UtcNow;
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
                nameof(ModeloAgendamento), ModeloAgendamento, $"Modelo de agendamento inválido: {ModeloAgendamento}."),
        };
    }
}
