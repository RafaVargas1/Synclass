using Synclass.Domain.Common;

namespace Synclass.Domain.Horarios;

/// <summary>
/// Um horário disponível, sempre um template recorrente (dia da semana +
/// hora + duração) — não uma ocorrência datada (ver Regra de Negócio da
/// issue #6 e a seção "Notas de modelagem" de requisitos-funcionais.md). A
/// duração é definida uma única vez na criação e nunca é alterável depois
/// (sem endpoint de update, só create/delete).
/// </summary>
public sealed class Horario
{
    private Horario(Guid id, Guid professorId, DiaSemana diaSemana, TimeOnly horaInicio, int duracaoMinutos, DateTimeOffset createdAt)
    {
        Id = id;
        ProfessorId = professorId;
        DiaSemana = diaSemana;
        HoraInicio = horaInicio;
        DuracaoMinutos = duracaoMinutos;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ProfessorId { get; private set; }

    public DiaSemana DiaSemana { get; private set; }

    public TimeOnly HoraInicio { get; private set; }

    public int DuracaoMinutos { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Horário de término, calculado a partir de <see cref="HoraInicio"/> e
    /// <see cref="DuracaoMinutos"/>. Não trata horários que cruzam a meia-
    /// noite (fora de escopo da issue #6 — ver implementation.md#edge-points).
    /// </summary>
    public TimeOnly HoraFim => HoraInicio.AddMinutes(DuracaoMinutos);

    public static Horario Criar(Guid professorId, DiaSemana diaSemana, TimeOnly horaInicio, int duracaoMinutos, IClock clock)
    {
        ValidarDiaSemana(diaSemana);
        DuracaoAula.Validar(duracaoMinutos);
        return new Horario(Guid.NewGuid(), professorId, diaSemana, horaInicio, duracaoMinutos, clock.UtcNow);
    }

    /// <summary>
    /// Garante que <paramref name="diaSemana"/> é um dos valores nomeados do
    /// enum. Necessário porque um cast direto de int (ex: no controller, a
    /// partir do contrato de Api) não é validado pelo compilador — um valor
    /// como 99 passaria incólume até aqui sem esta checagem.
    /// </summary>
    private static void ValidarDiaSemana(DiaSemana diaSemana)
    {
        if (!Enum.IsDefined(diaSemana))
        {
            throw new DiaSemanaInvalidoException((int)diaSemana);
        }
    }

    /// <summary>
    /// Verifica sobreposição com outro horário: mesmo dia da semana e
    /// intervalos [HoraInicio, HoraFim) que se cruzam. Bordas que só se
    /// tocam (um termina exatamente quando o outro começa) não contam como
    /// conflito — ver Critérios técnicos da issue #6.
    /// </summary>
    public bool Sobrepoe(Horario outro)
    {
        if (DiaSemana != outro.DiaSemana)
        {
            return false;
        }

        return HoraInicio < outro.HoraFim && outro.HoraInicio < HoraFim;
    }
}
