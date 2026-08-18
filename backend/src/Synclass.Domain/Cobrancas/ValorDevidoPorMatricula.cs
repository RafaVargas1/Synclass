namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Valor devido calculado para um vínculo (<see cref="MatriculaId"/>) num
/// <see cref="PeriodoConsulta"/> — não é acoplado a Professor nem a Aluno,
/// reaproveitável nas duas direções pela issue #13 (ver
/// implementation.md#reaproveitamento-pela-issue-13). <see cref="Valor"/> é
/// <c>null</c> quando <see cref="SemRegraDefinida"/> é <c>true</c> — nunca
/// <c>0</c> (critério de aceite 3 da issue #12).
/// </summary>
public sealed record ValorDevidoPorMatricula(
    Guid MatriculaId, Guid? AlunoUsuarioId, string Nome, decimal? Valor, bool SemRegraDefinida);
