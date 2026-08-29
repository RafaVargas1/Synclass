using Synclass.Domain.Autenticacao;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Fake de <see cref="IValidadorDeIdTokenApple"/> (issue #212): devolve um
/// <see cref="InformacoesIdTokenApple"/> previsível ou <c>null</c> conforme
/// configurado, para os testes de unidade do Domain exercitarem os cenários
/// do login Apple sem chamar a validação criptográfica real
/// (JwtSecurityTokenHandler + JWKS).
/// </summary>
public sealed class FakeValidadorDeIdTokenApple : IValidadorDeIdTokenApple
{
    private readonly InformacoesIdTokenApple? _resultado;

    public FakeValidadorDeIdTokenApple(InformacoesIdTokenApple? resultado)
    {
        _resultado = resultado;
    }

    public Task<InformacoesIdTokenApple?> ValidarAsync(string idToken, CancellationToken cancellationToken)
    {
        return Task.FromResult(_resultado);
    }
}
