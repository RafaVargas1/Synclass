using Synclass.Domain.Convites;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Repositório em memória usado nos testes de unidade do Domain, no lugar de
/// um banco real (ver docs/spec/code-style.md#testes — mocke I/O externo com
/// classes fake nomeadas, não stubs inline).
/// </summary>
public sealed class FakeConviteRepository : IConviteRepository
{
    private readonly List<Convite> _convites = new();

    public IReadOnlyCollection<Convite> Convites => _convites.AsReadOnly();

    public Task<Convite?> BuscarPorTokenAsync(string token, CancellationToken cancellationToken)
    {
        var convite = _convites.FirstOrDefault(c => c.Token == token);
        return Task.FromResult(convite);
    }

    public Task<bool> ExisteCodigoAtivoAsync(string codigo, DateTimeOffset agora, CancellationToken cancellationToken)
    {
        var existe = _convites.Any(c => c.Codigo == codigo && c.UsadoEm is null && c.ExpiraEm > agora);
        return Task.FromResult(existe);
    }

    public Task AdicionarAsync(Convite convite, CancellationToken cancellationToken)
    {
        _convites.Add(convite);
        return Task.CompletedTask;
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
