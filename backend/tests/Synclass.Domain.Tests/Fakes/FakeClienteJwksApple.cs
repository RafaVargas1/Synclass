using Microsoft.IdentityModel.Tokens;
using Synclass.Domain.Autenticacao;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Fake de <see cref="IClienteJwksApple"/> (issue #212): devolve um conjunto
/// fixo de <see cref="SecurityKey"/> (chaves RSA de teste), para
/// <see cref="ValidadorDeIdTokenApple"/> ser testado sem I/O real contra a
/// API da Apple.
/// </summary>
public sealed class FakeClienteJwksApple : IClienteJwksApple
{
    private readonly IReadOnlyCollection<SecurityKey> _chaves;

    public FakeClienteJwksApple(params SecurityKey[] chaves)
    {
        _chaves = chaves;
    }

    public Task<IReadOnlyCollection<SecurityKey>> ObterChavesAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_chaves);
    }
}
