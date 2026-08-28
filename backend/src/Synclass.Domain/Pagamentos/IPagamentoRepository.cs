namespace Synclass.Domain.Pagamentos;

/// <summary>
/// Abstrai a persistência de <see cref="Pagamento"/>. Implementado em
/// Synclass.Infrastructure (EF Core), permitindo que o Domain e seus testes
/// de unidade não dependam de banco. A unicidade do pendente por
/// (MatriculaId, período) é garantida pela busca de
/// <see cref="BuscarPendentePorMatriculaEPeriodoAsync"/> na lógica de
/// domínio, não por constraint de banco (ver
/// implementation.md#migration).
/// </summary>
public interface IPagamentoRepository
{
    Task<Pagamento?> BuscarPendentePorMatriculaEPeriodoAsync(
        Guid matriculaId, DateOnly inicio, DateOnly fimExclusivo, CancellationToken ct);

    Task<Pagamento?> BuscarConfirmadoPorMatriculaEPeriodoAsync(
        Guid matriculaId, DateOnly inicio, DateOnly fimExclusivo, CancellationToken ct);

    Task AdicionarAsync(Pagamento pagamento, CancellationToken ct);

    Task<List<Pagamento>> ListarPorMatriculaAsync(Guid matriculaId, CancellationToken ct);
}
