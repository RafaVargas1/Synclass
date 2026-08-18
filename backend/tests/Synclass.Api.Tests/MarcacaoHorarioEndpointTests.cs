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
/// Teste de fumaça dos endpoints de marcação livre do Aluno (issue #9):
/// listar horários vagos e marcar. Mesmo padrão de
/// <see cref="AlocacaoHorarioEndpointTests"/> (issue #8), EF Core InMemory.
/// </summary>
public sealed class MarcacaoHorarioEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MarcacaoHorarioEndpointTests(WebApplicationFactory<Program> factory)
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

    private static async Task<Guid> CriarProfessorAsync(HttpClient client, int modeloAgendamento = 0)
    {
        var response = await client.PostAsJsonAsync(
            "/professores/cadastro",
            new CadastroProfessorRequest("Maria Silva", $"{Guid.NewGuid()}@exemplo.com"));
        var corpo = await response.Content.ReadFromJsonAsync<CadastroProfessorResponse>();
        var professorId = corpo!.UsuarioId;

        await client.PutAsJsonAsync(
            $"/professores/{professorId}/configuracao/modelo-agendamento",
            new DefinirModeloAgendamentoRequest(modeloAgendamento));

        return professorId;
    }

    private static async Task<Guid> CriarHorarioAsync(HttpClient client, Guid professorId, int limiteAlunos = 1)
    {
        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, LimiteAlunos: limiteAlunos));
        var corpo = await response.Content.ReadFromJsonAsync<HorarioResponse>();
        return corpo!.Id;
    }

    private static async Task<Guid> CriarMatriculaAsync(HttpClient client, Guid professorId)
    {
        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/alunos-provisorios",
            new CadastroAlunoProvisorioRequest("Aluno Teste", $"aluno-{Guid.NewGuid()}"));
        var corpo = await response.Content.ReadFromJsonAsync<CadastroAlunoProvisorioResponse>();
        return corpo!.MatriculaId;
    }

    [Fact]
    public async Task Post_Marcacao_ReturnsOk_QuandoModeloVagoComVaga()
    {
        var client = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(client);
        var horarioId = await CriarHorarioAsync(client, professorId);
        var matriculaId = await CriarMatriculaAsync(client, professorId);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios/{horarioId}/marcacoes",
            new CriarMarcacaoHorarioRequest(matriculaId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<AlocacaoHorarioResponse>();
        corpo!.HorarioId.Should().Be(horarioId);
        corpo.MatriculaId.Should().Be(matriculaId);
    }

    [Fact]
    public async Task Post_Marcacao_ReturnsBadRequest_QuandoModeloFixo()
    {
        var client = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(client, modeloAgendamento: 1);
        var horarioId = await CriarHorarioAsync(client, professorId);
        var matriculaId = await CriarMatriculaAsync(client, professorId);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios/{horarioId}/marcacoes",
            new CriarMarcacaoHorarioRequest(matriculaId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Marcacao_ReturnsBadRequest_QuandoHorarioLotado()
    {
        var client = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(client);
        var horarioId = await CriarHorarioAsync(client, professorId, limiteAlunos: 1);
        var primeiraMatriculaId = await CriarMatriculaAsync(client, professorId);
        await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios/{horarioId}/marcacoes",
            new CriarMarcacaoHorarioRequest(primeiraMatriculaId));
        var segundaMatriculaId = await CriarMatriculaAsync(client, professorId);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios/{horarioId}/marcacoes",
            new CriarMarcacaoHorarioRequest(segundaMatriculaId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Marcacao_ReturnsBadRequest_QuandoMatriculaNaoVinculada()
    {
        var client = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(client);
        var horarioId = await CriarHorarioAsync(client, professorId);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios/{horarioId}/marcacoes",
            new CriarMarcacaoHorarioRequest(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Marcacao_ReturnsBadRequest_QuandoAlunoJaMarcado()
    {
        var client = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(client);
        var horarioId = await CriarHorarioAsync(client, professorId, limiteAlunos: 2);
        var matriculaId = await CriarMatriculaAsync(client, professorId);
        await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios/{horarioId}/marcacoes",
            new CriarMarcacaoHorarioRequest(matriculaId));

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios/{horarioId}/marcacoes",
            new CriarMarcacaoHorarioRequest(matriculaId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Marcacao_ReturnsNotFound_QuandoHorarioInexistente()
    {
        var client = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(client);
        var matriculaId = await CriarMatriculaAsync(client, professorId);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios/{Guid.NewGuid()}/marcacoes",
            new CriarMarcacaoHorarioRequest(matriculaId));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Vagos_ListaHorariosDisponiveisNoModeloVago()
    {
        var client = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(client);
        var horarioId = await CriarHorarioAsync(client, professorId);
        var matriculaId = await CriarMatriculaAsync(client, professorId);

        var response = await client.GetAsync($"/professores/{professorId}/horarios/vagos?matriculaId={matriculaId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<HorarioVagoResponse>>();
        corpo.Should().ContainSingle(h => h.Id == horarioId && h.VagasRestantes == 1);
    }

    [Fact]
    public async Task Get_Vagos_ListaVaziaNoModeloFixo()
    {
        var client = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(client, modeloAgendamento: 1);
        await CriarHorarioAsync(client, professorId);
        var matriculaId = await CriarMatriculaAsync(client, professorId);

        var response = await client.GetAsync($"/professores/{professorId}/horarios/vagos?matriculaId={matriculaId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<HorarioVagoResponse>>();
        corpo.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_Vagos_ReturnsBadRequest_QuandoMatriculaNaoVinculada()
    {
        var client = _factory.CreateClient();
        var professorId = await CriarProfessorAsync(client);

        var response = await client.GetAsync($"/professores/{professorId}/horarios/vagos?matriculaId={Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
