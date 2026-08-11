using FluentAssertions;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Matriculas;

/// <summary>
/// Cobre a Regra de Negócio central da issue #3: cadastro de Aluno
/// provisório sem exigir contato/login, com identificador único por
/// Professor (não global). Usa <see cref="FakeMatriculaRepository"/> no
/// lugar de EF/banco real.
/// </summary>
public sealed class CadastroAlunoProvisorioServiceTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));
    private static readonly Guid ProfessorId = Guid.NewGuid();

    [Fact]
    public async Task CadastrarAsync_NomeEIdentificadorValidos_CriaMatriculaProvisoriaSemContato()
    {
        var repositorio = new FakeMatriculaRepository();
        var servico = new CadastroAlunoProvisorioService(repositorio, Clock);

        var matricula = await servico.CadastrarAsync(ProfessorId, "João Pedro", "2024-013", CancellationToken.None);

        matricula.ProfessorId.Should().Be(ProfessorId);
        matricula.NomeProvisorio.Should().Be("João Pedro");
        matricula.IdentificadorProvisorio.Should().Be("2024-013");
        matricula.AlunoUsuarioId.Should().BeNull();
        repositorio.Matriculas.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarAsync_NomeVazio_RejeitaSemCriarMatricula()
    {
        var repositorio = new FakeMatriculaRepository();
        var servico = new CadastroAlunoProvisorioService(repositorio, Clock);

        var acao = () => servico.CadastrarAsync(ProfessorId, "   ", "2024-013", CancellationToken.None);

        await acao.Should().ThrowAsync<NomeProvisorioInvalidoException>();
        repositorio.Matriculas.Should().BeEmpty();
    }

    [Fact]
    public async Task CadastrarAsync_IdentificadorVazio_RejeitaSemCriarMatricula()
    {
        var repositorio = new FakeMatriculaRepository();
        var servico = new CadastroAlunoProvisorioService(repositorio, Clock);

        var acao = () => servico.CadastrarAsync(ProfessorId, "João Pedro", "   ", CancellationToken.None);

        await acao.Should().ThrowAsync<IdentificadorProvisorioInvalidoException>();
        repositorio.Matriculas.Should().BeEmpty();
    }

    [Fact]
    public async Task CadastrarAsync_IdentificadorJaUsadoPeloMesmoProfessor_RejeitaComMensagemClara()
    {
        var repositorio = new FakeMatriculaRepository();
        var servico = new CadastroAlunoProvisorioService(repositorio, Clock);
        await servico.CadastrarAsync(ProfessorId, "João Pedro", "2024-013", CancellationToken.None);

        var acao = () => servico.CadastrarAsync(ProfessorId, "Outro Aluno", "2024-013", CancellationToken.None);

        await acao.Should().ThrowAsync<IdentificadorProvisorioDuplicadoException>()
            .WithMessage("*2024-013*");
        repositorio.Matriculas.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarAsync_IdentificadorRepetidoEntreProfessoresDiferentes_NaoRejeita()
    {
        var repositorio = new FakeMatriculaRepository();
        var servico = new CadastroAlunoProvisorioService(repositorio, Clock);
        await servico.CadastrarAsync(ProfessorId, "João Pedro", "2024-013", CancellationToken.None);

        var outroProfessorId = Guid.NewGuid();
        var matricula = await servico.CadastrarAsync(outroProfessorId, "Outro Aluno", "2024-013", CancellationToken.None);

        matricula.ProfessorId.Should().Be(outroProfessorId);
        repositorio.Matriculas.Should().HaveCount(2);
    }
}
