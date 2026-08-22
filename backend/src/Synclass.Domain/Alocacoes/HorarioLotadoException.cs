using Synclass.Domain.Horarios;

namespace Synclass.Domain.Alocacoes;

/// <summary>
/// Lançada quando a quantidade de Alunos já alocados no horário atingiu
/// <c>Horario.LimiteAlunos</c> (AC3 da issue #8) — a checagem não é atômica
/// com o `INSERT` (corrida concorrente possível, sem guard rail no banco;
/// ver docs/specs/8-aluno-horario/implementation.md#edge-points). Mensagem
/// identifica o horário por dia+hora (mesmo padrão de
/// <see cref="HorarioConflitanteException"/>), não pelo Guid — o Guid não
/// significa nada pra quem lê o erro.
/// </summary>
public sealed class HorarioLotadoException : AlocacaoRejeitadaException
{
    public HorarioLotadoException(Horario horario, int limiteAlunos)
        : base($"O horário {horario.DiaSemana} {horario.HoraInicio:HH\\:mm} já atingiu o limite de {limiteAlunos} Aluno(s) alocado(s).")
    {
        HorarioId = horario.Id;
    }

    public Guid HorarioId { get; }
}
