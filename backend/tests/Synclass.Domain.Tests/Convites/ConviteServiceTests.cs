using FluentAssertions;
using Synclass.Domain.Convites;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Convites;

/// <summary>
/// Cobre <see cref="ConviteService.GerarAsync"/> — geração de convite
/// direcionado (critério de aceite 1), rejeitando Professor inexistente,
/// matrícula de origem inválida (edge point) e Aluno já vinculado
/// (critério de aceite 4). Usa Fakes no lugar de EF/banco real.
/// </summary>
public sealed class ConviteServiceTests
{
    private const int DiasValidade = 7;
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task GerarAsync_ContatoValidoSemAlunoPrevio_CriaConviteVinculadoAoProfessor()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();

        var convite = await servico.GerarAsync(professorId, "11987654321", matriculaId: null, CancellationToken.None);

        convite.ProfessorId.Should().Be(professorId);
        convite.Contato.Should().Be("11987654321");
        convite.ContatoTipo.Should().Be(TipoContato.Telefone);
        convite.Token.Should().Be("token-1");
        convite.ExpiraEm.Should().Be(Clock.UtcNow.AddDays(DiasValidade));
        contexto.Convites.Convites.Should().ContainSingle();
    }

    [Fact]
    public async Task GerarAsync_ProfessorIdInexistente_RejeitaComProfessorNaoEncontradoException()
    {
        var contexto = NovoContexto();
        var servico = NovoServico(contexto);
        var professorIdInexistente = Guid.NewGuid();

        var acao = () => servico.GerarAsync(professorIdInexistente, "11987654321", null, CancellationToken.None);

        await acao.Should().ThrowAsync<ProfessorNaoEncontradoException>();
        contexto.Convites.Convites.Should().BeEmpty();
    }

    [Fact]
    public async Task GerarAsync_MatriculaOrigemInexistente_RejeitaComMatriculaOrigemInvalidaException()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var matriculaIdInexistente = Guid.NewGuid();

        var acao = () => servico.GerarAsync(professorId, "11987654321", matriculaIdInexistente, CancellationToken.None);

        await acao.Should().ThrowAsync<MatriculaOrigemInvalidaException>();
        contexto.Convites.Convites.Should().BeEmpty();
    }

    [Fact]
    public async Task GerarAsync_MatriculaOrigemDeOutroProfessor_RejeitaComMatriculaOrigemInvalidaException()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var outroProfessorId = await AdicionarProfessorAsync(contexto.Usuarios, "outro-professor@exemplo.com");
        var matriculaDeOutroProfessor = Matricula.CriarProvisoria(outroProfessorId, "João Pedro", "2024-013", Clock);
        await contexto.Matriculas.AdicionarAsync(matriculaDeOutroProfessor, CancellationToken.None);

        var acao = () => servico.GerarAsync(professorId, "11987654321", matriculaDeOutroProfessor.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<MatriculaOrigemInvalidaException>();
    }

    [Fact]
    public async Task GerarAsync_MatriculaOrigemJaPromovida_RejeitaComMatriculaOrigemInvalidaException()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var matricula = Matricula.CriarProvisoria(professorId, "João Pedro", "2024-013", Clock);
        matricula.Promover(Guid.NewGuid());
        await contexto.Matriculas.AdicionarAsync(matricula, CancellationToken.None);

        var acao = () => servico.GerarAsync(professorId, "11987654321", matricula.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<MatriculaOrigemInvalidaException>();
    }

    [Fact]
    public async Task GerarAsync_MatriculaOrigemValida_CriaConvitePreservandoMatriculaId()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var matricula = Matricula.CriarProvisoria(professorId, "João Pedro", "2024-013", Clock);
        await contexto.Matriculas.AdicionarAsync(matricula, CancellationToken.None);

        var convite = await servico.GerarAsync(professorId, "11987654321", matricula.Id, CancellationToken.None);

        convite.MatriculaId.Should().Be(matricula.Id);
    }

    [Fact]
    public async Task GerarAsync_ContatoJaVinculadoComoAlunoPlenoDesteProfessor_RejeitaComContatoJaVinculadoException()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var aluno = Usuario.Cadastrar("Aluno Existente", "11987654321", PapelUsuario.Aluno, Clock);
        await contexto.Usuarios.AdicionarAsync(aluno, CancellationToken.None);
        var vinculo = Matricula.CriarVinculada(professorId, aluno.Id, Clock);
        await contexto.Matriculas.AdicionarAsync(vinculo, CancellationToken.None);

        var acao = () => servico.GerarAsync(professorId, "11987654321", null, CancellationToken.None);

        await acao.Should().ThrowAsync<ContatoJaVinculadoException>();
        contexto.Convites.Convites.Should().BeEmpty();
    }

    private static async Task<(ConviteService Servico, Contexto Contexto, Guid ProfessorId)> CriarServicoComProfessorExistenteAsync()
    {
        var contexto = NovoContexto();
        var servico = NovoServico(contexto);
        var professorId = await AdicionarProfessorAsync(contexto.Usuarios, "professor@exemplo.com");
        return (servico, contexto, professorId);
    }

    private static Contexto NovoContexto()
    {
        return new Contexto(new FakeConviteRepository(), new FakeMatriculaRepository(), new FakeUsuarioRepository());
    }

    private static ConviteService NovoServico(Contexto contexto)
    {
        return new ConviteService(
            contexto.Convites, contexto.Matriculas, contexto.Usuarios, new FakeGeradorDeTokenConvite(), Clock, DiasValidade);
    }

    private static async Task<Guid> AdicionarProfessorAsync(FakeUsuarioRepository usuarios, string contato)
    {
        var professor = Usuario.Cadastrar("Professor Teste", contato, PapelUsuario.Professor, Clock);
        await usuarios.AdicionarAsync(professor, CancellationToken.None);
        return professor.Id;
    }

    private sealed record Contexto(FakeConviteRepository Convites, FakeMatriculaRepository Matriculas, FakeUsuarioRepository Usuarios);
}
