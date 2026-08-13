namespace Synclass.Domain.Configuracoes;

/// <summary>
/// Modelo de agendamento escolhido pelo Professor (issue #7), determina como
/// os horários cadastrados por ele (issue #6) são preenchidos. Trafega como
/// inteiro no contrato de Api (não como string), mesma decisão já tomada
/// para <c>DiaSemana</c> na issue #6 (ver
/// docs/specs/6-horarios-disponiveis/implementation.md#contrato-de-api).
/// </summary>
public enum ModeloAgendamento
{
    /// <summary>Qualquer horário disponível pode ser marcado livremente por um Aluno.</summary>
    Vago = 0,

    /// <summary>Só o Professor atribui Alunos a horários — o Aluno nunca marca sozinho.</summary>
    Fixo = 1,

    /// <summary>
    /// Horários fixos são atribuídos primeiro pelo Professor; os que
    /// sobrarem ficam abertos para marcação livre, seguindo a regra de
    /// <see cref="ModeloAgendamento.Vago"/>.
    /// </summary>
    Hibrido = 2,
}
