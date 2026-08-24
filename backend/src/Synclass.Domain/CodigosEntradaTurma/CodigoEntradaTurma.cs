using Synclass.Domain.Common;

namespace Synclass.Domain.CodigosEntradaTurma;

/// <summary>
/// Código curto gerado pelo Professor pra qualquer Aluno autenticado entrar
/// na turma digitando o código — diferente de <see cref="Synclass.Domain.Convites.Convite"/>:
/// não é vinculado a um contato específico e não é de uso único.
/// Intencionalmente de vida curta (<see cref="MinutosDeValidade"/>) porque
/// substitui a checagem "vinculado a um contato" por uma janela de tempo
/// curta — qualquer Aluno com o código durante a aula/momento em que o
/// Professor o exibe pode entrar, quantas vezes for (ver task.md desta
/// feature).
/// </summary>
public sealed class CodigoEntradaTurma
{
    private const int MinutosDeValidade = 5;

    private CodigoEntradaTurma(Guid id, Guid professorId, string codigo, DateTimeOffset expiraEm, DateTimeOffset createdAt)
    {
        Id = id;
        ProfessorId = professorId;
        Codigo = codigo;
        ExpiraEm = expiraEm;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid ProfessorId { get; private set; }

    public string Codigo { get; private set; }

    public DateTimeOffset ExpiraEm { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Cria um código válido por <see cref="MinutosDeValidade"/> minutos a
    /// partir de agora. <paramref name="codigo"/> já validado/gerado por
    /// <see cref="Synclass.Domain.Convites.IGeradorDeCodigoConvite"/> — esta
    /// factory não gera aleatoriedade, só monta a entidade.
    /// </summary>
    public static CodigoEntradaTurma Gerar(Guid professorId, string codigo, IClock clock)
    {
        var agora = clock.UtcNow;
        return new CodigoEntradaTurma(Guid.NewGuid(), professorId, codigo, agora.AddMinutes(MinutosDeValidade), agora);
    }

    public bool EstaAtivo(DateTimeOffset agora)
    {
        return agora < ExpiraEm;
    }
}
