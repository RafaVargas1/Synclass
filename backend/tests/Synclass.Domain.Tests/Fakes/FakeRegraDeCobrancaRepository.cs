using Synclass.Domain.Cobrancas;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Repositório em memória usado nos testes de unidade do Domain, no lugar de
/// um banco real (ver docs/spec/code-style.md#testes). Simula o upsert real
/// da implementação EF Core: <see cref="SalvarAsync"/> nunca deixa duas
/// linhas para a mesma <see cref="RegraDeCobranca.MatriculaId"/>.
/// </summary>
public sealed class FakeRegraDeCobrancaRepository : IRegraDeCobrancaRepository
{
    private readonly List<RegraDeCobranca> _regras = new();

    public IReadOnlyCollection<RegraDeCobranca> Regras => _regras.AsReadOnly();

    public Task<RegraDeCobranca?> BuscarPorMatriculaAsync(Guid matriculaId, CancellationToken cancellationToken)
    {
        var regra = _regras.FirstOrDefault(r => r.MatriculaId == matriculaId);
        return Task.FromResult(regra);
    }

    public Task SalvarAsync(RegraDeCobranca regra, CancellationToken cancellationToken)
    {
        _regras.RemoveAll(r => r.MatriculaId == regra.MatriculaId);
        _regras.Add(regra);
        return Task.CompletedTask;
    }
}
