namespace Synclass.Domain.Alocacoes;

/// <summary>
/// Quem iniciou uma <see cref="AlocacaoHorario"/> — o Professor atribuindo um
/// Aluno a um horário específico (issue #8) ou o Aluno se marcando livremente
/// em um horário vago (issue #9). Usada no modelo Híbrido para decidir se um
/// horário tem "atribuição fixa" (ao menos uma linha com <see cref="Professor"/>)
/// e por isso está bloqueado para marcação livre — ver
/// docs/specs/9-aluno-marca-horario-vago/implementation.md.
/// </summary>
public enum OrigemAlocacao
{
    Professor = 0,
    Aluno = 1,
}
