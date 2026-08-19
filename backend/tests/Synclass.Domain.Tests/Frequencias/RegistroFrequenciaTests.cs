using FluentAssertions;
using Synclass.Domain.Frequencias;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Frequencias;

/// <summary>
/// Cobre <see cref="RegistroFrequencia.ConfirmarAluno"/> (issue #15) — mesma
/// simetria de <see cref="RegistroFrequencia.RegistrarProfessor"/> (issue
/// #14): seta só o campo correspondente, sem tocar no outro.
/// </summary>
public sealed class RegistroFrequenciaTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void ConfirmarAluno_SetaConfirmadoPeloAlunoEUpdatedAt_SemMexerNoStatusProfessor()
    {
        var registro = RegistroFrequencia.Criar(Guid.NewGuid(), Guid.NewGuid(), Clock);
        var clockDaConfirmacao = new FixedClock(Clock.UtcNow.AddMinutes(5));

        registro.ConfirmarAluno(clockDaConfirmacao);

        registro.ConfirmadoPeloAluno.Should().BeTrue();
        registro.StatusProfessor.Should().BeNull();
        registro.UpdatedAt.Should().Be(clockDaConfirmacao.UtcNow);
    }
}
