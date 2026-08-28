using FluentAssertions;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Pagamentos;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Pagamentos;

/// <summary>
/// Cobre <see cref="PagamentoService.IniciarAsync"/> (issue #199): checagem
/// de posse da Matrícula, Professor sem conta conectada, valor zero no
/// período, reaproveitamento de um <c>Pendente</c> existente e criação de
/// novo <see cref="Pagamento"/> com valor congelado — ver
/// implementation.md#fluxo-de-pagamentoserviceiniciarasync.
/// </summary>
public sealed class PagamentoServiceTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero));
    private const string RedirectUri = "https://api.synclass.com.br/professores/mercado-pago/callback";

    private static readonly PeriodoConsulta Periodo =
        PeriodoConsulta.Criar(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1));

    private static PagamentoService CriarServico(
        FakeMatriculaRepository matriculas,
        FakePagamentoRepository pagamentos,
        FakeGeradorDeCheckout gerador,
        ConexaoMercadoPagoService? conexoes = null,
        ConsultaCobrancaService? consulta = null,
        FakeConexaoMercadoPagoRepository? repositorioConexoes = null)
    {
        var usuarios = new FakeUsuarioRepository();
        var conexao = conexoes ?? CriarConexao(repositorioConexoes ?? new FakeConexaoMercadoPagoRepository(), usuarios);
        var servicoConsulta = consulta ?? CriarConsulta(matriculas, regras: null, usuarios);
        return new PagamentoService(matriculas, conexao, servicoConsulta, pagamentos, gerador, Clock);
    }

    private static ConexaoMercadoPagoService CriarConexao(
        FakeConexaoMercadoPagoRepository repositorio, FakeUsuarioRepository usuarios)
    {
        return new ConexaoMercadoPagoService(
            repositorio, usuarios, new FakeClienteOAuthMercadoPago(), Clock, RedirectUri);
    }

    private static ConsultaCobrancaService CriarConsulta(
        FakeMatriculaRepository matriculas,
        FakeRegraDeCobrancaRepository? regras = null,
        FakeUsuarioRepository? usuarios = null)
    {
        return new ConsultaCobrancaService(
            matriculas,
            regras ?? new FakeRegraDeCobrancaRepository(),
            new FakeAlocacaoHorarioRepository(),
            new FakeHorarioRepository(),
            usuarios ?? new FakeUsuarioRepository(),
            new FakeRegistroFrequenciaRepository(new FakeAulaRepository()));
    }

    private static async Task<Guid> CriarProfessorConectadoAsync(
        FakeUsuarioRepository usuarios, FakeConexaoMercadoPagoRepository repositorioConexoes)
    {
        var professor = Usuario.Cadastrar("Professor Pagamentos", $"prof{Guid.NewGuid():N}@exemplo.com", PapelUsuario.Professor, null, Clock);
        await usuarios.AdicionarAsync(professor, CancellationToken.None);

        var conexao = ConexaoMercadoPago.IniciarFluxoDeAutorizacao(professor.Id, "state-fluxo", Clock);
        conexao.RegistrarConexao("access", "refresh", "collector-id", Clock.UtcNow.AddHours(1), Clock);
        await repositorioConexoes.AdicionarAsync(conexao, CancellationToken.None);

        return professor.Id;
    }

    /// <summary>
    /// Matrícula de outro Aluno: o <paramref name="professorId"/> não
    /// precisa de conexão porque a checagem de posse (passo 3 do fluxo de
    /// implementation.md) acontece antes de resolver o collector_id.
    /// </summary>
    [Fact]
    public async Task IniciarAsync_MatriculaDeOutroAluno_RejeitaComMatriculaNaoPertenceAoAlunoException()
    {
        var matriculas = new FakeMatriculaRepository();
        var repositorioConexoes = new FakeConexaoMercadoPagoRepository();
        var usuarios = new FakeUsuarioRepository();
        var professorId = await CriarProfessorConectadoAsync(usuarios, repositorioConexoes);
        var donoDaMatricula = Guid.NewGuid();
        var matricula = Matricula.CriarVinculada(professorId, donoDaMatricula, Clock);
        await matriculas.AdicionarAsync(matricula, CancellationToken.None);

        var pagamentos = new FakePagamentoRepository();
        var gerador = new FakeGeradorDeCheckout();
        var servico = CriarServico(matriculas, pagamentos, gerador, repositorioConexoes: repositorioConexoes);

        var acao = () => servico.IniciarAsync(
            matricula.Id, Guid.NewGuid(), Periodo.Inicio, Periodo.FimExclusivo, CancellationToken.None);

        await acao.Should().ThrowAsync<MatriculaNaoPertenceAoAlunoException>();
        pagamentos.Pagamentos.Should().BeEmpty();
        gerador.Chamadas.Should().Be(0);
    }

    /// <summary>
    /// Professor sem conta Mercado Pago conectada: o collector_id não é
    /// resolvido (passo 4 do fluxo de implementation.md — contrato fechado
    /// com a #203 retorna null) e o fluxo para antes de criar/reaproveitar
    /// qualquer pagamento ou gerar checkout.
    /// </summary>
    [Fact]
    public async Task IniciarAsync_ProfessorSemContaConectada_RejeitaComProfessorSemContaConectadaExceptionSemCriarPagamento()
    {
        var matriculas = new FakeMatriculaRepository();
        var repositorioConexoes = new FakeConexaoMercadoPagoRepository(); // vazio → professor não conectado
        var alunoUsuarioId = Guid.NewGuid();
        var professorId = Guid.NewGuid();
        var matricula = Matricula.CriarVinculada(professorId, alunoUsuarioId, Clock);
        await matriculas.AdicionarAsync(matricula, CancellationToken.None);

        var pagamentos = new FakePagamentoRepository();
        var gerador = new FakeGeradorDeCheckout();
        var servico = CriarServico(matriculas, pagamentos, gerador, repositorioConexoes: repositorioConexoes);

        var acao = () => servico.IniciarAsync(
            matricula.Id, alunoUsuarioId, Periodo.Inicio, Periodo.FimExclusivo, CancellationToken.None);

        await acao.Should().ThrowAsync<ProfessorSemContaConectadaException>();
        pagamentos.Pagamentos.Should().BeEmpty();
        gerador.Chamadas.Should().Be(0);
    }

    /// <summary>
    /// Valor zero no período: a matrícula pertence ao Aluno, o Professor tem
    /// conta conectada, mas <see cref="ConsultaCobrancaService.ConsultarPorAlunoAsync"/>
    /// não devolve valor &gt; 0 pra matrícula (passo 5 do fluxo) — rejeita com
    /// <see cref="SemValorDevidoException"/> (mapeada a 400) antes de
    /// criar/reaproveitar pagamento ou gerar checkout.
    /// </summary>
    [Fact]
    public async Task IniciarAsync_SemValorDevidoNoPeriodo_RejeitaComSemValorDevidoExceptionSemCriarPagamento()
    {
        var matriculas = new FakeMatriculaRepository();
        var repositorioConexoes = new FakeConexaoMercadoPagoRepository();
        var usuarios = new FakeUsuarioRepository();
        var professorId = await CriarProfessorConectadoAsync(usuarios, repositorioConexoes);
        var alunoUsuarioId = Guid.NewGuid();
        // Matrícula plena, mas sem RegraDeCobranca → ConsultaCobrancaService
        // devolve SemRegraDefinida=true, Valor=null (nunca 0; ver #12).
        var matricula = Matricula.CriarVinculada(professorId, alunoUsuarioId, Clock);
        await matriculas.AdicionarAsync(matricula, CancellationToken.None);

        var pagamentos = new FakePagamentoRepository();
        var gerador = new FakeGeradorDeCheckout();
        var servico = CriarServico(matriculas, pagamentos, gerador, repositorioConexoes: repositorioConexoes);

        var acao = () => servico.IniciarAsync(
            matricula.Id, alunoUsuarioId, Periodo.Inicio, Periodo.FimExclusivo, CancellationToken.None);

        await acao.Should().ThrowAsync<SemValorDevidoException>();
        pagamentos.Pagamentos.Should().BeEmpty();
        gerador.Chamadas.Should().Be(0);
    }

    /// <summary>
    /// Já existe um <see cref="Pagamento"/> <c>Confirmado</c> pra mesma
    /// (MatriculaId, período): <see cref="ConsultaCobrancaService"/> não sabe
    /// de <see cref="Pagamento"/> (não é alterado por esta Task), então sem
    /// esta checagem o fluxo criaria uma segunda cobrança pra um período já
    /// pago. Rejeita com <see cref="SemValorDevidoException"/> (mesma
    /// exceção de "nada devido" — já pago também é "nada devido") sem gerar
    /// novo checkout.
    /// </summary>
    [Fact]
    public async Task IniciarAsync_JaConfirmadoNoPeriodo_RejeitaComSemValorDevidoExceptionSemCriarNovoPagamento()
    {
        var matriculas = new FakeMatriculaRepository();
        var repositorioConexoes = new FakeConexaoMercadoPagoRepository();
        var usuarios = new FakeUsuarioRepository();
        var professorId = await CriarProfessorConectadoAsync(usuarios, repositorioConexoes);
        var alunoUsuarioId = Guid.NewGuid();
        var matricula = Matricula.CriarVinculada(professorId, alunoUsuarioId, Clock);
        await matriculas.AdicionarAsync(matricula, CancellationToken.None);

        var regras = new FakeRegraDeCobrancaRepository();
        await regras.SalvarAsync(RegraFixoMensal.Criar(matricula.Id, 300m, Clock), CancellationToken.None);
        var consulta = CriarConsulta(matriculas, regras, usuarios);

        var pagamentoConfirmado = new Pagamento(
            Guid.NewGuid(), matricula.Id, alunoUsuarioId, professorId, 300m,
            Periodo.Inicio, Periodo.FimExclusivo, "https://checkout.mercadopago.com/pago", "pref-pago", Clock);
        pagamentoConfirmado.Confirmar(Clock);
        var pagamentos = new FakePagamentoRepository();
        await pagamentos.AdicionarAsync(pagamentoConfirmado, CancellationToken.None);

        var gerador = new FakeGeradorDeCheckout();
        var servico = CriarServico(matriculas, pagamentos, gerador, consulta: consulta, repositorioConexoes: repositorioConexoes);

        var acao = () => servico.IniciarAsync(
            matricula.Id, alunoUsuarioId, Periodo.Inicio, Periodo.FimExclusivo, CancellationToken.None);

        await acao.Should().ThrowAsync<SemValorDevidoException>();
        gerador.Chamadas.Should().Be(0);
        pagamentos.Pagamentos.Should().ContainSingle();
    }

    /// <summary>
    /// Reaproveitamento de um <c>Pendente</c> existente da mesma
    /// (MatriculaId, período): o fluxo (passo 6) devolve o mesmo
    /// <see cref="Pagamento.UrlCheckout"/> e <see cref="Pagamento.Id"/> sem
    /// gerar nova preferência de checkout no Mercado Pago — a URL é gravada
    /// na criação justamente pra isso (ver implementation.md#entidade-pagamento).
    /// O resultado carrega o <c>ProfessorId</c> e a
    /// <see cref="Pagamento.ReferenciaExterna"/> do pendente para o
    /// controller logar <c>PagamentoIniciado</c> com os campos do
    /// implementation.md#logs-estruturados.
    /// </summary>
    [Fact]
    public async Task IniciarAsync_PendenteExistente_ReaproveitaMesmaUrlCheckoutSemChamarIGeradorDeCheckout()
    {
        var matriculas = new FakeMatriculaRepository();
        var repositorioConexoes = new FakeConexaoMercadoPagoRepository();
        var usuarios = new FakeUsuarioRepository();
        var professorId = await CriarProfessorConectadoAsync(usuarios, repositorioConexoes);
        var alunoUsuarioId = Guid.NewGuid();
        var matricula = Matricula.CriarVinculada(professorId, alunoUsuarioId, Clock);
        await matriculas.AdicionarAsync(matricula, CancellationToken.None);

        var regras = new FakeRegraDeCobrancaRepository();
        await regras.SalvarAsync(RegraFixoMensal.Criar(matricula.Id, 300m, Clock), CancellationToken.None);
        var consulta = CriarConsulta(matriculas, regras, usuarios);

        var pagamentoPendente = new Pagamento(
            Guid.NewGuid(), matricula.Id, alunoUsuarioId, professorId, 300m,
            Periodo.Inicio, Periodo.FimExclusivo, "https://checkout.mercadopago.com/pendente", "pref-pendente", Clock);
        var pagamentos = new FakePagamentoRepository();
        await pagamentos.AdicionarAsync(pagamentoPendente, CancellationToken.None);

        var gerador = new FakeGeradorDeCheckout();
        var servico = CriarServico(matriculas, pagamentos, gerador, consulta: consulta, repositorioConexoes: repositorioConexoes);

        var resultado = await servico.IniciarAsync(
            matricula.Id, alunoUsuarioId, Periodo.Inicio, Periodo.FimExclusivo, CancellationToken.None);

        resultado.PagamentoId.Should().Be(pagamentoPendente.Id);
        resultado.UrlCheckout.Should().Be("https://checkout.mercadopago.com/pendente");
        resultado.Valor.Should().Be(300m);
        resultado.ProfessorId.Should().Be(professorId);
        resultado.ReferenciaExterna.Should().Be("pref-pendente");
        gerador.Chamadas.Should().Be(0);
        pagamentos.Pagamentos.Should().ContainSingle();
    }

    /// <summary>
    /// Sem pendente existente (passos 7-9 do fluxo): gera o id do pagamento
    /// ANTES da preferência (vira o external_reference), cria a preferência
    /// de checkout com o collector_id do Professor e persiste um novo
    /// <see cref="Pagamento"/> <c>Pendente</c> com o valor congelado da
    /// criação — nunca recalculado ao confirmar (ver implementation.md#entidade-pagamento).
    /// O resultado carrega o <c>ProfessorId</c> e a
    /// <see cref="Pagamento.ReferenciaExterna"/> do pagamento criado para o
    /// controller logar <c>PagamentoIniciado</c> (implementation.md#logs-estruturados).
    /// </summary>
    [Fact]
    public async Task IniciarAsync_SemPendenteExistente_CriaNovoPagamentoComValorCongelado()
    {
        var matriculas = new FakeMatriculaRepository();
        var repositorioConexoes = new FakeConexaoMercadoPagoRepository();
        var usuarios = new FakeUsuarioRepository();
        var professorId = await CriarProfessorConectadoAsync(usuarios, repositorioConexoes);
        var alunoUsuarioId = Guid.NewGuid();
        var matricula = Matricula.CriarVinculada(professorId, alunoUsuarioId, Clock);
        await matriculas.AdicionarAsync(matricula, CancellationToken.None);

        var regras = new FakeRegraDeCobrancaRepository();
        await regras.SalvarAsync(RegraFixoMensal.Criar(matricula.Id, 300m, Clock), CancellationToken.None);
        var consulta = CriarConsulta(matriculas, regras, usuarios);

        var pagamentos = new FakePagamentoRepository();
        var gerador = new FakeGeradorDeCheckout();
        var servico = CriarServico(matriculas, pagamentos, gerador, consulta: consulta, repositorioConexoes: repositorioConexoes);

        var resultado = await servico.IniciarAsync(
            matricula.Id, alunoUsuarioId, Periodo.Inicio, Periodo.FimExclusivo, CancellationToken.None);

        resultado.PagamentoId.Should().NotBeEmpty();
        resultado.UrlCheckout.Should().Be("https://checkout.mercadopago.com/pref-teste");
        resultado.Valor.Should().Be(300m);
        resultado.ProfessorId.Should().Be(professorId);
        resultado.ReferenciaExterna.Should().Be("pref-teste");
        gerador.Chamadas.Should().Be(1);
        gerador.UltimoProfessorId.Should().Be(professorId);
        gerador.UltimoCollectorId.Should().Be("collector-id");
        // valor congelado na criação = valor devido calculado no passo 5.
        gerador.UltimoValor.Should().Be(300m);
        // external_reference casa com o id do pagamento persistido.
        gerador.UltimoExternalReference.Should().Be(resultado.PagamentoId.ToString());

        var persistido = pagamentos.Pagamentos.Should().ContainSingle().Subject;
        persistido.Id.Should().Be(resultado.PagamentoId);
        persistido.Status.Should().Be(StatusPagamento.Pendente);
        persistido.Valor.Should().Be(300m);
        persistido.PeriodoInicio.Should().Be(Periodo.Inicio);
        persistido.PeriodoFimExclusivo.Should().Be(Periodo.FimExclusivo);
        persistido.UrlCheckout.Should().Be("https://checkout.mercadopago.com/pref-teste");
        persistido.ReferenciaExterna.Should().Be("pref-teste");
    }
}
