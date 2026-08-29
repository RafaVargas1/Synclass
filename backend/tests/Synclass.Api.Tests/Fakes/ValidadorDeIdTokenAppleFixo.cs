using Synclass.Domain.Autenticacao;

namespace Synclass.Api.Tests.Fakes;

/// <summary>
/// Fake de <see cref="IValidadorDeIdTokenApple"/> (issue #212) para os
/// testes de fumaça de <c>POST /auth/apple</c>: devolve um
/// <see cref="InformacoesIdTokenApple"/> fixo por cenário (e-mail valido
/// ou e-mail nao verificado) ou <c>null</c> para token invalido, sem
/// depender da validação criptográfica real (JWT + JWKS) no teste.
/// </summary>
public sealed class ValidadorDeIdTokenAppleFixo : IValidadorDeIdTokenApple
{
    public InformacoesIdTokenApple? Resultado { get; set; }

    public Task<InformacoesIdTokenApple?> ValidarAsync(string idToken, CancellationToken cancellationToken)
    {
        return Task.FromResult(Resultado);
    }
}
