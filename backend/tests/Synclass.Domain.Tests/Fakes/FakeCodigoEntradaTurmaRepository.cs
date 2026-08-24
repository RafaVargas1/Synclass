using Synclass.Domain.CodigosEntradaTurma;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Repositório em memória usado nos testes de unidade do Domain, no lugar de
/// um banco real (ver docs/spec/code-style.md#testes — mocke I/O externo com
/// classes fake nomeadas, não stubs inline).
/// </summary>
public sealed class FakeCodigoEntradaTurmaRepository : ICodigoEntradaTurmaRepository
{
    private readonly List<CodigoEntradaTurma> _codigos = new();

    public IReadOnlyCollection<CodigoEntradaTurma> Codigos => _codigos.AsReadOnly();

    public Task<bool> ExisteCodigoAtivoAsync(string codigo, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        var existe = _codigos.Any(c => c.Codigo == codigo && c.ExpiraEm > agora);
        return Task.FromResult(existe);
    }

    public Task<CodigoEntradaTurma?> BuscarAtivoPorCodigoAsync(string codigo, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        var codigoEntrada = _codigos
            .Where(c => c.Codigo == codigo && c.ExpiraEm > agora)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefault();
        return Task.FromResult(codigoEntrada);
    }

    public Task AdicionarAsync(CodigoEntradaTurma codigoEntradaTurma, CancellationToken cancellationToken)
    {
        _codigos.Add(codigoEntradaTurma);
        return Task.CompletedTask;
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
