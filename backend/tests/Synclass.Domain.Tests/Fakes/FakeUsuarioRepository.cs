using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Repositório em memória usado nos testes de unidade do Domain, no lugar de
/// um banco real (ver docs/spec/code-style.md#testes — mocke I/O externo com
/// classes fake nomeadas, não stubs inline).
/// </summary>
public sealed class FakeUsuarioRepository : IUsuarioRepository
{
    private readonly List<Usuario> _usuarios = new();

    public IReadOnlyCollection<Usuario> Usuarios => _usuarios.AsReadOnly();

    public Task<Usuario?> BuscarPorContatoAsync(string contatoNormalizado, CancellationToken cancellationToken)
    {
        var usuario = _usuarios.FirstOrDefault(u => u.Contato == contatoNormalizado);
        return Task.FromResult(usuario);
    }

    public Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken)
    {
        _usuarios.Add(usuario);
        return Task.CompletedTask;
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
