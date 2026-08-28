using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synclass.Api.Controllers;
using Synclass.Api.Tests.Fakes;
using Synclass.Domain.Common;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Pagamentos;
using Synclass.Domain.Usuarios;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Api.Tests;

/// <summary>
/// Teste de fumaça do endpoint de iniciação de pagamento do Aluno (issue
/// #199): <c>POST /alunos/matriculas/{matriculaId}/pagamentos</c>. Usa EF
/// Core InMemory e fakes no lugar dos clientes HTTP reais do Mercado Pago
/// (mesmo padrão de <see cref="MercadoPagoEndpointTests"/>): um
/// <see cref="FakeGeradorDeCheckout"/> pro checkout e um
/// <see cref="FakeClienteOAuthMercadoPago"/> pro OAuth que
/// <c>ConexaoMercadoPagoService</c> usa pra renovar token. A regra de
/// cobrança <c>FixoMensal</c> garante valor devido &gt; 0 no período
/// independente de aulas agendadas, então qualquer período válido serve.
/// </summary>
public sealed class PagamentosControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PagamentosControllerTests(WebApplicationFactory<Program> factory)
    {
        var nomeDoBancoEmMemoria = Guid.NewGuid().ToString();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RunMigrationsOnStartup", "false");
            builder.ConfigureServices(services =>
            {
                UseInMemoryDatabase(services, nomeDoBancoEmMemoria);
                UsarGeradorDeCheckoutFixo(services);
                UsarClienteOAuthFixo(services);
            });
        });
    }

    private static void UseInMemoryDatabase(IServiceCollection services, string nomeDoBanco)
    {
        services.RemoveAll<DbContextOptions<SynclassDbContext>>();
        services.AddDbContext<SynclassDbContext>(options =>
            options.UseInMemoryDatabase(nomeDoBanco));
    }

    private static void UsarGeradorDeCheckoutFixo(IServiceCollection services)
    {
        services.RemoveAll<IGeradorDeCheckout>();
        services.AddSingleton<IGeradorDeCheckout>(new FakeGeradorDeCheckout());
    }

    private static void UsarClienteOAuthFixo(IServiceCollection services)
    {
        services.RemoveAll<IClienteOAuthMercadoPago>();
        services.AddSingleton<IClienteOAuthMercadoPago>(new FakeClienteOAuthMercadoPago());
    }

    private static async Task<Guid> CriarProfessorPersistidoAsync(WebApplicationFactory<Program> factory, string nome = "Professor Um")
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var professor = Usuario.Cadastrar(nome, $"{Guid.NewGuid()}@exemplo.com", PapelUsuario.Professor, null, clock);
        dbContext.Usuarios.Add(professor);
        await dbContext.SaveChangesAsync();
        return professor.Id;
    }

    private static async Task ConectarMercadoPagoAsync(WebApplicationFactory<Program> factory, Guid professorId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var conexao = ConexaoMercadoPago.IniciarFluxoDeAutorizacao(professorId, "state-teste", clock);
        conexao.RegistrarConexao(
            "access-teste", "refresh-teste", "collector-teste", clock.UtcNow.AddHours(1), clock);
        dbContext.ConexoesMercadoPago.Add(conexao);
        await dbContext.SaveChangesAsync();
    }

    private static async Task<Guid> VincularMatriculaAsync(WebApplicationFactory<Program> factory, Guid professorId, Guid alunoUsuarioId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var matricula = Matricula.CriarVinculada(professorId, alunoUsuarioId, clock);
        dbContext.Matriculas.Add(matricula);
        await dbContext.SaveChangesAsync();
        return matricula.Id;
    }

    private async Task DefinirRegraFixoMensalAsync(HttpClient clientProfessor, Guid professorId, Guid matriculaId)
    {
        await clientProfessor.PutAsJsonAsync(
            $"/professores/{professorId}/matriculas/{matriculaId}/regra-de-cobranca",
            new DefinirRegraDeCobrancaRequest("FixoMensal", 300m, null));
    }

    [Fact]
    public async Task Post_Pagamentos_ComValorDevido_Devolve201ComPagamentoIdUrlCheckoutEValor()
    {
        var (client, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var clientProfessor = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorPersistidoAsync(_factory, "Professor Pagável");
        await ConectarMercadoPagoAsync(_factory, professorId);
        var matriculaId = await VincularMatriculaAsync(_factory, professorId, alunoUsuarioId);
        await DefinirRegraFixoMensalAsync(clientProfessor, professorId, matriculaId);

        var response = await client.PostAsJsonAsync(
            $"/alunos/matriculas/{matriculaId}/pagamentos",
            new IniciarPagamentoRequest(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1)));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var corpo = await response.Content.ReadFromJsonAsync<PagamentoIniciadoResponse>();
        corpo!.PagamentoId.Should().NotBeEmpty();
        corpo.UrlCheckout.Should().Be("https://checkout.mercadopago.com/pref-teste");
        corpo.Valor.Should().Be(300m);
    }

    [Fact]
    public async Task Post_Pagamentos_MatriculaDeOutroAluno_Devolve404()
    {
        var (client, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var (_, outroAlunoId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var clientProfessor = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorPersistidoAsync(_factory, "Professor Saldo");
        await ConectarMercadoPagoAsync(_factory, professorId);
        var matriculaId = await VincularMatriculaAsync(_factory, professorId, outroAlunoId);
        await DefinirRegraFixoMensalAsync(clientProfessor, professorId, matriculaId);

        var response = await client.PostAsJsonAsync(
            $"/alunos/matriculas/{matriculaId}/pagamentos",
            new IniciarPagamentoRequest(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1)));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_Pagamentos_ProfessorSemContaConectada_Devolve400ComTipoProfessorSemContaConectada()
    {
        var (client, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var clientProfessor = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorPersistidoAsync(_factory, "Professor Desconectado");
        var matriculaId = await VincularMatriculaAsync(_factory, professorId, alunoUsuarioId);
        await DefinirRegraFixoMensalAsync(clientProfessor, professorId, matriculaId);

        var response = await client.PostAsJsonAsync(
            $"/alunos/matriculas/{matriculaId}/pagamentos",
            new IniciarPagamentoRequest(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<PagamentoErrorResponse>();
        corpo!.Tipo.Should().Be("professor-sem-conta-conectada");
    }

    [Fact]
    public async Task Post_Pagamentos_SemValorDevidoNoPeriodo_Devolve400ComTipoSemValorDevido()
    {
        var (client, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var clientProfessor = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorPersistidoAsync(_factory, "Professor Sem Valor");
        await ConectarMercadoPagoAsync(_factory, professorId);
        var matriculaId = await VincularMatriculaAsync(_factory, professorId, alunoUsuarioId);
        // Sem regra de cobrança definida → SemRegraDefinida, Valor null → nada a pagar.

        var response = await client.PostAsJsonAsync(
            $"/alunos/matriculas/{matriculaId}/pagamentos",
            new IniciarPagamentoRequest(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<PagamentoErrorResponse>();
        corpo!.Tipo.Should().Be("sem-valor-devido");
    }
}
