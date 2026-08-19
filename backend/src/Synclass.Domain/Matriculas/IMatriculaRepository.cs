namespace Synclass.Domain.Matriculas;

/// <summary>
/// Abstrai a persistência de <see cref="Matricula"/>. Implementado em
/// Synclass.Infrastructure (EF Core), permitindo que o Domain e seus testes
/// de unidade não dependam de banco de dados.
/// <see cref="BuscarVinculoAsync"/> é sempre escopado por
/// <c>professorId</c> — nunca bloqueia um segundo Professor diferente para
/// o mesmo Aluno, o que sustenta a relação N:N entre Aluno e Professor
/// (issue #5, formalizada em teste sem exigir mudança de comportamento).
/// </summary>
public interface IMatriculaRepository
{
    /// <summary>
    /// Busca uma matrícula provisória pelo identificador escolhido pelo
    /// Professor, escopado por Professor — a unicidade é por
    /// <c>(ProfessorId, IdentificadorProvisorio)</c>, nunca global.
    /// </summary>
    Task<Matricula?> BuscarPorIdentificadorAsync(
        Guid professorId, string identificadorProvisorio, CancellationToken cancellationToken);

    /// <summary>
    /// Busca uma matrícula pelo <see cref="Matricula.Id"/> — usado ao aceitar
    /// um convite gerado a partir de uma matrícula de origem específica
    /// (issue #2), para promover exatamente aquela linha em vez de casar por
    /// nome/contato.
    /// </summary>
    Task<Matricula?> BuscarPorIdAsync(Guid matriculaId, CancellationToken cancellationToken);

    /// <summary>
    /// Busca a matrícula plena que já vincula este Professor a este Aluno
    /// (por identidade de usuário), se existir — usado para rejeitar convite
    /// duplicado (issue #2, critério de aceite 4) e para não duplicar o
    /// vínculo no aceite sem matrícula de origem.
    /// </summary>
    Task<Matricula?> BuscarVinculoAsync(Guid professorId, Guid alunoUsuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Lista todas as Matrículas (provisórias e plenas) de um Professor —
    /// usado para alimentar o seletor de Aluno da alocação a horário (issue
    /// #8, endpoint <c>GET /professores/{professorId}/alunos-provisorios</c>).
    /// </summary>
    Task<IReadOnlyCollection<Matricula>> ListarPorProfessorAsync(Guid professorId, CancellationToken cancellationToken);

    /// <summary>
    /// Lista todas as Matrículas plenas do Aluno autenticado — usado pela
    /// consulta de valor devido do Aluno (issue #13). Como
    /// <see cref="Matricula.AlunoUsuarioId"/> só é definido em matrículas
    /// plenas (ver <see cref="Matricula.Promover"/>), nunca devolve
    /// matrícula provisória.
    /// </summary>
    Task<IReadOnlyCollection<Matricula>> ListarPorAlunoAsync(Guid alunoUsuarioId, CancellationToken cancellationToken);

    Task AdicionarAsync(Matricula matricula, CancellationToken cancellationToken);

    Task SalvarAsync(CancellationToken cancellationToken);
}
