using FluentAssertions;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Usuarios;

public sealed class NomeUsuarioTests
{
    [Fact]
    public void Validar_NomeComEspacosNasPontas_RetornaTrimado()
    {
        var validado = NomeUsuario.Validar("  Maria Silva  ");

        validado.Should().Be("Maria Silva");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validar_NomeVazioOuSoEspacos_LancaNomeInvalidoException(string nomeBruto)
    {
        var acao = () => NomeUsuario.Validar(nomeBruto);

        acao.Should().Throw<NomeInvalidoException>();
    }
}
