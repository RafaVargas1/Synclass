namespace Synclass.Domain.Matriculas;

/// <summary>
/// Abstrai a persistência de <see cref="Matricula"/>. Implementado em
/// Synclass.Infrastructure (EF Core), permitindo que o Domain e seus testes
/// de unidade não dependam de banco de dados.
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

    Task AdicionarAsync(Matricula matricula, CancellationToken cancellationToken);

    Task SalvarAsync(CancellationToken cancellationToken);
}
