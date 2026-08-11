namespace Synclass.Domain.Horarios;

/// <summary>
/// Abstrai a persistência de <see cref="Horario"/>. Implementado em
/// Synclass.Infrastructure (EF Core), permitindo que o Domain e seus testes
/// de unidade não dependam de banco de dados.
/// </summary>
public interface IHorarioRepository
{
    Task<IReadOnlyCollection<Horario>> ListarPorProfessorEDiaAsync(Guid professorId, DiaSemana diaSemana, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Horario>> ListarPorProfessorAsync(Guid professorId, CancellationToken cancellationToken);

    Task<Horario?> BuscarPorIdAsync(Guid horarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Indica se existem Alunos alocados neste horário, em qualquer modelo
    /// de agendamento. A implementação EF Core retorna sempre <c>false</c>
    /// até a issue #8 modelar a alocação — ver implementation.md#dependência-da-issue-8.
    /// </summary>
    Task<bool> PossuiAlunosAlocadosAsync(Guid horarioId, CancellationToken cancellationToken);

    Task AdicionarAsync(Horario horario, CancellationToken cancellationToken);

    Task RemoverAsync(Horario horario, CancellationToken cancellationToken);

    Task SalvarAsync(CancellationToken cancellationToken);
}
