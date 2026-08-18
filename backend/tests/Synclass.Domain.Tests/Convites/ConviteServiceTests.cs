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

    [Fact]
    public async Task AceitarAsync_TokenInexistente_RejeitaComConviteInvalidoException()
    {
        var contexto = NovoContexto();
        var servico = NovoServico(contexto);

        var acao = () => servico.AceitarAsync("token-inexistente", "João Pedro", "11987654321", CancellationToken.None);

        await acao.Should().ThrowAsync<ConviteInvalidoException>();
    }

    [Fact]
    public async Task AceitarAsync_ContatoDivergenteDoConvite_RejeitaComConviteContatoDivergenteException()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var convite = await servico.GerarAsync(professorId, "11987654321", null, CancellationToken.None);

        var acao = () => servico.AceitarAsync(convite.Token, "João Pedro", "11900000000", CancellationToken.None);

        await acao.Should().ThrowAsync<ConviteContatoDivergenteException>();
        contexto.Usuarios.Usuarios.Should().ContainSingle(u => u.Id == professorId);
    }

    [Fact]
    public async Task AceitarAsync_SemMatriculaIdDeOrigemEContatoNovo_CriaUsuarioAlunoEMatriculaVinculadaNova()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var convite = await servico.GerarAsync(professorId, "11987654321", null, CancellationToken.None);

        var resultado = await servico.AceitarAsync(convite.Token, "João Pedro", "11987654321", CancellationToken.None);

        resultado.Usuario.Nome.Should().Be("João Pedro");
        resultado.Usuario.Contato.Should().Be("11987654321");
        resultado.Usuario.Papeis.Should().ContainSingle(p => p.Papel == PapelUsuario.Aluno);
        resultado.MatriculaPromovida.Should().BeFalse();
        var matriculaCriada = contexto.Matriculas.Matriculas.Should().ContainSingle().Subject;
        matriculaCriada.ProfessorId.Should().Be(professorId);
        matriculaCriada.AlunoUsuarioId.Should().Be(resultado.Usuario.Id);
    }

    [Fact]
    public async Task AceitarAsync_ContatoJaExistenteComoUsuario_ReaproveitaIdentidadeAdicionandoPapelAluno()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var professorConvidado = Usuario.Cadastrar("Maria Professora", "maria@exemplo.com", PapelUsuario.Professor, Clock);
        await contexto.Usuarios.AdicionarAsync(professorConvidado, CancellationToken.None);
        var convite = await servico.GerarAsync(professorId, "maria@exemplo.com", null, CancellationToken.None);

        var resultado = await servico.AceitarAsync(convite.Token, "Maria Professora", "maria@exemplo.com", CancellationToken.None);

        resultado.Usuario.Id.Should().Be(professorConvidado.Id);
        resultado.Usuario.Papeis.Should().Contain(p => p.Papel == PapelUsuario.Professor);
        resultado.Usuario.Papeis.Should().Contain(p => p.Papel == PapelUsuario.Aluno);
        contexto.Usuarios.Usuarios.Should().HaveCount(2);
    }

    [Fact]
    public async Task AceitarAsync_UsuarioJaEAluno_TrataComoSucessoIdempotenteEPromoveMatriculaDeOrigem()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var alunoExistente = Usuario.Cadastrar("João Pedro", "11987654321", PapelUsuario.Aluno, Clock);
        await contexto.Usuarios.AdicionarAsync(alunoExistente, CancellationToken.None);
        var matriculaOrigem = Matricula.CriarProvisoria(professorId, "João Pedro", "2024-013", Clock);
        await contexto.Matriculas.AdicionarAsync(matriculaOrigem, CancellationToken.None);
        var convite = await servico.GerarAsync(professorId, "11987654321", matriculaOrigem.Id, CancellationToken.None);

        var resultado = await servico.AceitarAsync(convite.Token, "João Pedro", "11987654321", CancellationToken.None);

        resultado.Usuario.Id.Should().Be(alunoExistente.Id);
        resultado.MatriculaPromovida.Should().BeTrue();
        matriculaOrigem.AlunoUsuarioId.Should().Be(resultado.Usuario.Id);
    }

    [Fact]
    public async Task AceitarAsync_ComMatriculaIdDeOrigem_PromoveExatamenteAquelaMatriculaPreservandoId()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var matriculaOrigem = Matricula.CriarProvisoria(professorId, "João Pedro", "2024-013", Clock);
        await contexto.Matriculas.AdicionarAsync(matriculaOrigem, CancellationToken.None);
        var convite = await servico.GerarAsync(professorId, "11987654321", matriculaOrigem.Id, CancellationToken.None);

        var resultado = await servico.AceitarAsync(convite.Token, "João Pedro", "11987654321", CancellationToken.None);

        resultado.MatriculaPromovida.Should().BeTrue();
        matriculaOrigem.AlunoUsuarioId.Should().Be(resultado.Usuario.Id);
        matriculaOrigem.NomeProvisorio.Should().Be("João Pedro");
        matriculaOrigem.IdentificadorProvisorio.Should().Be("2024-013");
        contexto.Matriculas.Matriculas.Should().ContainSingle();
    }

    [Fact]
    public async Task AceitarAsync_ConviteExpirado_RejeitaSemCriarOuAlterarNada()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var convite = await servico.GerarAsync(professorId, "11987654321", null, CancellationToken.None);
        var servicoAposExpirar = new ConviteService(
            contexto.Convites, contexto.Matriculas, contexto.Usuarios, new FakeGeradorDeTokenConvite(),
            new FixedClock(Clock.UtcNow.AddDays(DiasValidade + 1)), DiasValidade);

        var acao = () => servicoAposExpirar.AceitarAsync(convite.Token, "João Pedro", "11987654321", CancellationToken.None);

        await acao.Should().ThrowAsync<ConviteExpiradoException>();
        contexto.Usuarios.Usuarios.Should().ContainSingle(u => u.Id == professorId);
        contexto.Matriculas.Matriculas.Should().BeEmpty();
        convite.UsadoEm.Should().BeNull();
    }

    /// <summary>
    /// Regressão de achado de code-review no PR #29: nada em
    /// <see cref="ConviteService.GerarAsync"/> impede dois convites distintos
    /// referenciando a mesma matrícula de origem ainda não promovida (ex:
    /// reenvio acidental). Antes desta correção, aceitar o segundo convite
    /// deixava <see cref="Matricula.Promover"/> lançar
    /// <see cref="MatriculaJaPromovidaException"/> sem tratamento — 500 em
    /// vez de rejeição controlada — e ainda marcava o segundo convite como
    /// usado antes de falhar. Agora a validação ocorre antes de
    /// <see cref="Convite.MarcarUsado"/>, então nada é alterado na rejeição.
    /// </summary>
    [Fact]
    public async Task AceitarAsync_MatriculaOrigemJaPromovidaPorOutroConvite_RejeitaSemMarcarConviteComoUsado()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var matriculaOrigem = Matricula.CriarProvisoria(professorId, "João Pedro", "2024-013", Clock);
        await contexto.Matriculas.AdicionarAsync(matriculaOrigem, CancellationToken.None);
        var conviteA = await servico.GerarAsync(professorId, "11987654321", matriculaOrigem.Id, CancellationToken.None);
        var conviteB = await servico.GerarAsync(professorId, "11987654321", matriculaOrigem.Id, CancellationToken.None);
        await servico.AceitarAsync(conviteA.Token, "João Pedro", "11987654321", CancellationToken.None);

        var acao = () => servico.AceitarAsync(conviteB.Token, "João Pedro", "11987654321", CancellationToken.None);

        await acao.Should().ThrowAsync<MatriculaOrigemInvalidaException>();
        conviteB.UsadoEm.Should().BeNull();
    }

    /// <summary>
    /// Prova formal (issue #5, critério de aceite 1) de N:N: um Aluno já
    /// pleno de um Professor A aceita convite de um Professor B distinto —
    /// uma nova <see cref="Matricula"/> é criada para B, e a matrícula
    /// existente com A não é alterada.
    /// </summary>
    [Fact]
    public async Task AceitarAsync_AlunoJaPlenoDeOutroProfessor_CriaNovaMatriculaSemAlterarAMatriculaExistente()
    {
        var contexto = NovoContexto();
        var servico = NovoServico(contexto);
        var professorAId = await AdicionarProfessorAsync(contexto.Usuarios, "professor-a@exemplo.com");
        var professorBId = await AdicionarProfessorAsync(contexto.Usuarios, "professor-b@exemplo.com");
        var alunoExistente = Usuario.Cadastrar("João Pedro", "11987654321", PapelUsuario.Aluno, Clock);
        await contexto.Usuarios.AdicionarAsync(alunoExistente, CancellationToken.None);
        var matriculaComA = Matricula.CriarVinculada(professorAId, alunoExistente.Id, Clock);
        await contexto.Matriculas.AdicionarAsync(matriculaComA, CancellationToken.None);
        var conviteDoB = await servico.GerarAsync(professorBId, "11987654321", null, CancellationToken.None);

        var resultado = await servico.AceitarAsync(conviteDoB.Token, "João Pedro", "11987654321", CancellationToken.None);

        resultado.Usuario.Id.Should().Be(alunoExistente.Id);
        resultado.MatriculaPromovida.Should().BeFalse();
        contexto.Matriculas.Matriculas.Should().HaveCount(2);
        var matriculaComB = contexto.Matriculas.Matriculas.Should()
            .ContainSingle(m => m.ProfessorId == professorBId).Subject;
        matriculaComB.AlunoUsuarioId.Should().Be(alunoExistente.Id);
        matriculaComA.ProfessorId.Should().Be(professorAId);
        matriculaComA.AlunoUsuarioId.Should().Be(alunoExistente.Id);
    }

    /// <summary>
    /// Prova formal (issue #5, critério de aceite 3) de N:N: um Aluno
    /// provisório do Professor A (matrícula sem <see cref="Matricula.AlunoUsuarioId"/>)
    /// se cadastra pleno via convite do Professor B sem matrícula de origem —
    /// a matrícula provisória de A permanece intacta, e uma nova matrícula
    /// plena é criada para B.
    /// </summary>
    [Fact]
    public async Task AceitarAsync_ConviteDeOutroProfessorSemMatriculaDeOrigem_PreservaMatriculaProvisoriaExistente()
    {
        var contexto = NovoContexto();
        var servico = NovoServico(contexto);
        var professorAId = await AdicionarProfessorAsync(contexto.Usuarios, "professor-a@exemplo.com");
        var professorBId = await AdicionarProfessorAsync(contexto.Usuarios, "professor-b@exemplo.com");
        var matriculaProvisoriaComA = Matricula.CriarProvisoria(professorAId, "João Pedro", "2024-013", Clock);
        await contexto.Matriculas.AdicionarAsync(matriculaProvisoriaComA, CancellationToken.None);
        var conviteDoB = await servico.GerarAsync(professorBId, "11987654321", null, CancellationToken.None);

        var resultado = await servico.AceitarAsync(conviteDoB.Token, "João Pedro", "11987654321", CancellationToken.None);

        matriculaProvisoriaComA.AlunoUsuarioId.Should().BeNull();
        matriculaProvisoriaComA.NomeProvisorio.Should().Be("João Pedro");
        matriculaProvisoriaComA.IdentificadorProvisorio.Should().Be("2024-013");
        contexto.Matriculas.Matriculas.Should().HaveCount(2);
        var matriculaComB = contexto.Matriculas.Matriculas.Should()
            .ContainSingle(m => m.ProfessorId == professorBId).Subject;
        matriculaComB.AlunoUsuarioId.Should().Be(resultado.Usuario.Id);
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
