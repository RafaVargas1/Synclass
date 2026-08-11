using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Synclass.Api.Middleware;

namespace Synclass.Api.Tests;

/// <summary>
/// Teste de fumaça: prova que o esqueleto da API sobe e responde, e que o
/// TrackIdMiddleware está no pipeline devolvendo o header de correlação.
/// Nenhum requisito funcional é exercitado aqui — ver docs/backlog.
/// </summary>
public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        // Desliga a migration automática de startup: este teste de fumaça
        // não depende de um Postgres real, só do pipeline HTTP.
        _factory = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("RunMigrationsOnStartup", "false"));
    }

    [Fact]
    public async Task Get_Health_ReturnsOkWithTrackIdHeader()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey(TrackIdMiddleware.HeaderName);
    }

    [Fact]
    public async Task Get_Health_PropagatesIncomingTrackId()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add(TrackIdMiddleware.HeaderName, "meu-track-id-de-teste");

        var response = await client.SendAsync(request);

        response.Headers.GetValues(TrackIdMiddleware.HeaderName)
            .Should().Contain("meu-track-id-de-teste");
    }
}
