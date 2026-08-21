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
/// Teste de fumaça do rate limiting aplicado aos endpoints anônimos de
/// aceite de convite (issue #89). O limite configurado é 5 requisições por
/// 60 segundos por IP (<c>RateLimiting:ConvitesAnonimos</c>). Cobre apenas
/// o caso de exceder o limite — o comportamento normal (200/400) já é
/// coberto por <see cref="ConvitesEndpointTests"/>.
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
        var factory = new WebApplicationFactory<Program>();
        return factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RunMigrationsOnStartup", "false");
            builder.UseSetting("RateLimiting:ConvitesAnonimos:PermissoesPorJanela", PermissoesPorJanela.ToString());
            builder.UseSetting("RateLimiting:ConvitesAnonimos:JanelaEmSegundos", "60");
            builder.ConfigureServices(services => UseInMemoryDatabase(services));
        });
    }

    private static void UseInMemoryDatabase(IServiceCollection services)
    {
        services.RemoveAll<DbContextOptions<SynclassDbContext>>();
        services.AddDbContext<SynclassDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
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
}
