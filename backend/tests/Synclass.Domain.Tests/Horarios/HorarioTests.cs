using FluentAssertions;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Horarios;

namespace Synclass.Domain.Tests.Horarios;

/// <summary>
/// Cobre a Regra de Negócio central da issue #6 na entidade: criação válida,
/// rejeição de duração inválida e o cálculo puro de sobreposição de horário
/// (sem depender de repositório — ver <see cref="HorarioServiceTests"/> para
/// o fluxo orquestrado com persistência).
/// </summary>
public sealed class HorarioTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));
    private static readonly Guid ProfessorId = Guid.NewGuid();

    [Fact]
    public void Criar_DadosValidos_CriaHorarioComCamposInformados()
    {
        var horario = Horario.Criar(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, Clock);

        horario.ProfessorId.Should().Be(ProfessorId);
        horario.DiaSemana.Should().Be(DiaSemana.Terca);
        horario.HoraInicio.Should().Be(new TimeOnly(10, 0));
        horario.DuracaoMinutos.Should().Be(60);
        horario.HoraFim.Should().Be(new TimeOnly(11, 0));
        horario.CreatedAt.Should().Be(Clock.UtcNow);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-15)]
    public void Criar_DuracaoZeroOuNegativa_RejeitaComDuracaoInvalidaException(int duracaoMinutos)
    {
        var acao = () => Horario.Criar(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), duracaoMinutos, Clock);

        acao.Should().Throw<DuracaoInvalidaException>();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(99)]
    public void Criar_DiaSemanaForaDoIntervalo_RejeitaComDiaSemanaInvalidoException(int diaSemana)
    {
        var acao = () => Horario.Criar(ProfessorId, (DiaSemana)diaSemana, new TimeOnly(10, 0), 60, Clock);

        acao.Should().Throw<DiaSemanaInvalidoException>();
    }

    [Fact]
    public void Sobrepoe_MesmoDiaComIntervalosQueSeCruzam_RetornaTrue()
    {
        var existente = Horario.Criar(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, Clock);
        var novo = Horario.Criar(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 30), 60, Clock);

        novo.Sobrepoe(existente).Should().BeTrue();
    }

    [Fact]
    public void Sobrepoe_MesmoDiaComBordasQueSoSeTocam_RetornaFalse()
    {
        var existente = Horario.Criar(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, Clock);
        var novo = Horario.Criar(ProfessorId, DiaSemana.Terca, new TimeOnly(11, 0), 30, Clock);

        novo.Sobrepoe(existente).Should().BeFalse();
    }

    [Fact]
    public void Sobrepoe_DiasDiferentes_RetornaFalse()
    {
        var existente = Horario.Criar(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, Clock);
        var novo = Horario.Criar(ProfessorId, DiaSemana.Quarta, new TimeOnly(10, 0), 60, Clock);

        novo.Sobrepoe(existente).Should().BeFalse();
    }
}
