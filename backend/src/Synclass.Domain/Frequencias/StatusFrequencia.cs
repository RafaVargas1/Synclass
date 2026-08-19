namespace Synclass.Domain.Frequencias;

/// <summary>
/// Status de presença registrado pelo Professor para um Aluno em uma
/// <see cref="Synclass.Domain.Aulas.Aula"/> específica (issue #14). Trafega
/// como inteiro no banco/contrato Domain, mesma decisão já usada por
/// <c>ModeloAgendamento</c>/<c>DiaSemana</c>.
/// </summary>
public enum StatusFrequencia
{
    Ausente = 0,
    Presente = 1,
}
