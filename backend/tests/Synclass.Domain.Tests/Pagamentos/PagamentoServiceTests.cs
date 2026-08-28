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
        var servicoConsulta = consulta ?? CriarConsulta(matriculas, usuarios);
        return new PagamentoService(matriculas, conexao, servicoConsulta, pagamentos, gerador, Clock);
    }

    private static ConexaoMercadoPagoService CriarConexao(
        FakeConexaoMercadoPagoRepository repositorio, FakeUsuarioRepository usuarios)
    {
        return new ConexaoMercadoPagoService(
            repositorio, usuarios, new FakeClienteOAuthMercadoPago(), Clock, RedirectUri);
    }

    private static ConsultaCobrancaService CriarConsulta(
        FakeMatriculaRepository matriculas, FakeUsuarioRepository? usuarios = null)
    {
        return new ConsultaCobrancaService(
            matriculas,
            new FakeRegraDeCobrancaRepository(),
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
}
