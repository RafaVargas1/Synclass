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
/// Teste de fumaça da autorização por papel (issue #4): endpoints
/// Professor-only recusam requisição sem token (401) e com token sem o
/// papel exigido (403), e aceitam com o papel presente. Usa EF Core
/// InMemory, mesmo padrão de <see cref="HorarioEndpointTests"/>.
/// </summary>
public sealed class AutorizacaoEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AutorizacaoEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task Post_Horarios_ReturnsUnauthorized_SemHeaderAuthorization()
    {
        var client = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: 0));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_Horarios_ReturnsForbidden_ComTokenSemPapelProfessor()
    {
        var clientSetup = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(clientSetup);
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoAluno(_factory);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: 0));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_Horarios_AceitaRequisicao_ComTokenContendoPapelProfessor()
    {
        var clientSetup = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(clientSetup);
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: 0));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_HorariosVagos_ReturnsUnauthorized_SemHeaderAuthorization()
    {
        var clientSetup = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(clientSetup);

        var response = await clientSetup.GetAsync($"/professores/{professorId}/horarios/vagos");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_HorariosVagos_ReturnsForbidden_ComTokenSemPapelAluno()
    {
        var clientSetup = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(clientSetup);
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);

        var response = await client.GetAsync($"/professores/{professorId}/horarios/vagos");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Com papel Aluno mas sem vínculo (Matrícula) com este Professor, a
    /// requisição é aceita pela autorização por papel (issue #4) e rejeitada
    /// só depois, pela checagem de posse da issue #23 — 404, não 400: o
    /// Aluno deste teste nunca foi persistido/vinculado, então não há nada
    /// que ele possa corrigir reenviando os mesmos dados.
    /// </summary>
    [Fact]
    public async Task Get_HorariosVagos_AceitaAutorizacaoPorPapel_ComTokenContendoPapelAluno()
    {
        var clientSetup = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(clientSetup);
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoAluno(_factory);

        var response = await client.GetAsync($"/professores/{professorId}/horarios/vagos");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<Guid> CriarProfessorAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/professores/cadastro",
            new CadastroProfessorRequest("Maria Silva", $"{Guid.NewGuid()}@exemplo.com"));
        var corpo = await response.Content.ReadFromJsonAsync<CadastroProfessorResponse>();
        var professorId = corpo!.UsuarioId;

        var clienteProfessor = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        await clienteProfessor.PutAsJsonAsync(
            $"/professores/{professorId}/configuracao/modelo-agendamento",
            new DefinirModeloAgendamentoRequest(0));

        return professorId;
    }
}
