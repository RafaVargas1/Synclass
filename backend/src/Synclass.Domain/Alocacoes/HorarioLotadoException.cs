namespace Synclass.Domain.Alocacoes;

/// <summary>
/// Lançada quando a quantidade de Alunos já alocados no horário atingiu
/// <c>Horario.LimiteAlunos</c> (AC3 da issue #8) — a checagem não é atômica
/// com o `INSERT` (corrida concorrente possível, sem guard rail no banco;
/// ver docs/specs/8-aluno-horario/implementation.md#edge-points).
/// </summary>
public sealed class HorarioLotadoException : AlocacaoRejeitadaException
{
    public HorarioLotadoException(Guid horarioId, int limiteAlunos)
        : base($"O horário {horarioId} já atingiu o limite de {limiteAlunos} Aluno(s) alocado(s).")
    {
    }
}
