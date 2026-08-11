using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Api.Tests;

/// <summary>
/// Trava o comportamento de CORS configurado em Program.cs: sem ele, o
/// navegador bloqueia o preflight do frontend web (porta 8081) contra a Api
/// em outra origem, e o fetch falha com um erro de conexão indistinguível de
/// um problema de rede real (ver Cors:AllowedOrigins em
/// appsettings.Development.json).
/// </summary>
public sealed class CorsConfiguracaoTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CorsConfiguracaoTests(WebApplicationFactory<Program> factory)
    {
        var nomeDoBancoEmMemoria = Guid.NewGuid().ToString();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RunMigrationsOnStartup", "false");
            builder.ConfigureServices(services => UseInMemoryDatabase(services, nomeDoBancoEmMemoria));
        });
    }

    private static void UseInMemoryDatabase(IServiceCollection services, string nomeDoBanco)
    {
        services.RemoveAll<DbContextOptions<SynclassDbContext>>();
        services.AddDbContext<SynclassDbContext>(options =>
            options.UseInMemoryDatabase(nomeDoBanco));
    }

    [Fact]
    public async Task Preflight_RetornaAccessControlAllowOrigin_QuandoOrigemDoFrontendPermitida()
    {
        var client = _factory.CreateClient();
        var preflight = MontarPreflight("http://localhost:8081");

        var response = await client.SendAsync(preflight);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle("http://localhost:8081");
    }

    [Fact]
    public async Task Preflight_NaoRetornaAccessControlAllowOrigin_QuandoOrigemNaoPermitida()
    {
        var client = _factory.CreateClient();
        var preflight = MontarPreflight("http://origem-nao-permitida.example.com");

        var response = await client.SendAsync(preflight);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    private static HttpRequestMessage MontarPreflight(string origem)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/professores/cadastro");
        request.Headers.Add("Origin", origem);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        return request;
    }
}
