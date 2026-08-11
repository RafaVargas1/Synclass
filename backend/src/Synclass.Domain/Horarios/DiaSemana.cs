namespace Synclass.Domain.Horarios;

/// <summary>
/// Dia da semana de um <see cref="Horario"/> recorrente. Ordenação idêntica
/// a <see cref="System.DayOfWeek"/> (Domingo = 0 .. Sábado = 6) — ver
/// Critérios técnicos da issue #6. Trafega como inteiro no contrato de Api
/// (não como string), decisão documentada em
/// docs/specs/6-horarios-disponiveis/implementation.md#contrato-de-api.
/// </summary>
public enum DiaSemana
{
    Domingo = 0,
    Segunda = 1,
    Terca = 2,
    Quarta = 3,
    Quinta = 4,
    Sexta = 5,
    Sabado = 6,
}
