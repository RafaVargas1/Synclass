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

    [Fact]
    public void Validar_NomeMaiorQueTamanhoMaximo_LancaNomeInvalidoException()
    {
        var nomeMuitoLongo = new string('a', NomeUsuario.TamanhoMaximo + 1);

        var acao = () => NomeUsuario.Validar(nomeMuitoLongo);

        acao.Should().Throw<NomeInvalidoException>();
    }

    [Fact]
    public void Validar_NomeNoLimiteDoTamanhoMaximo_NaoLanca()
    {
        var nomeNoLimite = new string('a', NomeUsuario.TamanhoMaximo);

        var validado = NomeUsuario.Validar(nomeNoLimite);

        validado.Should().HaveLength(NomeUsuario.TamanhoMaximo);
    }
}
