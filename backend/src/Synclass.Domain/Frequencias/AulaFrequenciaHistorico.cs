using Synclass.Domain.Horarios;

namespace Synclass.Domain.Frequencias;

/// <summary>
/// Uma ocorrência (data concreta) de um <see cref="Horario"/> recorrente,
/// com o status de frequência resolvido para o histórico do Aluno (issue
/// #16) — ver <see cref="FrequenciaService.ListarHistoricoAsync"/>.
/// </summary>
public sealed record AulaFrequenciaHistorico(
    Guid HorarioId, DateOnly Data, DiaSemana DiaSemana, TimeOnly HoraInicio, StatusHistoricoFrequencia Status);
