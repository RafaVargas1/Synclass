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
/// Teste de fumaça do fluxo completo de login por OTP (issue #18): solicitar
/// código → confirmar → receber token, e as rejeições previstas nos
/// Critérios de aceite. Usa EF Core InMemory, mesmo padrão de
/// <see cref="ProfessorCadastroEndpointTests"/>. Desde a entrega via
/// WhatsApp (issue #193), roda com <c>AssinaturaDigital:ModoDev=true</c>
/// para que <c>NotificadorDeLog</c> seja registrado — o teste não deve
/// depender de rede real do provedor.
/// </summary>
public sealed class AutenticacaoEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AutenticacaoEndpointTests(WebApplicationFactory<Program> factory)
    {
        var nomeDoBancoEmMemoria = Guid.NewGuid().ToString();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RunMigrationsOnStartup", "false");
            builder.UseSetting("AssinaturaDigital:ModoDev", "true");
            builder.ConfigureServices(services =>
            {
                UseInMemoryDatabase(services, nomeDoBancoEmMemoria);
                UseCodigoOtpFixo(services);
            });
        });
    }

    private static void UseInMemoryDatabase(IServiceCollection services, string nomeDoBanco)
    {
        services.RemoveAll<DbContextOptions<SynclassDbContext>>();
        services.AddDbContext<SynclassDbContext>(options =>
            options.UseInMemoryDatabase(nomeDoBanco));
    }

    private static void UseCodigoOtpFixo(IServiceCollection services)
    {
        services.RemoveAll<IGeradorDeCodigoOtp>();
        services.AddSingleton<IGeradorDeCodigoOtp, CodigoFixoGeradorDeCodigoOtp>();
    }

    private async Task<HttpClient> CriarClienteComProfessorCadastradoAsync(string contato)
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/professores/cadastro", new CadastroUsuarioRequest("Maria Silva", contato));
        return client;
    }

    [Fact]
    public async Task Post_Codigo_ReturnsOk_QuandoContatoTemIdentidadePlena()
    {
        var client = await CriarClienteComProfessorCadastradoAsync("maria@exemplo.com");

        var response = await client.PostAsJsonAsync("/auth/codigo", new SolicitarCodigoRequest("maria@exemplo.com"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<SolicitarCodigoResponse>();
        corpo!.Enviado.Should().BeTrue();
    }

    [Fact]
    public async Task Post_Codigo_ReturnsBadRequest_QuandoContatoSemIdentidadePlena()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/codigo", new SolicitarCodigoRequest("naoexiste@exemplo.com"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<AutenticacaoErrorResponse>();
        corpo!.Mensagem.Should().Contain("Nenhuma conta encontrada");
    }

    [Fact]
    public async Task Post_Codigo_ContatoInvalido_RetornaBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/codigo", new SolicitarCodigoRequest("123"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<AutenticacaoErrorResponse>();
        corpo!.Mensagem.Should().Contain("Contato inválido");
    }

    [Fact]
    public async Task Post_Codigo_ProvedorFalha_RetornaBadGatewaySemDetalheTecnico()
    {
        var factoryComFalha = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<INotificador>();
                services.AddScoped<INotificador>(_ => new NotificadorQueFalha());
            });
        });
        var client = factoryComFalha.CreateClient();
        await client.PostAsJsonAsync("/professores/cadastro", new CadastroUsuarioRequest("Maria Silva", "11999999999"));

        var response = await client.PostAsJsonAsync("/auth/codigo", new SolicitarCodigoRequest("11999999999"));

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        var corpo = await response.Content.ReadFromJsonAsync<AutenticacaoErrorResponse>();
        corpo!.Mensagem.Should().Contain("Não foi possível enviar o código");
        corpo!.Mensagem.Should().NotContain("Twilio");
        corpo!.Mensagem.Should().NotContain("HttpRequest");
    }

    [Fact]
    public async Task Post_Confirmacao_ReturnsOkComToken_QuandoCodigoCorreto()
    {
        var client = await CriarClienteComProfessorCadastradoAsync("maria@exemplo.com");
        await client.PostAsJsonAsync("/auth/codigo", new SolicitarCodigoRequest("maria@exemplo.com"));

        var response = await client.PostAsJsonAsync(
            "/auth/confirmacao",
            new ConfirmarCodigoRequest("maria@exemplo.com", CodigoFixoGeradorDeCodigoOtp.Codigo));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<ConfirmarCodigoResponse>();
        corpo!.Token.Should().NotBeNullOrWhiteSpace();
        corpo.Papeis.Should().Contain("Professor");
    }

    [Fact]
    public async Task Post_Confirmacao_ReturnsBadRequest_QuandoCodigoIncorreto()
    {
        var client = await CriarClienteComProfessorCadastradoAsync("maria@exemplo.com");
        await client.PostAsJsonAsync("/auth/codigo", new SolicitarCodigoRequest("maria@exemplo.com"));

        var response = await client.PostAsJsonAsync(
            "/auth/confirmacao",
            new ConfirmarCodigoRequest("maria@exemplo.com", "000000"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<AutenticacaoErrorResponse>();
        corpo!.Mensagem.Should().Contain("Código inválido");
    }

    [Fact]
    public async Task Post_Confirmacao_ReturnsBadRequest_QuandoCodigoJaFoiUsado()
    {
        var client = await CriarClienteComProfessorCadastradoAsync("maria@exemplo.com");
        await client.PostAsJsonAsync("/auth/codigo", new SolicitarCodigoRequest("maria@exemplo.com"));
        var request = new ConfirmarCodigoRequest("maria@exemplo.com", CodigoFixoGeradorDeCodigoOtp.Codigo);
        await client.PostAsJsonAsync("/auth/confirmacao", request);

        var response = await client.PostAsJsonAsync("/auth/confirmacao", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Confirmacao_ReturnsBadRequest_QuandoExcedeuLimiteDeTentativasErradas()
    {
        var client = await CriarClienteComProfessorCadastradoAsync("maria@exemplo.com");
        await client.PostAsJsonAsync("/auth/codigo", new SolicitarCodigoRequest("maria@exemplo.com"));
        var requestErrado = new ConfirmarCodigoRequest("maria@exemplo.com", "000000");

        for (var tentativa = 0; tentativa < CodigoOtp.MaxTentativasFalhas; tentativa++)
        {
            await client.PostAsJsonAsync("/auth/confirmacao", requestErrado);
        }

        var response = await client.PostAsJsonAsync(
            "/auth/confirmacao",
            new ConfirmarCodigoRequest("maria@exemplo.com", CodigoFixoGeradorDeCodigoOtp.Codigo));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<AutenticacaoErrorResponse>();
        corpo!.Mensagem.Should().Contain("máximo de tentativas");
    }
}

/// <summary>
/// Implementação de falha de <see cref="INotificador"/> para o teste de
/// fumaça da issue #193: lança <see cref="OtpEnvioException"/> com motivo
/// amigável, simulando a rejeição do provedor WhatsApp — sem rede real.
/// </summary>
public sealed class NotificadorQueFalha : INotificador
{
    public Task EnviarCodigoOtpAsync(string contatoNormalizado, string codigo, CancellationToken cancellationToken)
    {
        throw new OtpEnvioException("Não foi possível enviar o código. Tente novamente em instantes.");
    }
}
