using Synclass.Domain.Autenticacao;

namespace Synclass.Api.Tests.Fakes;

/// <summary>
/// Fake de <see cref="IValidadorDeIdTokenGoogle"/> (issue #65) para os
/// testes de fumaça de <c>POST /auth/google</c>: devolve um
/// <see cref="InformacoesIdTokenGoogle"/> fixo por cenário (e-mail valido
/// ou e-mail nao verificado) ou <c>null</c> para token invalido, sem
/// depender do SDK <c>Google.Apis.Auth</c> no teste.
/// </summary>
public sealed class ValidadorDeIdTokenGoogleFixo : IValidadorDeIdTokenGoogle
{
    public InformacoesIdTokenGoogle? Resultado { get; set; }

    public Task<InformacoesIdTokenGoogle?> ValidarAsync(string idToken, CancellationToken cancellationToken)
    {
        return Task.FromResult(Resultado);
    }
}
