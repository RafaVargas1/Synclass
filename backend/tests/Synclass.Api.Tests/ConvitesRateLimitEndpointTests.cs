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
/// Teste de fumaça do rate limiting aplicado aos endpoints anônimos de
/// aceite de convite (issue #89). O limite configurado é 5 requisições por
/// 60 segundos por IP (<c>RateLimiting:ConvitesAnonimos</c>). Cobre o caso
/// de exceder o limite (429), o corpo/header da rejeição (issue #89) e o
/// caso de não o exceder (200/400, prova de que o limiter não interfere) —
/// o comportamento detalhado dos endpoints fica em
/// <see cref="ConvitesEndpointTests"/>.
/// </summary>
/// <remarks>
/// Cada teste cria uma instância própria de
/// <see cref="WebApplicationFactory{Program}"/> (não compartilhada via
/// <c>IClassFixture</c>): o contador do limiter vive em memória do processo
/// e persiste pela vida da instância — um teste que já excedeu o limite
/// vazaria a contagem para o próximo (ver
/// docs/specs/89-rate-limit-convites/implementation.md).
/// </remarks>
public sealed class ConvitesRateLimitEndpointTests
{
    private const int PermissoesPorJanela = 5;

    private WebApplicationFactory<Program> CriarFactory()
    {
        // Nome do banco em memória fixado por fábrica (não gerado dentro do
        // lambda de ConfigureServices): o WebApplicationFactory reconstrói o
        // host e reexecuta o callback mais de uma vez, e cada execução com um
        // Guid novo criaria um banco em memória diferente — o cadastro do
        // Professor não seria visível ao aceite do convite. Mesmo racional de
        // ConvitesEndpointTests, que fixa o nome no construtor da classe.
        var nomeDoBancoEmMemoria = $"rate-limit-{Guid.NewGuid()}";

        var factory = new WebApplicationFactory<Program>();
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RunMigrationsOnStartup", "false");
            builder.UseSetting("RateLimiting:ConvitesAnonimos:PermissoesPorJanela", PermissoesPorJanela.ToString());
            builder.UseSetting("RateLimiting:ConvitesAnonimos:JanelaEmSegundos", "60");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<SynclassDbContext>>();
                services.AddDbContext<SynclassDbContext>(options =>
                    options.UseInMemoryDatabase(nomeDoBancoEmMemoria));
            });
        });
    }

    private static async Task<HttpResponseMessage> FazerTentativasDeAceiteAsync(HttpClient client, string rota, int quantidade)
    {
        HttpResponseMessage resposta = null!;
        for (var i = 0; i < quantidade; i++)
        {
            resposta = await client.PostAsJsonAsync(
                rota, new AceitarConviteRequest("João Pedro", "11987654321"));
        }

        return resposta;
    }

    [Fact]
    public async Task Post_Aceite_ReturnsTooManyRequests_AoExcederLimiteNaMesmaJanela()
    {
        using var factory = CriarFactory();
        var client = factory.CreateClient();

        var resposta = await FazerTentativasDeAceiteAsync(client, "/convites/token-invalido/aceite", PermissoesPorJanela + 1);

        resposta.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Post_AceitePorCodigo_ReturnsTooManyRequests_AoExcederLimiteNaMesmaJanela()
    {
        using var factory = CriarFactory();
        var client = factory.CreateClient();

        var resposta = await FazerTentativasDeAceiteAsync(client, "/convites/codigo/99999/aceite", PermissoesPorJanela + 1);

        resposta.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Post_Aceite_TooManyRequests_UsaContratoConviteErrorResponseERetryAfter()
    {
        using var factory = CriarFactory();
        var client = factory.CreateClient();

        var resposta = await FazerTentativasDeAceiteAsync(client, "/convites/token-invalido/aceite", PermissoesPorJanela + 1);

        resposta.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var corpo = await resposta.Content.ReadFromJsonAsync<ConviteErrorResponse>();
        corpo!.Mensagem.Should().NotBeNullOrWhiteSpace();
        resposta.Headers.RetryAfter.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_AceitePorCodigo_TooManyRequests_UsaContratoConviteErrorResponseERetryAfter()
    {
        using var factory = CriarFactory();
        var client = factory.CreateClient();

        var resposta = await FazerTentativasDeAceiteAsync(client, "/convites/codigo/99999/aceite", PermissoesPorJanela + 1);

        resposta.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var corpo = await resposta.Content.ReadFromJsonAsync<ConviteErrorResponse>();
        corpo!.Mensagem.Should().NotBeNullOrWhiteSpace();
        resposta.Headers.RetryAfter.Should().NotBeNull();
    }

    [Fact]
    public async Task Post_Aceite_ContinuaNormal_QuandoDentroDoLimite()
    {
        using var factory = CriarFactory();
        var client = factory.CreateClient();

        var resposta = await client.PostAsJsonAsync(
            "/convites/token-invalido/aceite", new AceitarConviteRequest("João Pedro", "11987654321"));

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await resposta.Content.ReadFromJsonAsync<ConviteErrorResponse>();
        corpo!.Mensagem.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Post_AceitePorCodigo_ContinuaNormal_QuandoDentroDoLimite()
    {
        using var factory = CriarFactory();
        var client = factory.CreateClient();

        var resposta = await client.PostAsJsonAsync(
            "/convites/codigo/99999/aceite", new AceitarConviteRequest("João Pedro", "11987654321"));

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await resposta.Content.ReadFromJsonAsync<ConviteErrorResponse>();
        corpo!.Mensagem.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Post_Aceite_RetornaOk_QuandoDentroDoLimiteEConviteValido()
    {
        using var factory = CriarFactory();
        var convite = await GerarConviteAsync(factory);
        var client = factory.CreateClient();

        var resposta = await client.PostAsJsonAsync(
            $"/convites/{convite.Token}/aceite", new AceitarConviteRequest("João Pedro", "11987654321"));

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<AceitarConviteResponse>();
        corpo!.Nome.Should().Be("João Pedro");
        corpo.Papeis.Should().Contain("Aluno");
    }

    [Fact]
    public async Task Post_AceitePorCodigo_RetornaOk_QuandoDentroDoLimiteECodigoValido()
    {
        using var factory = CriarFactory();
        var convite = await GerarConviteAsync(factory);
        var client = factory.CreateClient();

        var resposta = await client.PostAsJsonAsync(
            $"/convites/codigo/{convite.Codigo}/aceite", new AceitarConviteRequest("João Pedro", "11987654321"));

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await resposta.Content.ReadFromJsonAsync<AceitarConviteResponse>();
        corpo!.Nome.Should().Be("João Pedro");
        corpo.Papeis.Should().Contain("Aluno");
    }

    private static async Task<GerarConviteResponse> GerarConviteAsync(WebApplicationFactory<Program> factory)
    {
        var clienteProfessor = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(factory);
        var cadastro = await clienteProfessor.PostAsJsonAsync(
            "/professores/cadastro", new CadastroUsuarioRequest("Professor Teste", "professor@exemplo.com"));
        var cadastroCorpo = await cadastro.Content.ReadFromJsonAsync<CadastroUsuarioResponse>();

        var gerar = await clienteProfessor.PostAsJsonAsync(
            $"/professores/{cadastroCorpo!.UsuarioId}/convites", new GerarConviteRequest("11987654321", null));
        var corpo = await gerar.Content.ReadFromJsonAsync<GerarConviteResponse>();
        return corpo!;
    }
}
