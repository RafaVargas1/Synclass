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

        var convite = (await servico.GerarAsync(professorId, "11987654321", matriculaId: null, CancellationToken.None)).Convite;

        convite.ProfessorId.Should().Be(professorId);
        convite.Contato.Should().Be("11987654321");
        convite.ContatoTipo.Should().Be(TipoContato.Telefone);
        convite.Token.Should().Be("token-1");
        convite.Codigo.Should().Be("00001");
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

        var convite = (await servico.GerarAsync(professorId, "11987654321", matricula.Id, CancellationToken.None)).Convite;

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

    /// <summary>
    /// Colisão de código contra convite ainda válido (não usado, não
    /// expirado): o gerador é chamado de novo até achar um código livre —
    /// item 3 da ordem de execução da issue #62.
    /// </summary>
    [Fact]
    public async Task GerarAsync_CodigoColideComConviteAtivo_ChamaGeradorNovamenteAteAcharCodigoLivre()
    {
        var contexto = NovoContexto();
        var geradorDeCodigo = new FakeGeradorDeCodigoConvite("11111", "11111", "22222");
        var servico = NovoServico(contexto, geradorDeCodigo);
        var professorId = await AdicionarProfessorAsync(contexto.Usuarios, "professor@exemplo.com");
        await servico.GerarAsync(professorId, "11987654321", null, CancellationToken.None);

        var resultado = await servico.GerarAsync(professorId, "11900000000", null, CancellationToken.None);

        resultado.Convite.Codigo.Should().Be("22222");
        resultado.Tentativas.Should().Be(2);
        geradorDeCodigo.VezesChamado.Should().Be(3);
    }

    /// <summary>
    /// Código de um convite já usado ou expirado pode ser reaproveitado por
    /// um novo convite: a checagem de unicidade ignora convites finalizados
    /// — item 4 da ordem de execução da issue #62.
    /// </summary>
    [Fact]
    public async Task GerarAsync_CodigoPertenceAConviteUsado_ReaproveitaCodigoSemRetry()
    {
        var contexto = NovoContexto();
        var geradorDeCodigo = new FakeGeradorDeCodigoConvite("11111", "11111");
        var servico = NovoServico(contexto, geradorDeCodigo);
        var professorId = await AdicionarProfessorAsync(contexto.Usuarios, "professor@exemplo.com");
        var conviteAntigo = (await servico.GerarAsync(professorId, "11987654321", null, CancellationToken.None)).Convite;
        conviteAntigo.MarcarUsado(Clock);

        var resultado = await servico.GerarAsync(professorId, "11900000000", null, CancellationToken.None);

        resultado.Convite.Codigo.Should().Be("11111");
        resultado.Tentativas.Should().Be(1);
    }

    /// <summary>
    /// Mesma reutilização acima, mas para um convite expirado em vez de
    /// usado — item 4 da ordem de execução da issue #62.
    /// </summary>
    [Fact]
    public async Task GerarAsync_CodigoPertenceAConviteExpirado_ReaproveitaCodigoSemRetry()
    {
        var contexto = NovoContexto();
        var geradorDeCodigo = new FakeGeradorDeCodigoConvite("11111", "11111");
        var servico = NovoServico(contexto, geradorDeCodigo);
        var professorId = await AdicionarProfessorAsync(contexto.Usuarios, "professor@exemplo.com");
        await servico.GerarAsync(professorId, "11987654321", null, CancellationToken.None);
        var servicoAposExpirar = new ConviteService(
            contexto.Convites, contexto.Matriculas, contexto.Usuarios, new FakeGeradorDeTokenConvite(),
            geradorDeCodigo, new FixedClock(Clock.UtcNow.AddDays(DiasValidade + 1)), DiasValidade);

        var resultado = await servicoAposExpirar.GerarAsync(professorId, "11900000000", null, CancellationToken.None);

        resultado.Convite.Codigo.Should().Be("11111");
        resultado.Tentativas.Should().Be(1);
    }

    /// <summary>
    /// Teto de tentativas do loop de geração de código único (edge point de
    /// docs/specs/62-codigo-convite-curto/implementation.md): sem ele, uma
    /// colisão persistente prenderia a requisição num loop indefinido.
    /// </summary>
    [Fact]
    public async Task GerarAsync_CodigoColideSempre_RejeitaComLimiteDeTentativasDeCodigoConviteExcedidoAposVinteTentativas()
    {
        var contexto = NovoContexto();
        var codigosSempreColidindo = Enumerable.Repeat("11111", 21).ToArray();
        var geradorDeCodigo = new FakeGeradorDeCodigoConvite(codigosSempreColidindo);
        var servico = NovoServico(contexto, geradorDeCodigo);
        var professorId = await AdicionarProfessorAsync(contexto.Usuarios, "professor@exemplo.com");
        await servico.GerarAsync(professorId, "11987654321", null, CancellationToken.None);

        var acao = () => servico.GerarAsync(professorId, "11900000000", null, CancellationToken.None);

        await acao.Should().ThrowAsync<LimiteDeTentativasDeCodigoConviteExcedidoException>();
        geradorDeCodigo.VezesChamado.Should().Be(21);
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
        var convite = (await servico.GerarAsync(professorId, "11987654321", null, CancellationToken.None)).Convite;

        var acao = () => servico.AceitarAsync(convite.Token, "João Pedro", "11900000000", CancellationToken.None);

        await acao.Should().ThrowAsync<ConviteContatoDivergenteException>();
        contexto.Usuarios.Usuarios.Should().ContainSingle(u => u.Id == professorId);
    }

    [Fact]
    public async Task AceitarAsync_SemMatriculaIdDeOrigemEContatoNovo_CriaUsuarioAlunoEMatriculaVinculadaNova()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var convite = (await servico.GerarAsync(professorId, "11987654321", null, CancellationToken.None)).Convite;

        var resultado = await servico.AceitarAsync(convite.Token, "João Pedro", "11987654321", CancellationToken.None);

        resultado.Usuario.Nome.Should().Be("João Pedro");
        resultado.Usuario.Contato.Should().Be("11987654321");
        resultado.Usuario.Papeis.Should().ContainSingle(p => p.Papel == PapelUsuario.Aluno);
        resultado.MatriculaPromovida.Should().BeFalse();
        resultado.PapelAdicionado.Should().BeFalse();
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
        var convite = (await servico.GerarAsync(professorId, "maria@exemplo.com", null, CancellationToken.None)).Convite;

        var resultado = await servico.AceitarAsync(convite.Token, "Maria Professora", "maria@exemplo.com", CancellationToken.None);

        resultado.Usuario.Id.Should().Be(professorConvidado.Id);
        resultado.Usuario.Papeis.Should().Contain(p => p.Papel == PapelUsuario.Professor);
        resultado.Usuario.Papeis.Should().Contain(p => p.Papel == PapelUsuario.Aluno);
        resultado.PapelAdicionado.Should().BeTrue();
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
        var convite = (await servico.GerarAsync(professorId, "11987654321", matriculaOrigem.Id, CancellationToken.None)).Convite;

        var resultado = await servico.AceitarAsync(convite.Token, "João Pedro", "11987654321", CancellationToken.None);

        resultado.Usuario.Id.Should().Be(alunoExistente.Id);
        resultado.MatriculaPromovida.Should().BeTrue();
        resultado.PapelAdicionado.Should().BeFalse();
        matriculaOrigem.AlunoUsuarioId.Should().Be(resultado.Usuario.Id);
    }

    [Fact]
    public async Task AceitarAsync_ComMatriculaIdDeOrigem_PromoveExatamenteAquelaMatriculaPreservandoId()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var matriculaOrigem = Matricula.CriarProvisoria(professorId, "João Pedro", "2024-013", Clock);
        await contexto.Matriculas.AdicionarAsync(matriculaOrigem, CancellationToken.None);
        var convite = (await servico.GerarAsync(professorId, "11987654321", matriculaOrigem.Id, CancellationToken.None)).Convite;

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
        var convite = (await servico.GerarAsync(professorId, "11987654321", null, CancellationToken.None)).Convite;
        var servicoAposExpirar = new ConviteService(
            contexto.Convites, contexto.Matriculas, contexto.Usuarios, new FakeGeradorDeTokenConvite(),
            new FakeGeradorDeCodigoConvite(), new FixedClock(Clock.UtcNow.AddDays(DiasValidade + 1)), DiasValidade);

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
        var conviteA = (await servico.GerarAsync(professorId, "11987654321", matriculaOrigem.Id, CancellationToken.None)).Convite;
        var conviteB = (await servico.GerarAsync(professorId, "11987654321", matriculaOrigem.Id, CancellationToken.None)).Convite;
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
        var conviteDoB = (await servico.GerarAsync(professorBId, "11987654321", null, CancellationToken.None)).Convite;

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
        var conviteDoB = (await servico.GerarAsync(professorBId, "11987654321", null, CancellationToken.None)).Convite;

        var resultado = await servico.AceitarAsync(conviteDoB.Token, "João Pedro", "11987654321", CancellationToken.None);

        matriculaProvisoriaComA.AlunoUsuarioId.Should().BeNull();
        matriculaProvisoriaComA.NomeProvisorio.Should().Be("João Pedro");
        matriculaProvisoriaComA.IdentificadorProvisorio.Should().Be("2024-013");
        contexto.Matriculas.Matriculas.Should().HaveCount(2);
        var matriculaComB = contexto.Matriculas.Matriculas.Should()
            .ContainSingle(m => m.ProfessorId == professorBId).Subject;
        matriculaComB.AlunoUsuarioId.Should().Be(resultado.Usuario.Id);
    }

    [Fact]
    public async Task AceitarPorCodigoAsync_CodigoValidoEAlunoJaAutenticado_CriaVinculoEMarcaConviteComoUsado()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var alunoExistente = Usuario.Cadastrar("João Pedro", "11987654321", PapelUsuario.Aluno, Clock);
        await contexto.Usuarios.AdicionarAsync(alunoExistente, CancellationToken.None);
        var convite = (await servico.GerarAsync(professorId, "11987654321", null, CancellationToken.None)).Convite;

        var resultado = await servico.AceitarPorCodigoAsync(convite.Codigo, "João Pedro", "11987654321", CancellationToken.None);

        resultado.Usuario.Id.Should().Be(alunoExistente.Id);
        var vinculoCriado = contexto.Matriculas.Matriculas.Should()
            .ContainSingle(m => m.ProfessorId == professorId && m.AlunoUsuarioId == alunoExistente.Id).Subject;
        vinculoCriado.Should().NotBeNull();
        convite.UsadoEm.Should().NotBeNull();
    }

    [Fact]
    public async Task AceitarPorCodigoAsync_CodigoExpirado_RejeitaComConviteExpiradoException()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var convite = (await servico.GerarAsync(professorId, "11987654321", null, CancellationToken.None)).Convite;
        var servicoAposExpirar = new ConviteService(
            contexto.Convites, contexto.Matriculas, contexto.Usuarios, new FakeGeradorDeTokenConvite(),
            new FakeGeradorDeCodigoConvite(), new FixedClock(Clock.UtcNow.AddDays(DiasValidade + 1)), DiasValidade);

        var acao = () => servicoAposExpirar.AceitarPorCodigoAsync(convite.Codigo, "João Pedro", "11987654321", CancellationToken.None);

        await acao.Should().ThrowAsync<ConviteExpiradoException>();
        convite.UsadoEm.Should().BeNull();
    }

    [Fact]
    public async Task AceitarPorCodigoAsync_CodigoJaUsado_RejeitaComConviteInvalidoException()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var convite = (await servico.GerarAsync(professorId, "11987654321", null, CancellationToken.None)).Convite;
        convite.MarcarUsado(Clock);

        var acao = () => servico.AceitarPorCodigoAsync(convite.Codigo, "João Pedro", "11987654321", CancellationToken.None);

        await acao.Should().ThrowAsync<ConviteInvalidoException>();
        contexto.Convites.Convites.Should().ContainSingle();
    }

    [Fact]
    public async Task AceitarPorCodigoAsync_CodigoInexistente_RejeitaComConviteInvalidoException()
    {
        var contexto = NovoContexto();
        var servico = NovoServico(contexto);

        var acao = () => servico.AceitarPorCodigoAsync("99999", "João Pedro", "11987654321", CancellationToken.None);

        await acao.Should().ThrowAsync<ConviteInvalidoException>();
    }

    [Fact]
    public async Task AceitarPorCodigoAsync_AlunoSemContaComCodigoValido_CriaUsuarioAlunoVinculadoAoProfessor()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var convite = (await servico.GerarAsync(professorId, "11987654321", null, CancellationToken.None)).Convite;

        var resultado = await servico.AceitarPorCodigoAsync(convite.Codigo, "João Pedro", "11987654321", CancellationToken.None);

        resultado.Usuario.Nome.Should().Be("João Pedro");
        resultado.Usuario.Papeis.Should().ContainSingle(p => p.Papel == PapelUsuario.Aluno);
        var matriculaCriada = contexto.Matriculas.Matriculas.Should().ContainSingle().Subject;
        matriculaCriada.ProfessorId.Should().Be(professorId);
        matriculaCriada.AlunoUsuarioId.Should().Be(resultado.Usuario.Id);
    }

    [Fact]
    public async Task AceitarPorCodigoAsync_ContatoJaExistenteComoProfessor_AdicionaPapelAlunoNaMesmaConta()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var professorConvidado = Usuario.Cadastrar("Maria Professora", "maria@exemplo.com", PapelUsuario.Professor, Clock);
        await contexto.Usuarios.AdicionarAsync(professorConvidado, CancellationToken.None);
        var convite = (await servico.GerarAsync(professorId, "maria@exemplo.com", null, CancellationToken.None)).Convite;

        var resultado = await servico.AceitarPorCodigoAsync(convite.Codigo, "Maria Professora", "maria@exemplo.com", CancellationToken.None);

        resultado.Usuario.Id.Should().Be(professorConvidado.Id);
        resultado.Usuario.Papeis.Should().Contain(p => p.Papel == PapelUsuario.Professor);
        resultado.Usuario.Papeis.Should().Contain(p => p.Papel == PapelUsuario.Aluno);
        resultado.PapelAdicionado.Should().BeTrue();
        contexto.Usuarios.Usuarios.Should().HaveCount(2);
    }

    /// <summary>
    /// Critério de aceite 5 da issue #63: diferente do fluxo por link, o
    /// fluxo por código rejeita um vínculo já existente em vez de o
    /// reaproveitar silenciosamente — o convite não deve ser marcado como
    /// usado na rejeição (mesma garantia de "rejeição não muda nada" de
    /// <see cref="ConviteService"/>).
    /// </summary>
    [Fact]
    public async Task AceitarPorCodigoAsync_AlunoJaVinculadoAoProfessor_RejeitaComContatoJaVinculadoExceptionSemMarcarConviteComoUsado()
    {
        var (servico, contexto, professorId) = await CriarServicoComProfessorExistenteAsync();
        var alunoExistente = Usuario.Cadastrar("João Pedro", "11987654321", PapelUsuario.Aluno, Clock);
        await contexto.Usuarios.AdicionarAsync(alunoExistente, CancellationToken.None);
        var convite = (await servico.GerarAsync(professorId, "11987654321", null, CancellationToken.None)).Convite;
        var vinculo = Matricula.CriarVinculada(professorId, alunoExistente.Id, Clock);
        await contexto.Matriculas.AdicionarAsync(vinculo, CancellationToken.None);

        var acao = () => servico.AceitarPorCodigoAsync(convite.Codigo, "João Pedro", "11987654321", CancellationToken.None);

        await acao.Should().ThrowAsync<ContatoJaVinculadoException>();
        convite.UsadoEm.Should().BeNull();
        contexto.Matriculas.Matriculas.Should().ContainSingle();
    }

    [Theory]
    [InlineData("12 345")]
    [InlineData("1-2-3-4-5")]
    public async Task AceitarPorCodigoAsync_CodigoComEspacosOuMascara_NormalizaEquivalenteASoDigitos(string codigoComMascara)
    {
        var contexto = NovoContexto();
        var geradorDeCodigo = new FakeGeradorDeCodigoConvite("12345");
        var servico = NovoServico(contexto, geradorDeCodigo);
        var professorId = await AdicionarProfessorAsync(contexto.Usuarios, "professor@exemplo.com");
        await servico.GerarAsync(professorId, "11987654321", null, CancellationToken.None);

        var resultado = await servico.AceitarPorCodigoAsync(codigoComMascara, "João Pedro", "11987654321", CancellationToken.None);

        resultado.Usuario.Contato.Should().Be("11987654321");
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

    private static ConviteService NovoServico(Contexto contexto, IGeradorDeCodigoConvite? geradorDeCodigo = null)
    {
        return new ConviteService(
            contexto.Convites, contexto.Matriculas, contexto.Usuarios, new FakeGeradorDeTokenConvite(),
            geradorDeCodigo ?? new FakeGeradorDeCodigoConvite(), Clock, DiasValidade);
    }

    private static async Task<Guid> AdicionarProfessorAsync(FakeUsuarioRepository usuarios, string contato)
    {
        var professor = Usuario.Cadastrar("Professor Teste", contato, PapelUsuario.Professor, Clock);
        await usuarios.AdicionarAsync(professor, CancellationToken.None);
        return professor.Id;
    }

    private sealed record Contexto(FakeConviteRepository Convites, FakeMatriculaRepository Matriculas, FakeUsuarioRepository Usuarios);
}
