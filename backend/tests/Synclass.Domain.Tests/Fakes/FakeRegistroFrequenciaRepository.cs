using Synclass.Domain.Frequencias;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Repositório em memória usado nos testes de unidade do Domain, no lugar de
/// um banco real (ver docs/spec/code-style.md#testes). Mesmo padrão de
/// <see cref="FakeCancelamentoAulaRepository"/>.
/// </summary>
public sealed class FakeRegistroFrequenciaRepository : IRegistroFrequenciaRepository
{
    private readonly List<RegistroFrequencia> _registros = new();

    public IReadOnlyCollection<RegistroFrequencia> Registros => _registros.AsReadOnly();

    public Task<RegistroFrequencia?> BuscarAsync(Guid aulaId, Guid matriculaId, CancellationToken cancellationToken)
    {
        var registro = _registros.FirstOrDefault(r => r.AulaId == aulaId && r.MatriculaId == matriculaId);
        return Task.FromResult(registro);
    }

    public Task AdicionarAsync(RegistroFrequencia registro, CancellationToken cancellationToken)
    {
        _registros.Add(registro);
        return Task.CompletedTask;
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
