namespace Synclass.Domain.Horarios;

/// <summary>
/// Lançada quando o horário sendo criado sobrepõe (<see cref="Horario.Sobrepoe"/>)
/// um horário já cadastrado do mesmo Professor no mesmo dia da semana.
/// </summary>
public sealed class HorarioConflitanteException : HorarioRejeitadoException
{
    public HorarioConflitanteException(Horario conflitante)
        : base($"Horário conflita com um já cadastrado: {conflitante.DiaSemana} " +
            $"{conflitante.HoraInicio:HH\\:mm}–{conflitante.HoraFim:HH\\:mm}.")
    {
        HorarioConflitanteId = conflitante.Id;
    }

    public Guid HorarioConflitanteId { get; }
}
