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
/// Teste de fumaça dos endpoints de alocação de Aluno a horário (issue #8):
/// criar, listar e desfazer. Usa EF Core InMemory, mesmo padrão de
/// <see cref="HorarioEndpointTests"/>.
/// </summary>
public sealed class AlocacaoHorarioEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AlocacaoHorarioEndpointTests(WebApplicationFactory<Program> factory)
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

    /// <summary>
    /// Cria um Professor com o modelo de agendamento informado (0 = Vago,
    /// 1 = Fixo, 2 = Híbrido — ver <c>ModeloAgendamento</c>).
    /// </summary>
    private static async Task<Guid> CriarProfessorAsync(HttpClient client, int modeloAgendamento = 1)
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
    public async Task Post_Alocacao_ReturnsOk_QuandoDadosValidos()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);
        var horarioId = await CriarHorarioAsync(client, professorId);
        var matriculaId = await CriarMatriculaAsync(client, professorId);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios/{horarioId}/alocacoes",
            new CriarAlocacaoHorarioRequest(matriculaId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<AlocacaoHorarioResponse>();
        corpo!.HorarioId.Should().Be(horarioId);
        corpo.MatriculaId.Should().Be(matriculaId);
    }

    [Fact]
    public async Task Post_Alocacao_ReturnsBadRequest_QuandoModeloVago()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client, modeloAgendamento: 0);
        var horarioId = await CriarHorarioAsync(client, professorId);
        var matriculaId = await CriarMatriculaAsync(client, professorId);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios/{horarioId}/alocacoes",
            new CriarAlocacaoHorarioRequest(matriculaId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Alocacao_ReturnsBadRequest_QuandoHorarioLotado()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);
        var horarioId = await CriarHorarioAsync(client, professorId, limiteAlunos: 1);
        var primeiraMatriculaId = await CriarMatriculaAsync(client, professorId);
        await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios/{horarioId}/alocacoes",
            new CriarAlocacaoHorarioRequest(primeiraMatriculaId));
        var segundaMatriculaId = await CriarMatriculaAsync(client, professorId);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios/{horarioId}/alocacoes",
            new CriarAlocacaoHorarioRequest(segundaMatriculaId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Alocacao_ReturnsBadRequest_QuandoMatriculaNaoVinculada()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);
        var horarioId = await CriarHorarioAsync(client, professorId);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios/{horarioId}/alocacoes",
            new CriarAlocacaoHorarioRequest(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Alocacao_ReturnsNotFound_QuandoHorarioInexistente()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);
        var matriculaId = await CriarMatriculaAsync(client, professorId);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios/{Guid.NewGuid()}/alocacoes",
            new CriarAlocacaoHorarioRequest(matriculaId));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Alocacoes_ListaAlocacoesDoHorario()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);
        var horarioId = await CriarHorarioAsync(client, professorId, limiteAlunos: 2);
        var matriculaId = await CriarMatriculaAsync(client, professorId);
        await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios/{horarioId}/alocacoes",
            new CriarAlocacaoHorarioRequest(matriculaId));

        var response = await client.GetAsync($"/professores/{professorId}/horarios/{horarioId}/alocacoes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<AlocacaoHorarioResponse>>();
        corpo.Should().ContainSingle(a => a.MatriculaId == matriculaId);
    }

    [Fact]
    public async Task Delete_Alocacao_ReturnsNoContent_QuandoRemovida()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);
        var horarioId = await CriarHorarioAsync(client, professorId);
        var matriculaId = await CriarMatriculaAsync(client, professorId);
        await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios/{horarioId}/alocacoes",
            new CriarAlocacaoHorarioRequest(matriculaId));

        var response = await client.DeleteAsync($"/professores/{professorId}/horarios/{horarioId}/alocacoes/{matriculaId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var listagem = await client.GetFromJsonAsync<List<AlocacaoHorarioResponse>>(
            $"/professores/{professorId}/horarios/{horarioId}/alocacoes");
        listagem.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_Alocacao_ReturnsNotFound_QuandoInexistente()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);
        var horarioId = await CriarHorarioAsync(client, professorId);

        var response = await client.DeleteAsync($"/professores/{professorId}/horarios/{horarioId}/alocacoes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
