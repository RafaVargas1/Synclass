using Synclass.Domain.Autenticacao;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Gera sempre o mesmo código configurado, no lugar de um aleatório real —
/// necessário para os testes de <see cref="LoginService"/> conseguirem
/// confirmar o código sem precisar capturá-lo de um gerador não
/// determinístico.
/// </summary>
public sealed class FakeGeradorDeCodigoOtp : IGeradorDeCodigoOtp
{
    private readonly string _codigo;

    public FakeGeradorDeCodigoOtp(string codigo = "123456")
    {
        _codigo = codigo;
    }

    public string Gerar()
    {
        return _codigo;
    }
}
