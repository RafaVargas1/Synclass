using Synclass.Domain.Common;

namespace Synclass.Domain.Matriculas;

/// <summary>
/// Vínculo entre um Professor e um Aluno — identidade de usuário e
/// "matrícula" são conceitos separados (ver
/// docs/backlog/requisitos-funcionais.md, "Notas de modelagem para etapas
/// futuras"). Uma Matricula nasce provisória (<see cref="AlunoUsuarioId"/>
/// nulo, identificada só por <see cref="NomeProvisorio"/> e
/// <see cref="IdentificadorProvisorio"/> escolhidos pelo Professor — issue
/// #3) e pode ser promovida para plena (<see cref="Promover"/>) quando o
/// Aluno completa cadastro via convite direcionado (issue #2), preservando
/// o mesmo <see cref="Id"/> e portanto todo histórico associado via FK.
/// </summary>
public sealed class Matricula
{
    private Matricula(
        Guid id,
        Guid professorId,
        Guid? alunoUsuarioId,
        string? nomeProvisorio,
        string? identificadorProvisorio,
        DateTimeOffset createdAt)
    {
        Id = id;
        ProfessorId = professorId;
        AlunoUsuarioId = alunoUsuarioId;
        NomeProvisorio = nomeProvisorio;
        IdentificadorProvisorio = identificadorProvisorio;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ProfessorId { get; private set; }

    /// <summary>
    /// Identidade de usuário do Aluno, nula enquanto a matrícula é
    /// provisória. Definida por <see cref="Promover"/>.
    /// </summary>
    public Guid? AlunoUsuarioId { get; private set; }

    public string? NomeProvisorio { get; private set; }

    public string? IdentificadorProvisorio { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Cria uma matrícula provisória — sem exigir contato, e-mail, telefone
    /// ou login do Aluno (Regra de Negócio da issue #3).
    /// </summary>
    public static Matricula CriarProvisoria(
        Guid professorId, string nomeValidado, string identificadorValidado, IClock clock)
    {
        return new Matricula(Guid.NewGuid(), professorId, null, nomeValidado, identificadorValidado, clock.UtcNow);
    }

    /// <summary>
    /// Cria uma matrícula já plena, sem passar pelo estado provisório —
    /// caminho do aceite de convite (issue #2) quando o Aluno completa
    /// cadastro sem uma matrícula de origem específica (nasceu de um
    /// cadastro completo, não de um registro provisório do Professor, então
    /// não tem <see cref="NomeProvisorio"/>/<see cref="IdentificadorProvisorio"/>).
    /// </summary>
    public static Matricula CriarVinculada(Guid professorId, Guid alunoUsuarioId, IClock clock)
    {
        return new Matricula(Guid.NewGuid(), professorId, alunoUsuarioId, null, null, clock.UtcNow);
    }

    /// <summary>
    /// Promove a matrícula provisória para plena, associando-a à identidade
    /// de usuário recém-criada do Aluno. Nunca cria uma segunda linha — só
    /// define <see cref="AlunoUsuarioId"/> nesta, preservando
    /// <see cref="Id"/> e o histórico (agendamento, frequência, cobrança) já
    /// associado via FK. Disparado a partir do fluxo de aceite de convite
    /// (issue #2).
    /// </summary>
    public void Promover(Guid alunoUsuarioId)
    {
        if (AlunoUsuarioId is not null)
        {
            throw new MatriculaJaPromovidaException(Id);
        }

        AlunoUsuarioId = alunoUsuarioId;
    }
}
