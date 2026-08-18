using Synclass.Domain.Common;

namespace Synclass.Domain.Aulas;

/// <summary>
/// Registra que uma <see cref="Aula"/> específica foi cancelada por um Aluno
/// (via <see cref="MatriculaId"/>) — não afeta a
/// <see cref="Synclass.Domain.Alocacoes.AlocacaoHorario"/> (vínculo
/// recorrente), só a ocorrência daquela data (issue #10, AC3). Índice único
/// (AulaId, MatriculaId) garante 1 cancelamento por Aluno por aula — ver
/// <c>CancelamentoAulaConfiguration</c>.
/// </summary>
public sealed class CancelamentoAula
{
    private CancelamentoAula(
        Guid id, Guid aulaId, Guid matriculaId, DateTimeOffset canceladoEm, DateTimeOffset createdAt)
    {
        Id = id;
        AulaId = aulaId;
        MatriculaId = matriculaId;
        CanceladoEm = canceladoEm;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid AulaId { get; private set; }

    public Guid MatriculaId { get; private set; }

    public DateTimeOffset CanceladoEm { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static CancelamentoAula Criar(Guid aulaId, Guid matriculaId, IClock clock)
    {
        return new CancelamentoAula(Guid.NewGuid(), aulaId, matriculaId, clock.UtcNow, clock.UtcNow);
    }
}
