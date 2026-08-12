using FluentAssertions;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Matriculas;

/// <summary>
/// Cobre a promoção de matrícula provisória para plena (critério de aceite 4
/// da issue #3): o vínculo (e o histórico associado via FK) é preservado, a
/// promoção só define <see cref="Matricula.AlunoUsuarioId"/> na linha
/// existente, nunca cria uma segunda.
/// </summary>
public sealed class MatriculaTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Promover_MatriculaProvisoria_DefineAlunoUsuarioIdPreservandoMatriculaId()
    {
        var professorId = Guid.NewGuid();
        var matricula = Matricula.CriarProvisoria(professorId, "João Pedro", "2024-013", Clock);
        var matriculaIdOriginal = matricula.Id;
        var alunoUsuarioId = Guid.NewGuid();

        matricula.Promover(alunoUsuarioId);

        matricula.Id.Should().Be(matriculaIdOriginal);
        matricula.AlunoUsuarioId.Should().Be(alunoUsuarioId);
        matricula.NomeProvisorio.Should().Be("João Pedro");
        matricula.IdentificadorProvisorio.Should().Be("2024-013");
    }

    [Fact]
    public void Promover_MatriculaJaPromovida_RejeitaComMatriculaJaPromovidaException()
    {
        var matricula = Matricula.CriarProvisoria(Guid.NewGuid(), "João Pedro", "2024-013", Clock);
        matricula.Promover(Guid.NewGuid());

        var acao = () => matricula.Promover(Guid.NewGuid());

        acao.Should().Throw<MatriculaJaPromovidaException>();
    }
}
