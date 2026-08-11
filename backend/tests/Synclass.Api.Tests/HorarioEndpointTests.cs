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
/// Teste de fumaça dos endpoints de horários disponíveis (issue #6):
/// criar, listar e remover. Usa EF Core InMemory, mesmo padrão de
/// <see cref="ProfessorCadastroEndpointTests"/>.
/// </summary>
public sealed class HorarioEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HorarioEndpointTests(WebApplicationFactory<Program> factory)
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

    private static async Task<Guid> CriarProfessorAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/professores/cadastro",
            new CadastroProfessorRequest("Maria Silva", $"{Guid.NewGuid()}@exemplo.com"));
        var corpo = await response.Content.ReadFromJsonAsync<CadastroProfessorResponse>();
        return corpo!.UsuarioId;
    }

    [Fact]
    public async Task Post_Horario_ReturnsOk_QuandoDadosValidos()
    {
        var client = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<HorarioResponse>();
        corpo!.DuracaoMinutos.Should().Be(60);
    }

    [Fact]
    public async Task Post_Horario_ReturnsBadRequest_QuandoDuracaoInvalida()
    {
        var client = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 0));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Horario_ReturnsBadRequest_QuandoConflitaComHorarioExistente()
    {
        var client = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(client);
        await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60));

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 30), DuracaoMinutos: 60));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<HorarioErrorResponse>();
        corpo!.Mensagem.Should().Contain("conflita");
    }

    [Fact]
    public async Task Get_Horarios_ListaHorariosCadastrados()
    {
        var client = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(client);
        await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60));

        var response = await client.GetAsync($"/professores/{professorId}/horarios");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<HorarioResponse>>();
        corpo.Should().ContainSingle();
    }

    [Fact]
    public async Task Delete_Horario_ReturnsNoContent_QuandoRemovido()
    {
        var client = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(client);
        var criado = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60));
        var horario = await criado.Content.ReadFromJsonAsync<HorarioResponse>();

        var response = await client.DeleteAsync($"/professores/{professorId}/horarios/{horario!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_Horario_ReturnsNotFound_QuandoHorarioNaoExiste()
    {
        var client = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(client);

        var response = await client.DeleteAsync($"/professores/{professorId}/horarios/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
