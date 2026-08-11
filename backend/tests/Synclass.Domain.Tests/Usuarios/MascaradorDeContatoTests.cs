using FluentAssertions;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Usuarios;

public sealed class MascaradorDeContatoTests
{
    [Fact]
    public void Mascarar_Email_NaoExpoeContatoEmTextoPleno()
    {
        var mascarado = MascaradorDeContato.Mascarar("maria@exemplo.com");

        mascarado.Should().NotBe("maria@exemplo.com");
        mascarado.Should().Contain("*");
    }

    [Fact]
    public void Mascarar_MesmoContato_ProduzMesmaMascara()
    {
        var primeira = MascaradorDeContato.Mascarar("11987654321");
        var segunda = MascaradorDeContato.Mascarar("11987654321");

        primeira.Should().Be(segunda);
    }

    [Fact]
    public void Mascarar_ContatoCurto_NaoLancaExcecao()
    {
        var mascarado = MascaradorDeContato.Mascarar("ab");

        mascarado.Should().NotBeNullOrEmpty();
    }
}
