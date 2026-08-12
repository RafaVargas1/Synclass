using Synclass.Domain.Autenticacao;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Repositório de <see cref="CodigoOtp"/> em memória, usado nos testes de
/// unidade do Domain (ver docs/spec/code-style.md#testes).
/// </summary>
public sealed class FakeCodigoOtpRepository : ICodigoOtpRepository
{
    private readonly List<CodigoOtp> _codigos = new();

    public IReadOnlyCollection<CodigoOtp> Codigos => _codigos.AsReadOnly();

    public Task<CodigoOtp?> BuscarMaisRecenteNaoUsadoAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var codigo = _codigos
            .Where(c => c.UsuarioId == usuarioId && c.UsadoEm is null)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefault();
        return Task.FromResult(codigo);
    }

    public Task AdicionarAsync(CodigoOtp codigo, CancellationToken cancellationToken)
    {
        _codigos.Add(codigo);
        return Task.CompletedTask;
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
