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
/// Teste de fumaça de <c>GET /professores/verificar-contato</c> (issue #27):
/// alimenta o campo Nome readonly do formulário de cadastro de Professor
/// (issue #1) quando o contato já pertence a uma identidade existente.
/// Endpoint público — roda antes de existir sessão, no fluxo de cadastro.
/// </summary>
public sealed class VerificarContatoEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public VerificarContatoEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task Get_VerificarContato_ContatoJaCadastrado_RetornaIdentidadeExistenteComNome()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/professores/cadastro", new CadastroProfessorRequest("Maria Silva", "maria@exemplo.com"));

        var response = await client.GetAsync("/professores/verificar-contato?contato=maria@exemplo.com");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<VerificarContatoResponse>();
        corpo!.IdentidadeExistente.Should().BeTrue();
        corpo.Nome.Should().Be("Maria Silva");
    }

    [Fact]
    public async Task Get_VerificarContato_ContatoNovo_RetornaIdentidadeInexistente()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/professores/verificar-contato?contato=novo@exemplo.com");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<VerificarContatoResponse>();
        corpo!.IdentidadeExistente.Should().BeFalse();
        corpo.Nome.Should().BeNull();
    }

    [Fact]
    public async Task Get_VerificarContato_ContatoAindaInvalido_NaoRejeitaERetornaIdentidadeInexistente()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/professores/verificar-contato?contato=incompleto");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<VerificarContatoResponse>();
        corpo!.IdentidadeExistente.Should().BeFalse();
    }
}
