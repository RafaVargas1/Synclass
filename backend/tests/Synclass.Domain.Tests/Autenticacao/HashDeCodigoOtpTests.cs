using FluentAssertions;
using Synclass.Domain.Autenticacao;

namespace Synclass.Domain.Tests.Autenticacao;

/// <summary>
/// Cobre o hash usado para nunca persistir o código OTP em texto puro (ver
/// Critérios técnicos da issue #18).
/// </summary>
public sealed class HashDeCodigoOtpTests
{
    [Fact]
    public void Gerar_MesmoCodigo_ProduzMesmoHash()
    {
        var hash1 = HashDeCodigoOtp.Gerar("123456");
        var hash2 = HashDeCodigoOtp.Gerar("123456");

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Gerar_CodigosDiferentes_ProduzemHashesDiferentes()
    {
        var hash1 = HashDeCodigoOtp.Gerar("123456");
        var hash2 = HashDeCodigoOtp.Gerar("654321");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Gerar_NuncaContemOCodigoEmTextoPuro()
    {
        var hash = HashDeCodigoOtp.Gerar("123456");

        hash.Should().NotContain("123456");
    }
}
