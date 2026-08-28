using FluentAssertions;
using Synclass.Domain.Pagamentos;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Usuarios;

namespace Synclass.Domain.Tests.Pagamentos;

/// <summary>
/// Cobre os use cases de ConexaoMercadoPagoService (issue #203): iniciar o
/// fluxo OAuth (ConectarAsync), processar o callback (ProcessarCallbackAsync)
/// e resolver o collectorId (ObterCollectorIdAsync). Segue o padrão de
/// fakes manuais e nomenclatura Metodo_Cenario_ResultadoEsperado de
/// docs/spec/203-professor-conecta-mercado-pago/implementation.md.
/// </summary>
public sealed class ConexaoMercadoPagoServiceTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 25, 12, 0, 0, TimeSpan.Zero));
    private const string RedirectUri = "https://api.synclass.com.br/professores/mercado-pago/callback";

    private static ConexaoMercadoPagoService CriarServico(
        FakeConexaoMercadoPagoRepository repositorio,
        FakeUsuarioRepository usuarios,
        IClienteOAuthMercadoPago cliente)
    {
        return new ConexaoMercadoPagoService(repositorio, usuarios, cliente, Clock, RedirectUri);
    }

    private static async Task<Guid> CriarProfessorAsync(FakeUsuarioRepository usuarios)
    {
        var professor = Usuario.Cadastrar("Maria Silva", $"maria{Guid.NewGuid():N}@exemplo.com", PapelUsuario.Professor, null, Clock);
        await usuarios.AdicionarAsync(professor, CancellationToken.None);
        return professor.Id;
    }

    [Fact]
    public async Task ConectarAsync_ProfessorSemConexao_CriaRegistroComStatePendenteERetornaUrl()
    {
        var usuarios = new FakeUsuarioRepository();
        var professorId = await CriarProfessorAsync(usuarios);
        var repositorio = new FakeConexaoMercadoPagoRepository();
        var cliente = new FakeClienteOAuthMercadoPago();
        var servico = CriarServico(repositorio, usuarios, cliente);

        var url = await servico.ConectarAsync(professorId, CancellationToken.None);

        url.Should().NotBeNullOrWhiteSpace();
        repositorio.Conexoes.Should().ContainSingle();
        var conexao = repositorio.Conexoes.Single();
        conexao.ProfessorId.Should().Be(professorId);
        conexao.State.Should().NotBeNullOrWhiteSpace();
        conexao.StateExpiraEm.Should().Be(Clock.UtcNow.AddMinutes(ConexaoMercadoPago.StateValidadeMinutos));
    }

    [Fact]
    public async Task ConectarAsync_ProfessorSemConexao_GeraUrlComStateERedirectUriCorretos()
    {
        var usuarios = new FakeUsuarioRepository();
        var professorId = await CriarProfessorAsync(usuarios);
        var repositorio = new FakeConexaoMercadoPagoRepository();
        var cliente = new FakeClienteOAuthMercadoPago();
        var servico = CriarServico(repositorio, usuarios, cliente);

        var url = await servico.ConectarAsync(professorId, CancellationToken.None);

        // O state gerado e persistido é o mesmo embutido na URL de autorização,
        // e a redirect_uri fixa é repassada ao cliente OAuth.
        var conexao = repositorio.Conexoes.Single();
        conexao.State.Should().NotBeNullOrWhiteSpace();
        cliente.UltimoStateNaUrl.Should().Be(conexao.State);
        url.Should().Contain(conexao.State);
        cliente.UltimaRedirectUri.Should().Be(RedirectUri);
    }

    [Fact]
    public async Task ConectarAsync_ProfessorJaConectado_ReaproveitaRegistroAtualizandoStateSemCriarSegundo()
    {
        var usuarios = new FakeUsuarioRepository();
        var professorId = await CriarProfessorAsync(usuarios);
        var repositorio = new FakeConexaoMercadoPagoRepository();
        var cliente = new FakeClienteOAuthMercadoPago();

        // Conexão já existente, concluída (credenciais válidas) — reconexão
        // (implementation.md#reconexão) deve reaproveitar o mesmo registro.
        var conexaoExistente = ConexaoMercadoPago.IniciarFluxoDeAutorizacao(professorId, "state-antigo", Clock);
        conexaoExistente.RegistrarConexao("access-antigo", "refresh-antigo", "collector", Clock.UtcNow.AddHours(1), Clock);
        await repositorio.AdicionarAsync(conexaoExistente, CancellationToken.None);

        var servico = CriarServico(repositorio, usuarios, cliente);

        var url = await servico.ConectarAsync(professorId, CancellationToken.None);

        repositorio.Conexoes.Should().ContainSingle();
        var conexao = repositorio.Conexoes.Single();
        conexao.Id.Should().Be(conexaoExistente.Id);
        conexao.State.Should().NotBeNullOrWhiteSpace();
        conexao.State.Should().NotBe("state-antigo");
        conexao.StateExpiraEm.Should().Be(Clock.UtcNow.AddMinutes(ConexaoMercadoPago.StateValidadeMinutos));
        // Credenciais da conexão antiga permanecem válidas até o novo callback
        // confirmar a troca — só o state foi sobrescrito.
        conexao.AccessToken.Should().Be("access-antigo");
        conexao.CollectorId.Should().Be("collector");
        url.Should().Contain(conexao.State);
    }

    [Fact]
    public async Task ProcessarCallbackAsync_StateExpirado_RejeitaAntesDeTrocarCode()
    {
        var usuarios = new FakeUsuarioRepository();
        var professorId = await CriarProfessorAsync(usuarios);
        var repositorio = new FakeConexaoMercadoPagoRepository();
        var cliente = new FakeClienteOAuthMercadoPago();

        // Fluxo iniciado há mais de StateValidadeMinutos (10 min) — a janela
        // do state já venceu quando o callback chega (implementation.md#edge-points).
        var clockDoInicio = new FixedClock(Clock.UtcNow.AddMinutes(-ConexaoMercadoPago.StateValidadeMinutos - 1));
        var conexao = ConexaoMercadoPago.IniciarFluxoDeAutorizacao(professorId, "state-venceu", clockDoInicio);
        await repositorio.AdicionarAsync(conexao, CancellationToken.None);

        var servico = CriarServico(repositorio, usuarios, cliente);

        var acao = async () => await servico.ProcessarCallbackAsync("code", "state-venceu", CancellationToken.None);

        await acao.Should().ThrowAsync<StateInvalidoException>();
        // A troca de code nunca acontece para um fluxo expirado.
        cliente.UltimoCode.Should().BeNull();
        repositorio.Conexoes.Single().AccessToken.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessarCallbackAsync_StateNaoEncontrado_RejeitaAntesDeTrocarCode()
    {
        var usuarios = new FakeUsuarioRepository();
        var professorId = await CriarProfessorAsync(usuarios);
        var repositorio = new FakeConexaoMercadoPagoRepository();
        var cliente = new FakeClienteOAuthMercadoPago();

        // Nenhum registro guarda o state recebido no callback — o fluxo nunca
        // foi iniciado, ou um state de outro fluxo foi reutilizado. O service
        // resolve o Professor pelo state (o callback é anônimo, sem claim) e
        // rejeita antes de qualquer troca de code (implementation.md#edge-points).
        var servico = CriarServico(repositorio, usuarios, cliente);

        var acao = async () => await servico.ProcessarCallbackAsync("code", "state-inexistente", CancellationToken.None);

        await acao.Should().ThrowAsync<StateInvalidoException>();
        cliente.UltimoCode.Should().BeNull();
        repositorio.Conexoes.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessarCallbackAsync_StateValido_PersisteConexaoAposTrocarCode()
    {
        var usuarios = new FakeUsuarioRepository();
        var professorId = await CriarProfessorAsync(usuarios);
        var repositorio = new FakeConexaoMercadoPagoRepository();
        var cliente = new FakeClienteOAuthMercadoPago
        {
            ResultadoTroca = new TrocaCodePorTokenResultado(
                "access-novo", "refresh-novo", "collector-id", Clock.UtcNow.AddHours(1))
        };

        // Fluxo em andamento válido (mesmo Professor, dentro da janela).
        var conexao = ConexaoMercadoPago.IniciarFluxoDeAutorizacao(professorId, "state-valido", Clock);
        await repositorio.AdicionarAsync(conexao, CancellationToken.None);

        var servico = CriarServico(repositorio, usuarios, cliente);

        await servico.ProcessarCallbackAsync("code-do-mp", "state-valido", CancellationToken.None);

        cliente.UltimoCode.Should().Be("code-do-mp");
        var conexaoPersistida = repositorio.Conexoes.Single();
        conexaoPersistida.AccessToken.Should().Be("access-novo");
        conexaoPersistida.RefreshToken.Should().Be("refresh-novo");
        conexaoPersistida.CollectorId.Should().Be("collector-id");
        conexaoPersistida.ExpiraEm.Should().Be(Clock.UtcNow.AddHours(1));
        // Fluxo pendente encerrado: state limpo, não reutilizável.
        conexaoPersistida.State.Should().BeNull();
        conexaoPersistida.StateExpiraEm.Should().BeNull();
    }

    [Fact]
    public async Task ProcessarCallbackAsync_StateValido_RetornaProfessorIdECollectorId()
    {
        var usuarios = new FakeUsuarioRepository();
        var professorId = await CriarProfessorAsync(usuarios);
        var repositorio = new FakeConexaoMercadoPagoRepository();
        var cliente = new FakeClienteOAuthMercadoPago
        {
            ResultadoTroca = new TrocaCodePorTokenResultado(
                "access-novo", "refresh-novo", "collector-id", Clock.UtcNow.AddHours(1))
        };

        // Fluxo em andamento válido (mesmo Professor, dentro da janela). O
        // endpoint de callback é anônimo (sem claim), então o controller
        // depende do retorno pra logar ProfessorConectouMercadoPago com
        // ProfessorId/CollectorId reais (ver implementation.md#decisão-de-design-logging-de-professorconectoumercadopago-sem-violar-camadas).
        var conexao = ConexaoMercadoPago.IniciarFluxoDeAutorizacao(professorId, "state-valido", Clock);
        await repositorio.AdicionarAsync(conexao, CancellationToken.None);

        var servico = CriarServico(repositorio, usuarios, cliente);

        var (professorIdRetornado, collectorIdRetornado) =
            await servico.ProcessarCallbackAsync("code-do-mp", "state-valido", CancellationToken.None);

        professorIdRetornado.Should().Be(professorId);
        collectorIdRetornado.Should().Be("collector-id");
    }

    [Fact]
    public async Task ObterCollectorIdAsync_ProfessorConectado_RetornaCollectorId()
    {
        var usuarios = new FakeUsuarioRepository();
        var professorId = await CriarProfessorAsync(usuarios);
        var repositorio = new FakeConexaoMercadoPagoRepository();
        var cliente = new FakeClienteOAuthMercadoPago();

        // Conexão ativa com token ainda válido (expira em 1h, bem além da
        // margem de renovação de 5 min) — não precisa renovar.
        var conexao = ConexaoMercadoPago.IniciarFluxoDeAutorizacao(professorId, "state-antigo", Clock);
        conexao.RegistrarConexao("access-valido", "refresh-valido", "collector-id", Clock.UtcNow.AddHours(1), Clock);
        await repositorio.AdicionarAsync(conexao, CancellationToken.None);

        var servico = CriarServico(repositorio, usuarios, cliente);

        var collectorId = await servico.ObterCollectorIdAsync(professorId, CancellationToken.None);

        collectorId.Should().Be("collector-id");
        // Token válido: nenhuma renovação disparada.
        cliente.UltimoRefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task ObterCollectorIdAsync_ProfessorSemConexao_RetornaNull()
    {
        var usuarios = new FakeUsuarioRepository();
        var professorId = await CriarProfessorAsync(usuarios);
        var repositorio = new FakeConexaoMercadoPagoRepository();
        var cliente = new FakeClienteOAuthMercadoPago();
        var servico = CriarServico(repositorio, usuarios, cliente);

        // Professor nunca iniciou um fluxo OAuth — nenhum registro de conexão.
        // O contrato com a Task #199 (implementation.md#contrato-com-a-task-199)
        // exige null (não exceção) para "Professor não conectado".
        var collectorId = await servico.ObterCollectorIdAsync(professorId, CancellationToken.None);

        collectorId.Should().BeNull();
        cliente.UltimoRefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task ObterCollectorIdAsync_TokenExpirado_RenovaERetornaCollectorId()
    {
        var usuarios = new FakeUsuarioRepository();
        var professorId = await CriarProfessorAsync(usuarios);
        var repositorio = new FakeConexaoMercadoPagoRepository();
        var cliente = new FakeClienteOAuthMercadoPago
        {
            ResultadoTroca = new TrocaCodePorTokenResultado(
                "access-renovado", "refresh-renovado", "collector-id", Clock.UtcNow.AddHours(1))
        };

        // Token a menos de MargemRenovacaoMinutos (5 min) de expirar — a
        // margem de segurança (implementation.md#edge-points) exige renovar
        // antes de usar, para evitar corrida de borda.
        var conexao = ConexaoMercadoPago.IniciarFluxoDeAutorizacao(professorId, "state-antigo", Clock);
        conexao.RegistrarConexao("access-quase-vencido", "refresh-antigo", "collector-id", Clock.UtcNow.AddMinutes(1), Clock);
        await repositorio.AdicionarAsync(conexao, CancellationToken.None);

        var servico = CriarServico(repositorio, usuarios, cliente);

        var collectorId = await servico.ObterCollectorIdAsync(professorId, CancellationToken.None);

        collectorId.Should().Be("collector-id");
        // Renovação disparada com o refresh_token antigo; as credenciais
        // foram substituídas no registro persistido.
        cliente.UltimoRefreshToken.Should().Be("refresh-antigo");
        var conexaoPersistida = repositorio.Conexoes.Single();
        conexaoPersistida.AccessToken.Should().Be("access-renovado");
        conexaoPersistida.RefreshToken.Should().Be("refresh-renovado");
        conexaoPersistida.CollectorId.Should().Be("collector-id");
    }

    [Fact]
    public async Task ObterCollectorIdAsync_RenovacaoFalha_RetornaNullERemoveRegistro()
    {
        var usuarios = new FakeUsuarioRepository();
        var professorId = await CriarProfessorAsync(usuarios);
        var repositorio = new FakeConexaoMercadoPagoRepository();
        var cliente = new ClienteOAuthQueFalhaNaRenovacao();

        // Token já expirado (passado) — a renovação é obrigatória e, como o
        // refresh_token foi revogado (refresh também 401), o contrato com a
        // Task #199 (implementation.md#contrato-com-a-task-199) exige retornar
        // null e tratar o registro como irrecuperável.
        var conexao = ConexaoMercadoPago.IniciarFluxoDeAutorizacao(professorId, "state-antigo", Clock);
        conexao.RegistrarConexao("access-expirado", "refresh-revogado", "collector-id", Clock.UtcNow.AddHours(-1), Clock);
        await repositorio.AdicionarAsync(conexao, CancellationToken.None);

        var servico = CriarServico(repositorio, usuarios, cliente);

        var collectorId = await servico.ObterCollectorIdAsync(professorId, CancellationToken.None);

        collectorId.Should().BeNull();
        cliente.UltimoRefreshToken.Should().Be("refresh-revogado");
        // Registro removido: token revogado é irrecuperável, manter só
        // acumularia lixo e confundiria o status "conectado".
        repositorio.Conexoes.Should().BeEmpty();
    }
}
