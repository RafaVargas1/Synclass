using FluentAssertions;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Autenticacao;

/// <summary>
/// Cobre a conversão de formatos variados de telefone BR para E.164
/// (<see cref="TelefoneUtils.NormalizarParaE164"/>, issue #193) — edge point
/// explícito do card: o provedor de WhatsApp exige o número com DDI, o que a
/// normalização de contato do login (que devolve só os dígitos sem DDI) não
/// garante.
/// </summary>
public sealed class TelefoneUtilsTests
{
    [Theory]
    [InlineData("+5511999999999", "+5511999999999")] // já E.164
    [InlineData("+55 11 99999-9999", "+5511999999999")] // espaços/hífen
    [InlineData("11999999999", "+5511999999999")] // sem DDI, adiciona +55
    [InlineData("5511999999999", "+5511999999999")] // já com DDI, sem +
    public void NormalizarParaE164_FormatoVariado_ConverteParaE164(string contato, string esperado)
    {
        TelefoneUtils.NormalizarParaE164(contato).Should().Be(esperado);
    }

    [Theory]
    [InlineData("123")] // sem DDD nem DDI
    [InlineData("abc")]
    [InlineData("")]
    public void NormalizarParaE164_FormatoInvalido_LancaContatoInvalido(string contato)
    {
        var acao = () => TelefoneUtils.NormalizarParaE164(contato);

        acao.Should().Throw<ContatoInvalidoException>()
            .WithMessage($"*{contato}*");
    }
}
