using FluentAssertions;
using Synclass.Domain.Convites;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Convites;

/// <summary>
/// Cobre a criação e o uso único de um convite (issue #2): geração com
/// validade configurável (critério de aceite 1) e rejeição de reuso ou de
/// convite expirado (critério de aceite 3).
/// </summary>
public sealed class ConviteTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Gerar_ContatoETokenValidos_CriaConviteNaoUsadoComExpiraEmIgualAgoraMaisDias()
    {
        var professorId = Guid.NewGuid();

        var convite = Convite.Gerar(professorId, "11987654321", TipoContato.Telefone, matriculaId: null, "token-alta-entropia", diasValidade: 7, Clock);

        convite.ProfessorId.Should().Be(professorId);
        convite.Contato.Should().Be("11987654321");
        convite.ContatoTipo.Should().Be(TipoContato.Telefone);
        convite.MatriculaId.Should().BeNull();
        convite.Token.Should().Be("token-alta-entropia");
        convite.ExpiraEm.Should().Be(Clock.UtcNow.AddDays(7));
        convite.CreatedAt.Should().Be(Clock.UtcNow);
        convite.UsadoEm.Should().BeNull();
    }

    [Fact]
    public void Gerar_ComMatriculaIdDeOrigem_PreservaMatriculaId()
    {
        var matriculaId = Guid.NewGuid();

        var convite = Convite.Gerar(Guid.NewGuid(), "maria@exemplo.com", TipoContato.Email, matriculaId, "token", diasValidade: 7, Clock);

        convite.MatriculaId.Should().Be(matriculaId);
    }

    [Fact]
    public void MarcarUsado_ConviteNaoUsadoNemExpirado_DefineUsadoEm()
    {
        var convite = Convite.Gerar(Guid.NewGuid(), "11987654321", TipoContato.Telefone, null, "token", diasValidade: 7, Clock);

        convite.MarcarUsado(Clock);

        convite.UsadoEm.Should().Be(Clock.UtcNow);
    }

    [Fact]
    public void MarcarUsado_ConviteJaUsado_RejeitaComConviteInvalidoException()
    {
        var convite = Convite.Gerar(Guid.NewGuid(), "11987654321", TipoContato.Telefone, null, "token", diasValidade: 7, Clock);
        convite.MarcarUsado(Clock);

        var acao = () => convite.MarcarUsado(Clock);

        acao.Should().Throw<ConviteInvalidoException>();
    }

    [Fact]
    public void MarcarUsado_ConviteExpirado_RejeitaComConviteExpiradoException()
    {
        var convite = Convite.Gerar(Guid.NewGuid(), "11987654321", TipoContato.Telefone, null, "token", diasValidade: 7, Clock);
        var clockDepoisDeExpirar = new FixedClock(Clock.UtcNow.AddDays(8));

        var acao = () => convite.MarcarUsado(clockDepoisDeExpirar);

        acao.Should().Throw<ConviteExpiradoException>();
    }
}
