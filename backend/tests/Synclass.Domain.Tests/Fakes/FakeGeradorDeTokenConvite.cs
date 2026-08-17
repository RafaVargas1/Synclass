using Synclass.Domain.Convites;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Gera tokens sequenciais e previsíveis ("token-1", "token-2", ...) no
/// lugar de aleatoriedade real — permite aos testes de
/// <see cref="ConviteService"/> referenciar o token gerado sem precisar
/// capturá-lo de uma fonte não determinística.
/// </summary>
public sealed class FakeGeradorDeTokenConvite : IGeradorDeTokenConvite
{
    private int _contador;

    public string Gerar()
    {
        _contador++;
        return $"token-{_contador}";
    }
}
