using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synclass.Api.Controllers;
using Synclass.Api.Tests.Fakes;
using Synclass.Domain.Common;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Api.Tests;

/// <summary>
/// Teste de fumaça dos endpoints de convite (issue #2): geração,
/// Professor/matrícula inválidos, Aluno já vinculado, aceite e expiração.
/// Usa EF Core InMemory e um <see cref="AdvanceableClock"/> (no lugar do
/// relógio real) para simular expiração — mesmo padrão de
/// <see cref="AlunoProvisorioCadastroEndpointTests"/>.
/// </summary>
public sealed class ConvitesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly AdvanceableClock _clock = new();

    public ConvitesEndpointTests(WebApplicationFactory<Program> factory)
    {
        var nomeDoBancoEmMemoria = Guid.NewGuid().ToString();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("RunMigrationsOnStartup", "false");
            builder.ConfigureServices(services =>
            {
                UseInMemoryDatabase(services, nomeDoBancoEmMemoria);
                services.RemoveAll<IClock>();
                services.AddSingleton<IClock>(_clock);
            });
        });
    }

    private static void UseInMemoryDatabase(IServiceCollection services, string nomeDoBanco)
    {
        services.RemoveAll<DbContextOptions<SynclassDbContext>>();
        services.AddDbContext<SynclassDbContext>(options =>
            options.UseInMemoryDatabase(nomeDoBanco));
    }

    [Fact]
    public async Task Post_Convites_ReturnsOk_QuandoContatoValido()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CadastrarProfessorAsync(client, "professor1@exemplo.com");

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/convites", new GerarConviteRequest("11987654321", null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<GerarConviteResponse>();
        corpo!.Token.Should().NotBeNullOrWhiteSpace();
        corpo.ExpiraEm.Should().BeAfter(_clock.UtcNow);
    }

    [Fact]
    public async Task Post_Convites_ReturnsNotFound_QuandoProfessorIdInexistente()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorIdInexistente = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorIdInexistente}/convites", new GerarConviteRequest("11987654321", null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var corpo = await response.Content.ReadFromJsonAsync<ConviteErrorResponse>();
        corpo!.Mensagem.Should().Contain(professorIdInexistente.ToString());
    }

    [Fact]
    public async Task Post_Convites_ReturnsBadRequest_QuandoAlunoJaVinculado()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CadastrarProfessorAsync(client, "professor2@exemplo.com");
        var conviteAceito = await GerarEAceitarConviteAsync(client, professorId, "11987654321");
        conviteAceito.Should().NotBeNull();

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/convites", new GerarConviteRequest("11987654321", null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Aceite_ReturnsOk_QuandoTokenValidoEContatoCorrespondente()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CadastrarProfessorAsync(client, "professor3@exemplo.com");
        var convite = await GerarConviteAsync(client, professorId, "11987654321");

        var response = await client.PostAsJsonAsync(
            $"/convites/{convite.Token}/aceite", new AceitarConviteRequest("João Pedro", "11987654321"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<AceitarConviteResponse>();
        corpo!.Nome.Should().Be("João Pedro");
        corpo.Papeis.Should().Contain("Aluno");
    }

    [Fact]
    public async Task Post_Aceite_ReturnsBadRequest_QuandoTokenExpirado()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CadastrarProfessorAsync(client, "professor4@exemplo.com");
        var convite = await GerarConviteAsync(client, professorId, "11987654321");
        _clock.UtcNow = _clock.UtcNow.AddDays(8);

        var response = await client.PostAsJsonAsync(
            $"/convites/{convite.Token}/aceite", new AceitarConviteRequest("João Pedro", "11987654321"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<ConviteErrorResponse>();
        corpo!.Mensagem.Should().Contain("expirou");
    }

    private static async Task<GerarConviteResponse> GerarConviteAsync(HttpClient client, Guid professorId, string contato)
    {
        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/convites", new GerarConviteRequest(contato, null));
        var corpo = await response.Content.ReadFromJsonAsync<GerarConviteResponse>();
        return corpo!;
    }

    private static async Task<AceitarConviteResponse> GerarEAceitarConviteAsync(HttpClient client, Guid professorId, string contato)
    {
        var convite = await GerarConviteAsync(client, professorId, contato);
        var response = await client.PostAsJsonAsync(
            $"/convites/{convite.Token}/aceite", new AceitarConviteRequest("João Pedro", contato));
        var corpo = await response.Content.ReadFromJsonAsync<AceitarConviteResponse>();
        return corpo!;
    }

    private static async Task<Guid> CadastrarProfessorAsync(HttpClient client, string contato)
    {
        var response = await client.PostAsJsonAsync(
            "/professores/cadastro", new CadastroUsuarioRequest("Professor Teste", contato));
        var corpo = await response.Content.ReadFromJsonAsync<CadastroUsuarioResponse>();
        return corpo!.UsuarioId;
    }
}
