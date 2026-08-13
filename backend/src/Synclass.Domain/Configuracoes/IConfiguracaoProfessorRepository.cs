namespace Synclass.Domain.Configuracoes;

/// <summary>
/// Abstrai a persistência de <see cref="ConfiguracaoProfessor"/>. Implementado
/// em Synclass.Infrastructure (EF Core), mesmo padrão de
/// <c>IHorarioRepository</c> (issue #6) — "alterar" é só mutar a entidade já
/// rastreada e chamar <see cref="SalvarAsync"/>, sem método de update próprio.
/// </summary>
public interface IConfiguracaoProfessorRepository
{
    Task<ConfiguracaoProfessor?> BuscarPorProfessorAsync(Guid professorId, CancellationToken cancellationToken);

    Task AdicionarAsync(ConfiguracaoProfessor configuracao, CancellationToken cancellationToken);

    Task SalvarAsync(CancellationToken cancellationToken);
}
