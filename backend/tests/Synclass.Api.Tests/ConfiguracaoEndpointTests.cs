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
/// Teste de fumaça dos endpoints de configuração de agendamento (issue #7):
/// definir/consultar o modelo de agendamento do Professor. Usa EF Core
/// InMemory, mesmo padrão de <see cref="HorarioEndpointTests"/>.
/// </summary>
public sealed class ConfiguracaoEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ConfiguracaoEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task Put_ModeloAgendamento_ReturnsOk_QuandoAindaNaoDefinido()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/professores/{professorId}/configuracao/modelo-agendamento",
            new DefinirModeloAgendamentoRequest(ModeloAgendamento: 0));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<ConfiguracaoResponse>();
        corpo!.ModeloAgendamento.Should().Be(0);
    }

    [Fact]
    public async Task Put_ModeloAgendamento_ReturnsOk_QuandoJaDefinido()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);
        await client.PutAsJsonAsync(
            $"/professores/{professorId}/configuracao/modelo-agendamento",
            new DefinirModeloAgendamentoRequest(ModeloAgendamento: 1));

        var response = await client.PutAsJsonAsync(
            $"/professores/{professorId}/configuracao/modelo-agendamento",
            new DefinirModeloAgendamentoRequest(ModeloAgendamento: 0));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<ConfiguracaoResponse>();
        corpo!.ModeloAgendamento.Should().Be(0);
    }

    [Fact]
    public async Task Put_ModeloAgendamento_ReturnsBadRequest_QuandoModeloInvalido()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/professores/{professorId}/configuracao/modelo-agendamento",
            new DefinirModeloAgendamentoRequest(ModeloAgendamento: 99));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_Configuracao_ReturnsOk_QuandoDefinida()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);
        await client.PutAsJsonAsync(
            $"/professores/{professorId}/configuracao/modelo-agendamento",
            new DefinirModeloAgendamentoRequest(ModeloAgendamento: 2));

        var response = await client.GetAsync($"/professores/{professorId}/configuracao");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<ConfiguracaoResponse>();
        corpo!.ModeloAgendamento.Should().Be(2);
    }

    [Fact]
    public async Task Get_Configuracao_ReturnsNotFound_QuandoNaoDefinida()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);

        var response = await client.GetAsync($"/professores/{professorId}/configuracao");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_Horario_ReturnsBadRequest_QuandoProfessorNaoDefiniuModelo()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
