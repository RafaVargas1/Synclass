using FluentAssertions;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Matriculas;

/// <summary>
/// Cobre a promoção de matrícula provisória para plena (critério de aceite 4
/// da issue #3): o vínculo (e o histórico associado via FK) é preservado, a
/// promoção só define <see cref="Matricula.AlunoUsuarioId"/> na linha
/// existente, nunca cria uma segunda. Também cobre o
/// <see cref="Matricula.IdentificadorAluno"/> gravado em
/// <see cref="Matricula.CriarProvisoria"/> (issue #70).
/// </summary>
public sealed class MatriculaTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Promover_MatriculaProvisoria_DefineAlunoUsuarioIdPreservandoMatriculaId()
    {
        var professorId = Guid.NewGuid();
        var matricula = Matricula.CriarProvisoria(professorId, "João Pedro", "2024-013", null, Clock);
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
        var matricula = Matricula.CriarProvisoria(Guid.NewGuid(), "João Pedro", "2024-013", null, Clock);
        matricula.Promover(Guid.NewGuid());

        var acao = () => matricula.Promover(Guid.NewGuid());

        acao.Should().Throw<MatriculaJaPromovidaException>();
    }

    /// <summary>
    /// Prova formal (issue #5) de que a relação Aluno-Professor é N:N: duas
    /// <see cref="Matricula"/> plenas do mesmo <see cref="Matricula.AlunoUsuarioId"/>,
    /// uma para cada Professor, coexistem como linhas independentes — nada no
    /// desenho da entidade impede múltiplos vínculos simultâneos do mesmo Aluno.
    /// </summary>
    [Fact]
    public void CriarVinculada_MesmoAlunoDoisProfessoresDiferentes_CoexistemComoLinhasIndependentes()
    {
        var alunoUsuarioId = Guid.NewGuid();
        var professorAId = Guid.NewGuid();
        var professorBId = Guid.NewGuid();

        var matriculaComA = Matricula.CriarVinculada(professorAId, alunoUsuarioId, Clock);
        var matriculaComB = Matricula.CriarVinculada(professorBId, alunoUsuarioId, Clock);

        matriculaComA.Id.Should().NotBe(matriculaComB.Id);
        matriculaComA.AlunoUsuarioId.Should().Be(alunoUsuarioId);
        matriculaComB.AlunoUsuarioId.Should().Be(alunoUsuarioId);
        matriculaComA.ProfessorId.Should().Be(professorAId);
        matriculaComB.ProfessorId.Should().Be(professorBId);
    }

    /// <summary>
    /// Issue #70: <see cref="Matricula.CriarProvisoria"/> grava o
    /// <see cref="Matricula.IdentificadorAluno"/> recebido (coluna nova,
    /// independente de <see cref="Matricula.IdentificadorProvisorio"/>, que é
    /// o identificador escolhido pelo Professor).
    /// </summary>
    [Fact]
    public void CriarProvisoria_ComIdentificadorAluno_GravaIdentificadorAlunoIndependenteDoProvisorio()
    {
        var matricula = Matricula.CriarProvisoria(Guid.NewGuid(), "João Pedro", "2024-013", "ALU-2B7K", Clock);

        matricula.IdentificadorAluno.Should().Be("ALU-2B7K");
        matricula.IdentificadorProvisorio.Should().Be("2024-013");
    }
}
