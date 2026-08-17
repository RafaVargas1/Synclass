namespace Synclass.Domain.Alocacoes;

/// <summary>
/// Abstrai a persistência de <see cref="AlocacaoHorario"/>. Implementado em
/// Synclass.Infrastructure (EF Core), permitindo que o Domain e seus testes
/// de unidade não dependam de banco de dados.
/// </summary>
public interface IAlocacaoHorarioRepository
{
    /// <summary>
    /// Busca a alocação de um Aluno específico neste horário, se existir —
    /// usado para checar duplicidade antes de alocar e para localizar a
    /// linha a remover em <see cref="AlocacaoHorarioService.DesalocarAsync"/>.
    /// </summary>
    Task<AlocacaoHorario?> BuscarAsync(Guid horarioId, Guid matriculaId, CancellationToken cancellationToken);

    /// <summary>
    /// Conta quantos Alunos já estão alocados neste horário — comparado com
    /// <c>Horario.LimiteAlunos</c> para checar vaga disponível.
    /// </summary>
    Task<int> ContarPorHorarioAsync(Guid horarioId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<AlocacaoHorario>> ListarPorHorarioAsync(Guid horarioId, CancellationToken cancellationToken);

    Task AdicionarAsync(AlocacaoHorario alocacao, CancellationToken cancellationToken);

    Task RemoverAsync(AlocacaoHorario alocacao, CancellationToken cancellationToken);

    Task SalvarAsync(CancellationToken cancellationToken);
}
