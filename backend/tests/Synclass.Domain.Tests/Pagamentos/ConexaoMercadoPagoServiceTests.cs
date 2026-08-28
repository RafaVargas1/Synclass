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
        FakeClienteOAuthMercadoPago cliente)
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
}
