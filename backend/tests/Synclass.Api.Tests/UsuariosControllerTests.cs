using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synclass.Api.Controllers;
using Synclass.Api.Tests.Fakes;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Api.Tests;

/// <summary>
/// Teste de fumaça dos endpoints de perfil (issue #27): consulta do próprio
/// nome e atualização, ambos derivando o <c>usuarioId</c> do token de sessão
/// (issue #23) — nunca de um parâmetro de rota.
/// </summary>
public sealed class UsuariosControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UsuariosControllerTests(WebApplicationFactory<Program> factory)
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
    public async Task Get_UsuariosMe_RetornaNomeEContatoAtual()
    {
        var (client, usuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var contatoCadastrado = await ContatoDoUsuarioAsync(usuarioId);

        var response = await client.GetAsync("/usuarios/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<UsuarioPerfilResponse>();
        corpo!.UsuarioId.Should().Be(usuarioId);
        corpo.Nome.Should().Be("Usuário de Teste");
        corpo.Contato.Should().Be(contatoCadastrado);
    }

    [Fact]
    public async Task Get_UsuariosMe_SemToken_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/usuarios/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Put_UsuariosMeNome_NomeValido_AtualizaNome()
    {
        var (client, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);

        var response = await client.PutAsJsonAsync("/usuarios/me/nome", new AtualizarNomeRequest("Maria Souza"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<UsuarioPerfilResponse>();
        corpo!.Nome.Should().Be("Maria Souza");

        var consulta = await client.GetAsync("/usuarios/me");
        var corpoConsulta = await consulta.Content.ReadFromJsonAsync<UsuarioPerfilResponse>();
        corpoConsulta!.Nome.Should().Be("Maria Souza");
    }

    [Fact]
    public async Task Put_UsuariosMeNome_NomeVazio_RetornaBadRequest()
    {
        var (client, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);

        var response = await client.PutAsJsonAsync("/usuarios/me/nome", new AtualizarNomeRequest("   "));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_UsuariosMeNome_SemToken_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/usuarios/me/nome", new AtualizarNomeRequest("Maria Souza"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string> ContatoDoUsuarioAsync(Guid usuarioId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        var usuario = await dbContext.Usuarios.SingleAsync(u => u.Id == usuarioId);
        return usuario.Contato;
    }
}
