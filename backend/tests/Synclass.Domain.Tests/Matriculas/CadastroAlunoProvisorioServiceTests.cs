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
    public async Task CadastrarAsync_NomeValido_SemIdentificador_CriaMatriculaProvisoriaSemContato()
    {
        var (servico, repositorio, professorId) = await CriarServicoComProfessorExistenteAsync();

        var matricula = await servico.CadastrarAsync(professorId, "João Pedro", CancellationToken.None);

        matricula.ProfessorId.Should().Be(professorId);
        matricula.NomeProvisorio.Should().Be("João Pedro");
        matricula.AlunoUsuarioId.Should().BeNull();
        repositorio.Matriculas.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarAsync_SemIdentificadorInformado_IdentificadorProvisorioIgualaAoIdentificadorAlunoGerado()
    {
        var (servico, identificadorAluno, repositorio, professorId) = await CriarServicoComProfessorEIdentificadorAsync();

        var matricula = await servico.CadastrarAsync(professorId, "João Pedro", CancellationToken.None);

        identificadorAluno.VezesGerado.Should().Be(1);
        matricula.IdentificadorAluno.Should().NotBeNull();
        matricula.IdentificadorProvisorio.Should().Be(matricula.IdentificadorAluno);
        repositorio.Matriculas.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarAsync_NomeVazio_RejeitaSemCriarMatricula()
    {
        var (servico, repositorio, professorId) = await CriarServicoComProfessorExistenteAsync();

        var acao = () => servico.CadastrarAsync(professorId, "   ", CancellationToken.None);

        await acao.Should().ThrowAsync<NomeProvisorioInvalidoException>();
        repositorio.Matriculas.Should().BeEmpty();
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

        var acao = () => servico.CadastrarAsync(professorIdInexistente, "João Pedro", CancellationToken.None);

        await acao.Should().ThrowAsync<ProfessorNaoEncontradoException>()
            .WithMessage($"*{professorIdInexistente}*");
        repositorio.Matriculas.Should().BeEmpty();
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
    /// #159 prove que o identificador gerado preenche tanto
    /// <see cref="Matricula.IdentificadorProvisorio"/> quanto
    /// <see cref="Matricula.IdentificadorAluno"/> da Matricula provisória.
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
