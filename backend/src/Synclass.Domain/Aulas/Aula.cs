using Synclass.Domain.Common;

namespace Synclass.Domain.Aulas;

/// <summary>
/// Ocorrência datada de um <see cref="Synclass.Domain.Horarios.Horario"/>
/// (template recorrente) — identificada pelo par (<see cref="HorarioId"/>,
/// <see cref="Data"/>). Instanciada sob demanda pelo primeiro evento que a
/// referencia (cancelamento, issue #10; futuramente confirmação/frequência,
/// itens 14+), nunca criada preventivamente para todas as semanas futuras —
/// ver docs/specs/10-cancelamento-aula/implementation.md#edge-points.
/// </summary>
public sealed class Aula
{
    private Aula(Guid id, Guid horarioId, DateOnly data, DateTimeOffset createdAt)
    {
        Id = id;
        HorarioId = horarioId;
        Data = data;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid HorarioId { get; private set; }

    public DateOnly Data { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Aula Criar(Guid horarioId, DateOnly data, IClock clock)
    {
        return new Aula(Guid.NewGuid(), horarioId, data, clock.UtcNow);
    }
}
