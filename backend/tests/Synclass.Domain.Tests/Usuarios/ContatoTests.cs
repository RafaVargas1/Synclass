using FluentAssertions;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Usuarios;

public sealed class ContatoTests
{
    [Theory]
    [InlineData("  Maria@Exemplo.com  ", "maria@exemplo.com")]
    [InlineData("MARIA@EXEMPLO.COM", "maria@exemplo.com")]
    public void Normalizar_Email_TrimAndLowercase(string bruto, string esperado)
    {
        var normalizado = Contato.Normalizar(bruto);

        normalizado.Should().Be(esperado);
    }

    [Theory]
    [InlineData("(11) 98765-4321", "11987654321")]
    [InlineData("11 3456-7890", "1134567890")]
    public void Normalizar_Telefone_ApenasDigitos(string bruto, string esperado)
    {
        var normalizado = Contato.Normalizar(bruto);

        normalizado.Should().Be(esperado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sem-arroba-exemplo.com")]
    [InlineData("nome@")]
    public void Normalizar_EmailInvalido_LancaContatoInvalidoException(string bruto)
    {
        var acao = () => Contato.Normalizar(bruto);

        acao.Should().Throw<ContatoInvalidoException>();
    }

    [Theory]
    [InlineData("123456789")] // 9 dígitos: falta um dígito para DDD+número
    [InlineData("123456789012")] // 12 dígitos: excede o formato BR
    public void Normalizar_TelefoneForaDoFormatoBr_LancaContatoInvalidoException(string bruto)
    {
        var acao = () => Contato.Normalizar(bruto);

        acao.Should().Throw<ContatoInvalidoException>();
    }

    [Fact]
    public void Normalizar_EmailMaiorQueTamanhoMaximo_LancaContatoInvalidoException()
    {
        var localMuitoLongo = new string('a', Contato.TamanhoMaximo);
        var emailMuitoLongo = $"{localMuitoLongo}@exemplo.com";

        var acao = () => Contato.Normalizar(emailMuitoLongo);

        acao.Should().Throw<ContatoInvalidoException>();
    }

    [Fact]
    public void IdentificarTipo_ContatoNormalizadoComArroba_RetornaEmail()
    {
        Contato.IdentificarTipo("maria@exemplo.com").Should().Be(TipoContato.Email);
    }

    [Fact]
    public void IdentificarTipo_ContatoNormalizadoSemArroba_RetornaTelefone()
    {
        Contato.IdentificarTipo("11987654321").Should().Be(TipoContato.Telefone);
    }
}
