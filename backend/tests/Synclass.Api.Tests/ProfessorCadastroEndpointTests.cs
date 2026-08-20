using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synclass.Api.Controllers;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Api.Tests;

/// <summary>
/// Teste de fumaça do endpoint de cadastro de Professor (issue #1): sucesso,
/// contato inválido e contato duplicado. Usa EF Core InMemory (banco isolado
/// por teste) no lugar de um Postgres real — a migration real é validada à
/// parte, gerando o arquivo estaticamente (ver backend/README.md#migrations).
/// </summary>
public sealed class ProfessorCadastroEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProfessorCadastroEndpointTests(WebApplicationFactory<Program> factory)
    {
        // Um nome de banco fixo por instância de teste: o nome precisa ser
        // capturado antes da lambda de configuração, pois AddDbContext a
        // reexecuta a cada escopo de DI (uma por requisição HTTP) — gerar o
        // Guid dentro da lambda criaria um banco em memória novo por request.
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
    public async Task Post_Cadastro_ReturnsOk_QuandoDadosValidos()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/professores/cadastro",
            new CadastroUsuarioRequest("Maria Silva", "maria@exemplo.com"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<CadastroUsuarioResponse>();
        corpo!.Nome.Should().Be("Maria Silva");
    }

    [Fact]
    public async Task Post_Cadastro_ReturnsBadRequest_QuandoContatoInvalido()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/professores/cadastro",
            new CadastroUsuarioRequest("Maria Silva", "nao-e-um-contato-valido"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<CadastroUsuarioErrorResponse>();
        corpo!.Mensagem.Should().Contain("Contato inválido");
    }

    [Fact]
    public async Task Post_Cadastro_ReturnsBadRequest_QuandoContatoJaCadastradoComoProfessor()
    {
        var client = _factory.CreateClient();
        var request = new CadastroUsuarioRequest("Maria Silva", "duplicada@exemplo.com");
        await client.PostAsJsonAsync("/professores/cadastro", request);

        var response = await client.PostAsJsonAsync("/professores/cadastro", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<CadastroUsuarioErrorResponse>();
        corpo!.Mensagem.Should().Contain("Professor");
    }
}
