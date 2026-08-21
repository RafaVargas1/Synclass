using Synclass.Domain.Common;

namespace Synclass.Domain.Horarios;

/// <summary>
/// Um horário disponível, sempre um template recorrente (dia da semana +
/// hora + duração) — não uma ocorrência datada (ver Regra de Negócio da
/// issue #6 e a seção "Notas de modelagem" de requisitos-funcionais.md). A
/// duração é definida uma única vez na criação e nunca é alterável depois
/// (sem endpoint de update, só create/delete) — a única exceção é a política
/// de marcação, editável via <see cref="AlterarTipoMarcacao"/> (issue #71).
/// </summary>
public sealed class Horario
{
    private Horario(
        Guid id,
        Guid professorId,
        DiaSemana diaSemana,
        TimeOnly horaInicio,
        int duracaoMinutos,
        TipoMarcacao tipoMarcacao,
        DateTimeOffset createdAt,
        int limiteAlunos)
    {
        Id = id;
        ProfessorId = professorId;
        DiaSemana = diaSemana;
        HoraInicio = horaInicio;
        DuracaoMinutos = duracaoMinutos;
        TipoMarcacao = tipoMarcacao;
        CreatedAt = createdAt;
        LimiteAlunos = limiteAlunos;
    }

    public Guid Id { get; private set; }

    public Guid ProfessorId { get; private set; }

    public DiaSemana DiaSemana { get; private set; }

    public TimeOnly HoraInicio { get; private set; }

    public int DuracaoMinutos { get; private set; }

    /// <summary>
    /// Política de marcação deste Horário específico (issue #73), obrigatória
    /// na criação — ver <see cref="TipoMarcacao"/>. Editável depois da
    /// criação via <see cref="AlterarTipoMarcacao"/> (issue #71).
    /// </summary>
    public TipoMarcacao TipoMarcacao { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Quantos Alunos podem ocupar este horário (mínimo 1 — aula individual,
    /// e também o default quando o Professor não informa nada na criação;
    /// ver Regra de Negócio da issue #17).
    /// </summary>
    public int LimiteAlunos { get; private set; }

    /// <summary>
    /// Horário de término, calculado a partir de <see cref="HoraInicio"/> e
    /// <see cref="DuracaoMinutos"/>. Não trata horários que cruzam a meia-
    /// noite (fora de escopo da issue #6 — ver implementation.md#edge-points).
    /// </summary>
    public TimeOnly HoraFim => HoraInicio.AddMinutes(DuracaoMinutos);

    /// <summary>
    /// <paramref name="limiteAlunos"/> nulo aplica o default de
    /// <see cref="LimiteAlunosHorario.Padrao"/> (issue #17) — o Professor
    /// pode cadastrar um horário sem se preocupar com esse campo e ele nasce
    /// como aula individual.
    /// </summary>
    public static Horario Criar(
        Guid professorId,
        DiaSemana diaSemana,
        TimeOnly horaInicio,
        int duracaoMinutos,
        TipoMarcacao tipoMarcacao,
        IClock clock,
        int? limiteAlunos = null)
    {
        ValidarDiaSemana(diaSemana);
        DuracaoAula.Validar(duracaoMinutos);
        ValidarTipoMarcacao(tipoMarcacao);
        var limiteAlunosResolvido = limiteAlunos ?? LimiteAlunosHorario.Padrao;
        LimiteAlunosHorario.Validar(limiteAlunosResolvido);
        return new Horario(
            Guid.NewGuid(), professorId, diaSemana, horaInicio, duracaoMinutos, tipoMarcacao, clock.UtcNow, limiteAlunosResolvido);
    }

    /// <summary>
    /// Altera <see cref="LimiteAlunos"/>, rejeitando um valor menor que
    /// <paramref name="quantidadeAlunosAlocados"/> (issue #17 — reduzir
    /// abaixo da quantidade já alocada deixaria o horário inconsistente).
    /// Método de domínio puro: não consulta repositório, o chamador é quem
    /// traz a contagem real de <c>AlocacoesHorario</c> (issue #8) — ver
    /// docs/specs/17-limite-alunos-horario/implementation.md#dependência-da-issue-8,
    /// já que esse método ainda não é chamado por nenhum endpoint.
    /// </summary>
    public void AlterarLimiteAlunos(int novoLimite, int quantidadeAlunosAlocados)
    {
        LimiteAlunosHorario.Validar(novoLimite);
        if (novoLimite < quantidadeAlunosAlocados)
        {
            throw new LimiteAlunosMenorQueAlocadosException(novoLimite, quantidadeAlunosAlocados);
        }

        LimiteAlunos = novoLimite;
    }

    /// <summary>
    /// Altera <see cref="TipoMarcacao"/> de um horário já cadastrado (issue
    /// #71) — a única propriedade editável de um horário; duração, dia da
    /// semana, hora de início e <see cref="LimiteAlunos"/> permanecem
    /// imutáveis (ver docs/spec/business-rules.md#horários-e-política-de-marcação).
    /// Rejeita valor fora do enum com a mesma exceção usada na criação
    /// (<see cref="TipoMarcacaoInvalidoException"/>).
    /// </summary>
    public void AlterarTipoMarcacao(TipoMarcacao novoTipo)
    {
        ValidarTipoMarcacao(novoTipo);
        TipoMarcacao = novoTipo;
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
    /// Garante que <paramref name="tipoMarcacao"/> é um dos valores nomeados
    /// do enum, pelo mesmo motivo de <see cref="ValidarDiaSemana"/>.
    /// </summary>
    private static void ValidarTipoMarcacao(TipoMarcacao tipoMarcacao)
    {
        if (!Enum.IsDefined(tipoMarcacao))
        {
            throw new TipoMarcacaoInvalidoException((int)tipoMarcacao);
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
