using FluentAssertions;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Autenticacao;

/// <summary>
/// Cobre os invariantes de <see cref="CodigoOtp"/>: expiração de 10min,
/// uso único, e nunca guardar o código em texto puro (ver Critérios
/// técnicos da issue #18).
/// </summary>
public sealed class CodigoOtpTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));
    private static readonly Guid UsuarioId = Guid.NewGuid();

    [Fact]
    public void Gerar_CalculaExpiracaoEmDezMinutosENasceNaoUsado()
    {
        var codigo = CodigoOtp.Gerar(UsuarioId, "123456", Clock);

        codigo.ExpiraEm.Should().Be(Clock.UtcNow.AddMinutes(10));
        codigo.UsadoEm.Should().BeNull();
        codigo.CodigoHash.Should().Be(HashDeCodigoOtp.Gerar("123456"));
    }

    [Fact]
    public void Corresponde_MesmoCodigo_RetornaTrue()
    {
        var codigo = CodigoOtp.Gerar(UsuarioId, "123456", Clock);

        codigo.Corresponde("123456").Should().BeTrue();
    }

    [Fact]
    public void Corresponde_CodigoDiferente_RetornaFalse()
    {
        var codigo = CodigoOtp.Gerar(UsuarioId, "123456", Clock);

        codigo.Corresponde("000000").Should().BeFalse();
    }

    [Fact]
    public void Expirado_AntesDoPrazo_RetornaFalse()
    {
        var codigo = CodigoOtp.Gerar(UsuarioId, "123456", Clock);
        var relogioNoveMinutosDepois = new FixedClock(Clock.UtcNow.AddMinutes(9));

        codigo.Expirado(relogioNoveMinutosDepois).Should().BeFalse();
    }

    [Fact]
    public void Expirado_AposDezMinutos_RetornaTrue()
    {
        var codigo = CodigoOtp.Gerar(UsuarioId, "123456", Clock);
        var relogioDepoisDoPrazo = new FixedClock(Clock.UtcNow.AddMinutes(10));

        codigo.Expirado(relogioDepoisDoPrazo).Should().BeTrue();
    }

    [Fact]
    public void Invalidar_MarcaUsadoEmComOInstanteInformado()
    {
        var codigo = CodigoOtp.Gerar(UsuarioId, "123456", Clock);
        var relogioDepois = new FixedClock(Clock.UtcNow.AddMinutes(1));

        codigo.Invalidar(relogioDepois);

        codigo.UsadoEm.Should().Be(relogioDepois.UtcNow);
    }
}
