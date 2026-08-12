using FluentAssertions;
using Synclass.Domain.Matriculas;

namespace Synclass.Domain.Tests.Matriculas;

public sealed class IdentificadorProvisorioTests
{
    [Fact]
    public void Validar_IdentificadorComEspacosNasPontas_RetornaTrimado()
    {
        var validado = IdentificadorProvisorio.Validar("  2024-013  ");

        validado.Should().Be("2024-013");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validar_IdentificadorVazioOuSoEspacos_LancaIdentificadorProvisorioInvalidoException(string identificadorBruto)
    {
        var acao = () => IdentificadorProvisorio.Validar(identificadorBruto);

        acao.Should().Throw<IdentificadorProvisorioInvalidoException>();
    }

    [Fact]
    public void Validar_IdentificadorMaiorQueTamanhoMaximo_LancaIdentificadorProvisorioInvalidoException()
    {
        var identificadorMuitoLongo = new string('a', IdentificadorProvisorio.TamanhoMaximo + 1);

        var acao = () => IdentificadorProvisorio.Validar(identificadorMuitoLongo);

        acao.Should().Throw<IdentificadorProvisorioInvalidoException>();
    }

    [Fact]
    public void Validar_IdentificadorNoLimiteDoTamanhoMaximo_NaoLanca()
    {
        var identificadorNoLimite = new string('a', IdentificadorProvisorio.TamanhoMaximo);

        var validado = IdentificadorProvisorio.Validar(identificadorNoLimite);

        validado.Should().HaveLength(IdentificadorProvisorio.TamanhoMaximo);
    }
}
