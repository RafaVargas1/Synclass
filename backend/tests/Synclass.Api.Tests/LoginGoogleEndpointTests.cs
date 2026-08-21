using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synclass.Api.Controllers;
using Synclass.Api.Tests.Fakes;
using Synclass.Domain.Autenticacao;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Api.Tests;

/// <summary>
/// Teste de fumaça do fluxo de login via idToken do Google (issue #65):
/// <c>POST /auth/google</c>. Usa EF Core InMemory e um
/// <see cref="ValidadorDeIdTokenGoogleFixo"/> no lugar do SDK
/// <c>Google.Apis.Auth</c>, mesmo padrão de
/// <see cref="AutenticacaoEndpointTests"/>.
/// </summary>
public sealed class LoginGoogleEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LoginGoogleEndpointTests(WebApplicationFactory<Program> factory)
    {
        var nomeDoBancoEmMemoria = Guid.NewGuid().ToString();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RunMigrationsOnStartup", "false");
            builder.ConfigureServices(services =>
            {
                UseInMemoryDatabase(services, nomeDoBancoEmMemoria);
                UseValidadorGoogleFixo(services);
            });
        });
    }

    private static void UseInMemoryDatabase(IServiceCollection services, string nomeDoBanco)
    {
        services.RemoveAll<DbContextOptions<SynclassDbContext>>();
        services.AddDbContext<SynclassDbContext>(options =>
            options.UseInMemoryDatabase(nomeDoBanco));
    }

    private static void UseValidadorGoogleFixo(IServiceCollection services)
    {
        services.RemoveAll<IValidadorDeIdTokenGoogle>();
        services.AddSingleton<IValidadorDeIdTokenGoogle, ValidadorDeIdTokenGoogleFixo>();
    }

    private ValidadorDeIdTokenGoogleFixo Validador()
    {
        using var scope = _factory.Services.CreateScope();
        return (ValidadorDeIdTokenGoogleFixo)scope.ServiceProvider.GetRequiredService<IValidadorDeIdTokenGoogle>();
    }

    private async Task<HttpClient> CriarClienteComProfessorCadastradoAsync(string contato)
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/professores/cadastro", new CadastroUsuarioRequest("Maria Silva", contato));
        return client;
    }

    [Fact]
    public async Task Post_Google_ReturnsOkComToken_QuandoTokenValidoEUsuarioExiste()
    {
        await CriarClienteComProfessorCadastradoAsync("maria@exemplo.com");
        Validador().Resultado = new InformacoesIdTokenGoogle("maria@exemplo.com", EmailVerificado: true);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/google", new LoginGoogleRequest("idToken-ficticio"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<LoginGoogleResponse>();
        corpo!.Token.Should().NotBeNullOrWhiteSpace();
        corpo.UsuarioId.Should().NotBeNull();
        corpo.Papeis.Should().Contain("Professor");
        corpo.CadastroPendente.Should().BeFalse();
    }

    [Fact]
    public async Task Post_Google_ReturnsCadastroPendente_QuandoTokenValidoSemUsuario()
    {
        Validador().Resultado = new InformacoesIdTokenGoogle("naoexiste@exemplo.com", EmailVerificado: true);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/google", new LoginGoogleRequest("idToken-ficticio"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<LoginGoogleResponse>();
        corpo!.CadastroPendente.Should().BeTrue();
        corpo.Email.Should().Be("naoexiste@exemplo.com");
        corpo.Token.Should().BeNull();
    }

    [Fact]
    public async Task Post_Google_ReturnsBadRequest_QuandoEmailNaoVerificado()
    {
        Validador().Resultado = new InformacoesIdTokenGoogle("maria@exemplo.com", EmailVerificado: false);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/google", new LoginGoogleRequest("idToken-ficticio"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<AutenticacaoErrorResponse>();
        corpo!.Mensagem.Should().Contain("não foi verificado");
    }

    [Fact]
    public async Task Post_Google_ReturnsBadRequest_QuandoTokenInvalido()
    {
        Validador().Resultado = null;
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/google", new LoginGoogleRequest("idToken-invalido"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<AutenticacaoErrorResponse>();
        corpo!.Mensagem.Should().Contain("Token do Google");
    }
}
