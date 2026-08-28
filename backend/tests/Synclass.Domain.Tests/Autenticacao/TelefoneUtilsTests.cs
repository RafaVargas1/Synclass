using FluentAssertions;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Autenticacao;

/// <summary>
/// Cobre a normalização E.164 de telefone (edge point explícito do card
/// #193): o provedor de WhatsApp exige E.164, e o contato já normalizado por
/// <c>Contato.Normalizar</c> (10-11 dígitos BR, sem DDI) precisa dessa
/// conversão final para ser aceito.
/// </summary>
public sealed class TelefoneUtilsTests
{
    [Theory]
    [InlineData("+5511987654321", "+5511987654321")] // já E.164
    [InlineData("+55 11 99999-9999", "+5511999999999")] // E.164 com espaços/hífen
    [InlineData("11999999999", "+5511999999999")] // sem DDI, adiciona +55
    [InlineData("5511999999999", "+5511999999999")] // com DDI, sem '+'
    [InlineData("011999999999", "+5511999999999")] // zero à frente do DDD
    [InlineData("(11) 98765-4321", "+5511987654321")] // máscara BR
    public void NormalizarParaE164_FormatoVariado_RetornaE164(string contato, string esperado)
    {
        TelefoneUtils.NormalizarParaE164(contato).Should().Be(esperado);
    }

    [Fact]
    public void NormalizarParaE164_NumeroInvalido_LancaContatoInvalidoException()
    {
        var acao = () => TelefoneUtils.NormalizarParaE164("123");

        acao.Should().Throw<ContatoInvalidoException>()
            .WithMessage("*formato E.164*");
    }
}
