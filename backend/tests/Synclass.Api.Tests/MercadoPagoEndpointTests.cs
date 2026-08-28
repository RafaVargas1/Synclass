using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synclass.Api.Tests.Fakes;
using Synclass.Domain.Pagamentos;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Api.Tests;

/// <summary>
/// Teste de fumaça dos endpoints de integração do Professor com o Mercado
/// Pago (issue #203): <c>GET /professores/mercado-pago/conectar</c> e <c>GET
/// /professores/mercado-pago/callback</c>. Usa EF Core InMemory e um
/// <see cref="FakeClienteOAuthMercadoPago"/> no lugar do cliente HTTP real
/// (não chamar a Api do Mercado Pago num teste), mesmo padrão de
/// <see cref="LoginGoogleEndpointTests"/>.
/// </summary>
public sealed class MercadoPagoEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MercadoPagoEndpointTests(WebApplicationFactory<Program> factory)
    {
        var nomeDoBancoEmMemoria = Guid.NewGuid().ToString();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RunMigrationsOnStartup", "false");
            builder.ConfigureServices(services =>
            {
                UseInMemoryDatabase(services, nomeDoBancoEmMemoria);
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

    private static void UsarClienteOAuthFixo(IServiceCollection services)
    {
        services.RemoveAll<IClienteOAuthMercadoPago>();
        services.AddSingleton<IClienteOAuthMercadoPago>(new FakeClienteOAuthMercadoPago());
    }

    private static async Task<ConexaoMercadoPago?> BuscarConexaoAsync(WebApplicationFactory<Program> factory, Guid professorId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        return await db.ConexoesMercadoPago.FirstOrDefaultAsync(c => c.ProfessorId == professorId);
    }

    [Fact]
    public async Task Get_Conectar_ReturnsOkComUrl_QuandoProfessorAutenticado()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);

        var response = await client.GetAsync("/professores/mercado-pago/conectar");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<MercadoPagoConectarResponse>();
        corpo!.Url.Should().NotBeNullOrWhiteSpace();
        // A conexão foi persistida (registro criado no início do fluxo), com
        // o Professor vinculado.
        var conexao = await BuscarConexaoAsync(_factory, professorId);
        conexao.Should().NotBeNull();
        conexao!.ProfessorId.Should().Be(professorId);
    }

    [Fact]
    public async Task Get_Conectar_ReturnsNotFound_QuandoProfessorInexistente()
    {
        // Cliente sem professor persistido: o token assina um Usuario que não
        // existe no banco — ConectarAsync lança UsuarioNaoEncontradoException
        // e o controller devolve 404 (ver implementation.md#exceção-por-professor-não-encontrado).
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);

        var response = await client.GetAsync("/professores/mercado-pago/conectar");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Callback_ReturnsOkEPersisteConexao_QuandoStateValido()
    {
        // Inicia um fluxo real via /conectar para persistir um state válido,
        // depois exercita o callback com esse state + um code fictício.
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var conectar = await client.GetAsync("/professores/mercado-pago/conectar");
        var corpoConectar = await conectar.Content.ReadFromJsonAsync<MercadoPagoConectarResponse>();
        var state = ExtrairStateDaUrl(corpoConectar!.Url);

        var clientAnonimo = _factory.CreateClient();
        var response = await clientAnonimo.GetAsync(
            $"/professores/mercado-pago/callback?code=code-do-mp&state={state}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Credenciais e collector gravados após a troca de code.
        var conexao = await BuscarConexaoAsync(_factory, professorId);
        conexao.Should().NotBeNull();
        conexao!.CollectorId.Should().Be("collector-teste");
        conexao.AccessToken.Should().Be("access-teste");
        // Fluxo pendente encerrado: state limpo, não reutilizável.
        conexao.State.Should().BeNull();
    }

    private static string ExtrairStateDaUrl(string url)
    {
        var uri = new Uri(url);
        var parametros = QueryHelpers.ParseQuery(uri.Query);
        return parametros["state"]!;
    }
}

public sealed record MercadoPagoConectarResponse(string Url);
