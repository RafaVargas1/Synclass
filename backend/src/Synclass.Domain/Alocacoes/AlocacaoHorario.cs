namespace Synclass.Domain.Alocacoes;

/// <summary>
/// Vincula um Aluno (via <see cref="MatriculaId"/>) a um horário disponível
/// (<see cref="HorarioId"/> — sempre um template recorrente, nunca uma
/// ocorrência datada; ver docs/specs/8-aluno-horario/implementation.md#edge-points).
/// Entidade "burra": não valida modelo de agendamento, vaga disponível ou
/// vínculo do Aluno com o Professor — toda regra de negócio fica em
/// <see cref="AlocacaoHorarioService"/>, que já precisa buscar essas
/// informações para orquestrar, evitando duplicar consultas.
/// </summary>
public sealed class AlocacaoHorario
{
    private AlocacaoHorario(Guid id, Guid horarioId, Guid matriculaId, DateTimeOffset createdAt)
    {
        Id = id;
        HorarioId = horarioId;
        MatriculaId = matriculaId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid HorarioId { get; private set; }

    public Guid MatriculaId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static AlocacaoHorario Criar(Guid horarioId, Guid matriculaId, Common.IClock clock)
    {
        return new AlocacaoHorario(Guid.NewGuid(), horarioId, matriculaId, clock.UtcNow);
    }
}
