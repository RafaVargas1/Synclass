using FluentAssertions;
using Synclass.Domain.CodigosEntradaTurma;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.CodigosEntradaTurma;

/// <summary>
/// Cobre <see cref="CodigoEntradaTurmaService"/> — geração (rejeitando
/// Professor inexistente) e aceite (idempotente quando já existe vínculo,
/// cria matrícula quando não existe, rejeita código expirado/inexistente).
/// Usa Fakes no lugar de EF/banco real.
/// </summary>
public sealed class CodigoEntradaTurmaServiceTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task GerarAsync_ProfessorExistente_CriaCodigoValidoPorCincoMinutos()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();

        var codigoEntrada = await servico.GerarAsync(professorId, CancellationToken.None);

        codigoEntrada.ProfessorId.Should().Be(professorId);
        codigoEntrada.Codigo.Should().Be("00001");
        codigoEntrada.ExpiraEm.Should().Be(Clock.UtcNow.AddMinutes(5));
        contexto.Codigos.Codigos.Should().ContainSingle();
    }

    [Fact]
    public async Task GerarAsync_ProfessorIdInexistente_RejeitaComProfessorNaoEncontradoException()
    {
        var contexto = NovoContexto();
        var servico = NovoServico(contexto);

        var acao = () => servico.GerarAsync(Guid.NewGuid(), CancellationToken.None);

        await acao.Should().ThrowAsync<ProfessorNaoEncontradoException>();
        contexto.Codigos.Codigos.Should().BeEmpty();
    }

    [Fact]
    public async Task AceitarAsync_CodigoAtivoSemVinculoPrevio_CriaMatriculaVinculada()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var codigoEntrada = await servico.GerarAsync(professorId, CancellationToken.None);
        var alunoUsuarioId = Guid.NewGuid();

        var resultado = await servico.AceitarAsync(codigoEntrada.Codigo, alunoUsuarioId, CancellationToken.None);

        resultado.ProfessorId.Should().Be(professorId);
        resultado.VinculoCriado.Should().BeTrue();
        contexto.Matriculas.Matriculas.Should().ContainSingle(m => m.AlunoUsuarioId == alunoUsuarioId && m.ProfessorId == professorId);
    }

    [Fact]
    public async Task AceitarAsync_VinculoJaExistente_EIdempotenteENaoDuplicaMatricula()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var codigoEntrada = await servico.GerarAsync(professorId, CancellationToken.None);
        var alunoUsuarioId = Guid.NewGuid();
        var vinculoExistente = Matricula.CriarVinculada(professorId, alunoUsuarioId, Clock);
        await contexto.Matriculas.AdicionarAsync(vinculoExistente, CancellationToken.None);

        var resultado = await servico.AceitarAsync(codigoEntrada.Codigo, alunoUsuarioId, CancellationToken.None);

        resultado.VinculoCriado.Should().BeFalse();
        contexto.Matriculas.Matriculas.Should().ContainSingle();
    }

    [Fact]
    public async Task AceitarAsync_CodigoInexistente_RejeitaComCodigoEntradaInvalidoException()
    {
        var contexto = NovoContexto();
        var servico = NovoServico(contexto);

        var acao = () => servico.AceitarAsync("99999", Guid.NewGuid(), CancellationToken.None);

        await acao.Should().ThrowAsync<CodigoEntradaInvalidoException>();
    }

    [Fact]
    public async Task AceitarAsync_CodigoExpirado_RejeitaComCodigoEntradaInvalidoException()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var codigoEntrada = await servico.GerarAsync(professorId, CancellationToken.None);
        var relogioAposExpirar = new FixedClock(Clock.UtcNow.AddMinutes(6));
        var servicoAposExpirar = NovoServico(contexto, relogioAposExpirar);

        var acao = () => servicoAposExpirar.AceitarAsync(codigoEntrada.Codigo, Guid.NewGuid(), CancellationToken.None);

        await acao.Should().ThrowAsync<CodigoEntradaInvalidoException>();
    }

    [Theory]
    [InlineData("00 001")]
    [InlineData("0-0-0-0-1")]
    public async Task AceitarAsync_CodigoComEspacosOuMascara_NormalizaEquivalenteASoDigitos(string codigoComMascara)
    {
        var (servico, _, professorId) = await CriarServicoComProfessorExistenteAsync();
        await servico.GerarAsync(professorId, CancellationToken.None);

        var resultado = await servico.AceitarAsync(codigoComMascara, Guid.NewGuid(), CancellationToken.None);

        resultado.ProfessorId.Should().Be(professorId);
    }

    private static async Task<(CodigoEntradaTurmaService Servico, Contexto Contexto, Guid ProfessorId)> CriarServicoComProfessorExistenteAsync()
    {
        var contexto = NovoContexto();
        var servico = NovoServico(contexto);
        var professorId = await AdicionarProfessorAsync(contexto.Usuarios, "professor@exemplo.com");
        return (servico, contexto, professorId);
    }

    private static Contexto NovoContexto()
    {
        return new Contexto(new FakeCodigoEntradaTurmaRepository(), new FakeMatriculaRepository(), new FakeUsuarioRepository());
    }

    private static CodigoEntradaTurmaService NovoServico(Contexto contexto, FixedClock? clock = null)
    {
        return new CodigoEntradaTurmaService(
            contexto.Codigos, contexto.Matriculas, contexto.Usuarios, new FakeGeradorDeCodigoConvite(), clock ?? Clock);
    }

    private static async Task<Guid> AdicionarProfessorAsync(FakeUsuarioRepository usuarios, string contato)
    {
        var professor = Usuario.Cadastrar("Professor Teste", contato, PapelUsuario.Professor, null, Clock);
        await usuarios.AdicionarAsync(professor, CancellationToken.None);
        return professor.Id;
    }

    private sealed record Contexto(FakeCodigoEntradaTurmaRepository Codigos, FakeMatriculaRepository Matriculas, FakeUsuarioRepository Usuarios);
}
