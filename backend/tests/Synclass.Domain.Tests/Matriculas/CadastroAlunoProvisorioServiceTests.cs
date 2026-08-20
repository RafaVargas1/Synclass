using FluentAssertions;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Tests.Usuarios;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Matriculas;

/// <summary>
/// Cobre a Regra de Negócio central da issue #3: cadastro de Aluno
/// provisório sem exigir contato/login, com identificador único por
/// Professor (não global). Usa <see cref="FakeMatriculaRepository"/> e
/// <see cref="FakeUsuarioRepository"/> no lugar de EF/banco real.
/// </summary>
public sealed class CadastroAlunoProvisorioServiceTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task CadastrarAsync_NomeEIdentificadorValidos_CriaMatriculaProvisoriaSemContato()
    {
        var (servico, repositorio, professorId) = await CriarServicoComProfessorExistenteAsync();

        var matricula = await servico.CadastrarAsync(professorId, "João Pedro", "2024-013", CancellationToken.None);

        matricula.ProfessorId.Should().Be(professorId);
        matricula.NomeProvisorio.Should().Be("João Pedro");
        matricula.IdentificadorProvisorio.Should().Be("2024-013");
        matricula.AlunoUsuarioId.Should().BeNull();
        repositorio.Matriculas.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarAsync_NomeVazio_RejeitaSemCriarMatricula()
    {
        var (servico, repositorio, professorId) = await CriarServicoComProfessorExistenteAsync();

        var acao = () => servico.CadastrarAsync(professorId, "   ", "2024-013", CancellationToken.None);

        await acao.Should().ThrowAsync<NomeProvisorioInvalidoException>();
        repositorio.Matriculas.Should().BeEmpty();
    }

    [Fact]
    public async Task CadastrarAsync_IdentificadorVazio_RejeitaSemCriarMatricula()
    {
        var (servico, repositorio, professorId) = await CriarServicoComProfessorExistenteAsync();

        var acao = () => servico.CadastrarAsync(professorId, "João Pedro", "   ", CancellationToken.None);

        await acao.Should().ThrowAsync<IdentificadorProvisorioInvalidoException>();
        repositorio.Matriculas.Should().BeEmpty();
    }

    [Fact]
    public async Task CadastrarAsync_IdentificadorJaUsadoPeloMesmoProfessor_RejeitaComMensagemClara()
    {
        var (servico, repositorio, professorId) = await CriarServicoComProfessorExistenteAsync();
        await servico.CadastrarAsync(professorId, "João Pedro", "2024-013", CancellationToken.None);

        var acao = () => servico.CadastrarAsync(professorId, "Outro Aluno", "2024-013", CancellationToken.None);

        await acao.Should().ThrowAsync<IdentificadorProvisorioDuplicadoException>()
            .WithMessage("*2024-013*");
        repositorio.Matriculas.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarAsync_IdentificadorRepetidoEntreProfessoresDiferentes_NaoRejeita()
    {
        var usuarios = new FakeUsuarioRepository();
        var repositorio = new FakeMatriculaRepository();
        var servico = new CadastroAlunoProvisorioService(repositorio, usuarios, Clock, new IdentificadorAlunoFake().Servico);
        var professorId = await AdicionarProfessorAsync(usuarios, "professor1@exemplo.com");
        await servico.CadastrarAsync(professorId, "João Pedro", "2024-013", CancellationToken.None);

        var outroProfessorId = await AdicionarProfessorAsync(usuarios, "professor2@exemplo.com");
        var matricula = await servico.CadastrarAsync(outroProfessorId, "Outro Aluno", "2024-013", CancellationToken.None);

        matricula.ProfessorId.Should().Be(outroProfessorId);
        repositorio.Matriculas.Should().HaveCount(2);
    }

    /// <summary>
    /// Regressão do achado de dev-review no PR #22: um professorId
    /// inexistente deve ser rejeitado com um erro específico, não com o
    /// <see cref="MatriculaConcorrenteException"/> genérico de "tente
    /// novamente" que só apareceria antes por violação de foreign key no
    /// Postgres — não reproduzível com o EF Core InMemory usado aqui.
    /// </summary>
    [Fact]
    public async Task CadastrarAsync_ProfessorIdInexistente_RejeitaComErroEspecificoSemCriarMatricula()
    {
        var usuarios = new FakeUsuarioRepository();
        var repositorio = new FakeMatriculaRepository();
        var servico = new CadastroAlunoProvisorioService(repositorio, usuarios, Clock, new IdentificadorAlunoFake().Servico);
        var professorIdInexistente = Guid.NewGuid();

        var acao = () => servico.CadastrarAsync(professorIdInexistente, "João Pedro", "2024-013", CancellationToken.None);

        await acao.Should().ThrowAsync<ProfessorNaoEncontradoException>()
            .WithMessage($"*{professorIdInexistente}*");
        repositorio.Matriculas.Should().BeEmpty();
    }

    // Cenário específico da issue #70: o cadastro de Aluno provisório gera
    // um IdentificadorAluno único via IdentificadorAlunoService e o grava na
    // Matricula provisória, independente do IdentificadorProvisorio escolhido
    // pelo Professor.

    [Fact]
    public async Task CadastrarAsync_GeraIdentificadorAlunoNaMatriculaProvisoria()
    {
        var (servico, identificadorAluno, repositorio, professorId) = await CriarServicoComProfessorEIdentificadorAsync();

        var matricula = await servico.CadastrarAsync(professorId, "João Pedro", "2024-013", CancellationToken.None);

        identificadorAluno.VezesGerado.Should().Be(1);
        matricula.IdentificadorAluno.Should().NotBeNull();
        repositorio.Matriculas.Should().ContainSingle();
    }

    private static async Task<(CadastroAlunoProvisorioService Servico, FakeMatriculaRepository Matriculas, Guid ProfessorId)>
        CriarServicoComProfessorExistenteAsync()
    {
        var usuarios = new FakeUsuarioRepository();
        var repositorio = new FakeMatriculaRepository();
        var servico = new CadastroAlunoProvisorioService(repositorio, usuarios, Clock, new IdentificadorAlunoFake().Servico);
        var professorId = await AdicionarProfessorAsync(usuarios, "professor@exemplo.com");
        return (servico, repositorio, professorId);
    }

    /// <summary>
    /// Monta um <see cref="CadastroAlunoProvisorioService"/> com Professor já
    /// existente e um <see cref="IdentificadorAlunoService"/> real (fakes de
    /// gerador e checador), devolvendo junto o fake para que o teste da issue
    /// #70 prove que o identificador é gerado na Matricula provisória.
    /// </summary>
    private static async Task<(CadastroAlunoProvisorioService Servico, IdentificadorAlunoFake IdentificadorAluno, FakeMatriculaRepository Matriculas, Guid ProfessorId)>
        CriarServicoComProfessorEIdentificadorAsync()
    {
        var usuarios = new FakeUsuarioRepository();
        var repositorio = new FakeMatriculaRepository();
        var identificadorAluno = new IdentificadorAlunoFake();
        var servico = new CadastroAlunoProvisorioService(repositorio, usuarios, Clock, identificadorAluno.Servico);
        var professorId = await AdicionarProfessorAsync(usuarios, "professor@exemplo.com");
        return (servico, identificadorAluno, repositorio, professorId);
    }

    private static async Task<Guid> AdicionarProfessorAsync(FakeUsuarioRepository usuarios, string contato)
    {
        var professor = Usuario.Cadastrar("Professor Teste", contato, PapelUsuario.Professor, null, Clock);
        await usuarios.AdicionarAsync(professor, CancellationToken.None);
        return professor.Id;
    }
}
