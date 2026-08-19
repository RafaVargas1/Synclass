namespace Synclass.Domain.Frequencias;

/// <summary>
/// Histórico de frequência de um Aluno agrupado por Professor — mesma forma
/// de agregação de <see cref="Synclass.Domain.Cobrancas.ValorDevidoPorMatricula"/>
/// (issue #13), mesma RN: nunca somar/misturar ocorrências entre
/// Professores diferentes.
/// </summary>
public sealed record HistoricoFrequenciaPorProfessor(
    Guid ProfessorId, string NomeProfessor, IReadOnlyCollection<AulaFrequenciaHistorico> Aulas);
