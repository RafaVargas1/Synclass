using Synclass.Domain.Autenticacao;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Fake de <see cref="IValidadorDeIdTokenGoogle"/> (issue #65): devolve um
/// <see cref="InformacoesIdTokenGoogle"/> previsível ou <c>null</c> conforme
/// configurado, para os testes de unidade do Domain exercitarem os quatro
/// cenários do login Google sem chamar o SDK real de validação.
/// </summary>
public sealed class FakeValidadorDeIdTokenGoogle : IValidadorDeIdTokenGoogle
{
    private readonly InformacoesIdTokenGoogle? _resultado;

    public FakeValidadorDeIdTokenGoogle(InformacoesIdTokenGoogle? resultado)
    {
        _resultado = resultado;
    }

    public Task<InformacoesIdTokenGoogle?> ValidarAsync(string idToken, CancellationToken cancellationToken)
    {
        return Task.FromResult(_resultado);
    }
}
