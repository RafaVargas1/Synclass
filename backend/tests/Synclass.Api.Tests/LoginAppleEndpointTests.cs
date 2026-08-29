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
/// Teste de fumaça do fluxo de login via idToken da Apple (issue #212):
/// <c>POST /auth/apple</c>. Usa EF Core InMemory e um
/// <see cref="ValidadorDeIdTokenAppleFixo"/> no lugar da validação
/// criptográfica real (JWT + JWKS), mesmo padrão de
/// <see cref="LoginGoogleEndpointTests"/>.
/// </summary>
public sealed class LoginAppleEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LoginAppleEndpointTests(WebApplicationFactory<Program> factory)
    {
        var nomeDoBancoEmMemoria = Guid.NewGuid().ToString();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RunMigrationsOnStartup", "false");
            builder.ConfigureServices(services =>
            {
                UseInMemoryDatabase(services, nomeDoBancoEmMemoria);
                UseValidadorAppleFixo(services);
            });
        });
    }

    private static void UseInMemoryDatabase(IServiceCollection services, string nomeDoBanco)
    {
        services.RemoveAll<DbContextOptions<SynclassDbContext>>();
        services.AddDbContext<SynclassDbContext>(options =>
            options.UseInMemoryDatabase(nomeDoBanco));
    }

    private static void UseValidadorAppleFixo(IServiceCollection services)
    {
        services.RemoveAll<IValidadorDeIdTokenApple>();
        services.AddSingleton<IValidadorDeIdTokenApple, ValidadorDeIdTokenAppleFixo>();
    }

    private ValidadorDeIdTokenAppleFixo Validador()
    {
        using var scope = _factory.Services.CreateScope();
        return (ValidadorDeIdTokenAppleFixo)scope.ServiceProvider.GetRequiredService<IValidadorDeIdTokenApple>();
    }

    private async Task<HttpClient> CriarClienteComProfessorCadastradoAsync(string contato)
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/professores/cadastro", new CadastroUsuarioRequest("Maria Silva", contato));
        return client;
    }

    [Fact]
    public async Task Post_Apple_ReturnsOkComToken_QuandoTokenValidoEUsuarioExiste()
    {
        await CriarClienteComProfessorCadastradoAsync("maria@exemplo.com");
        Validador().Resultado = new InformacoesIdTokenApple("maria@exemplo.com", EmailVerificado: true);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/apple", new LoginAppleRequest("idToken-ficticio"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<LoginAppleResponse>();
        corpo!.Token.Should().NotBeNullOrWhiteSpace();
        corpo.UsuarioId.Should().NotBeNull();
        corpo.Papeis.Should().Contain("Professor");
        corpo.CadastroPendente.Should().BeFalse();
    }

    [Fact]
    public async Task Post_Apple_ReturnsCadastroPendente_QuandoTokenValidoSemUsuario()
    {
        Validador().Resultado = new InformacoesIdTokenApple("naoexiste@exemplo.com", EmailVerificado: true);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/apple", new LoginAppleRequest("idToken-ficticio"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<LoginAppleResponse>();
        corpo!.CadastroPendente.Should().BeTrue();
        corpo.Email.Should().Be("naoexiste@exemplo.com");
        corpo.Token.Should().BeNull();
    }

    [Fact]
    public async Task Post_Apple_ReturnsBadRequest_QuandoEmailNaoVerificado()
    {
        Validador().Resultado = new InformacoesIdTokenApple("maria@exemplo.com", EmailVerificado: false);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/apple", new LoginAppleRequest("idToken-ficticio"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<AutenticacaoErrorResponse>();
        corpo!.Mensagem.Should().Contain("não foi verificado");
    }

    [Fact]
    public async Task Post_Apple_ReturnsBadRequest_QuandoTokenInvalido()
    {
        Validador().Resultado = null;
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/apple", new LoginAppleRequest("idToken-invalido"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<AutenticacaoErrorResponse>();
        corpo!.Mensagem.Should().Contain("Token da Apple");
    }
}
