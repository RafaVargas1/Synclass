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
using Synclass.Domain.Matriculas;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Api.Tests;

/// <summary>
/// Teste de fumaça dos endpoints de marcação livre do Aluno (issue #9):
/// listar horários vagos e marcar. Mesmo padrão de
/// <see cref="AlocacaoHorarioEndpointTests"/> (issue #8), EF Core InMemory.
/// `professorId` continua vindo da rota (Professor sendo navegado), mas
/// `matriculaId` deixou de ser enviado pelo cliente (issue #23) — os testes
/// agora seedam a Matrícula vinculada ao Aluno autenticado direto no banco,
/// em vez de repassar `matriculaId` no corpo/query.
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

    private static async Task DefinirModeloAgendamentoAsync(HttpClient client, Guid professorId, int modeloAgendamento)
    {
        await client.PutAsJsonAsync(
            $"/professores/{professorId}/configuracao/modelo-agendamento",
            new DefinirModeloAgendamentoRequest(modeloAgendamento));
    }

    private static async Task<Guid> CriarHorarioAsync(HttpClient client, Guid professorId, int limiteAlunos = 1)
    {
        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: 0, LimiteAlunos: limiteAlunos));
        var corpo = await response.Content.ReadFromJsonAsync<HorarioResponse>();
        return corpo!.Id;
    }

    /// <summary>
    /// Seeda direto no banco uma Matrícula já plena vinculando este
    /// <paramref name="alunoUsuarioId"/> a <paramref name="professorId"/> —
    /// equivalente ao estado após aceite de convite (issue #2), sem passar
    /// pelo fluxo completo. Necessário porque a resolução de
    /// <c>matriculaId</c> (issue #23) exige <see cref="Matricula.AlunoUsuarioId"/>
    /// preenchido; uma matrícula provisória (<see cref="Matricula.CriarProvisoria"/>)
    /// não seria encontrada por <c>BuscarVinculoAsync</c>.
    /// </summary>
    private async Task<Guid> VincularAlunoAoProfessorAsync(Guid professorId, Guid alunoUsuarioId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var matricula = Matricula.CriarVinculada(professorId, alunoUsuarioId, clock);
        dbContext.Matriculas.Add(matricula);
        await dbContext.SaveChangesAsync();
        return matricula.Id;
    }

    [Fact]
    public async Task Post_Marcacao_ReturnsOk_QuandoModeloVagoComVaga()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId, modeloAgendamento: 0);
        var horarioId = await CriarHorarioAsync(client, professorId);
        var (alunoClient, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var matriculaId = await VincularAlunoAoProfessorAsync(professorId, alunoUsuarioId);

        var response = await alunoClient.PostAsync($"/professores/{professorId}/horarios/{horarioId}/marcacoes", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<AlocacaoHorarioResponse>();
        corpo!.HorarioId.Should().Be(horarioId);
        corpo.MatriculaId.Should().Be(matriculaId);
    }

    [Fact]
    public async Task Post_Marcacao_ReturnsBadRequest_QuandoModeloFixo()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId, modeloAgendamento: 1);
        var horarioId = await CriarHorarioAsync(client, professorId);
        var (alunoClient, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        await VincularAlunoAoProfessorAsync(professorId, alunoUsuarioId);

        var response = await alunoClient.PostAsync($"/professores/{professorId}/horarios/{horarioId}/marcacoes", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Marcacao_ReturnsBadRequest_QuandoHorarioLotado()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId, modeloAgendamento: 0);
        var horarioId = await CriarHorarioAsync(client, professorId, limiteAlunos: 1);
        var (primeiroAlunoClient, primeiroAlunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        await VincularAlunoAoProfessorAsync(professorId, primeiroAlunoUsuarioId);
        await primeiroAlunoClient.PostAsync($"/professores/{professorId}/horarios/{horarioId}/marcacoes", null);
        var (segundoAlunoClient, segundoAlunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        await VincularAlunoAoProfessorAsync(professorId, segundoAlunoUsuarioId);

        var response = await segundoAlunoClient.PostAsync($"/professores/{professorId}/horarios/{horarioId}/marcacoes", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Marcacao_ReturnsNotFound_QuandoAlunoNaoVinculadoAoProfessor()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId, modeloAgendamento: 0);
        var horarioId = await CriarHorarioAsync(client, professorId);
        var (alunoClient, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);

        var response = await alunoClient.PostAsync($"/professores/{professorId}/horarios/{horarioId}/marcacoes", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_Marcacao_ReturnsBadRequest_QuandoAlunoJaMarcado()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId, modeloAgendamento: 0);
        var horarioId = await CriarHorarioAsync(client, professorId, limiteAlunos: 2);
        var (alunoClient, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        await VincularAlunoAoProfessorAsync(professorId, alunoUsuarioId);
        await alunoClient.PostAsync($"/professores/{professorId}/horarios/{horarioId}/marcacoes", null);

        var response = await alunoClient.PostAsync($"/professores/{professorId}/horarios/{horarioId}/marcacoes", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Marcacao_ReturnsNotFound_QuandoHorarioInexistente()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId, modeloAgendamento: 0);
        var (alunoClient, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        await VincularAlunoAoProfessorAsync(professorId, alunoUsuarioId);

        var response = await alunoClient.PostAsync($"/professores/{professorId}/horarios/{Guid.NewGuid()}/marcacoes", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Vagos_ListaHorariosDisponiveisNoModeloVago()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId, modeloAgendamento: 0);
        var horarioId = await CriarHorarioAsync(client, professorId);
        var (alunoClient, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        await VincularAlunoAoProfessorAsync(professorId, alunoUsuarioId);

        var response = await alunoClient.GetAsync($"/professores/{professorId}/horarios/vagos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<HorarioVagoResponse>>();
        corpo.Should().ContainSingle(h => h.Id == horarioId && h.VagasRestantes == 1);
    }

    [Fact]
    public async Task Get_Vagos_ListaVaziaNoModeloFixo()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId, modeloAgendamento: 1);
        await CriarHorarioAsync(client, professorId);
        var (alunoClient, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        await VincularAlunoAoProfessorAsync(professorId, alunoUsuarioId);

        var response = await alunoClient.GetAsync($"/professores/{professorId}/horarios/vagos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<HorarioVagoResponse>>();
        corpo.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_Vagos_ReturnsNotFound_QuandoAlunoNaoVinculadoAoProfessor()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId, modeloAgendamento: 0);
        var (alunoClient, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);

        var response = await alunoClient.GetAsync($"/professores/{professorId}/horarios/vagos");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
