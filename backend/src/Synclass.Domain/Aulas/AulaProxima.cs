using Synclass.Domain.Horarios;

namespace Synclass.Domain.Aulas;

/// <summary>
/// Próxima ocorrência futura de um horário em que a matrícula está alocada
/// (issue #10, <see cref="AulaService.ListarProximasAsync"/>) — calculada,
/// não necessariamente uma <see cref="Aula"/> já persistida (ver
/// docs/specs/10-cancelamento-aula/implementation.md#contrato-de-api).
/// </summary>
public sealed record AulaProxima(
    Guid HorarioId,
    DateOnly Data,
    DiaSemana DiaSemana,
    TimeOnly HoraInicio,
    int DuracaoMinutos,
    bool PodeCancelar,
    DateTimeOffset CancelavelAte,
    int PrazoCancelamentoMinutos);
