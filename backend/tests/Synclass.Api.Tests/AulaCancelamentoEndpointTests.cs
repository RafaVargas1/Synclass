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
using Synclass.Domain.Configuracoes;
using Synclass.Domain.Matriculas;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Api.Tests;

/// <summary>
/// Teste de fumaça dos endpoints de cancelamento de aula (issue #10):
/// cancelar dentro/fora do prazo e listar as próximas aulas. Usa
/// <see cref="AdvanceableClock"/> fixo (mesmo padrão de
/// <see cref="ConvitesEndpointTests"/>) para tornar os cálculos de prazo
/// determinísticos, e EF Core InMemory, mesmo padrão de
/// <see cref="MarcacaoHorarioEndpointTests"/>. `professorId` continua vindo
/// da rota (Professor sendo navegado), mas `matriculaId` deixou de ser
/// enviado pelo cliente (issue #23) — os testes seedam a Matrícula vinculada
/// ao Aluno autenticado direto no banco, mesmo padrão de
/// <see cref="MarcacaoHorarioEndpointTests.VincularAlunoAoProfessorAsync"/>.
/// </summary>
public sealed class AulaCancelamentoEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    // Terça-feira, 12:00 — mesmo instante fixo usado nos testes de domínio
    // equivalentes (AulaServiceCancelarAsyncTests).
    private readonly AdvanceableClock _clock = new() { UtcNow = new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero) };
    private readonly WebApplicationFactory<Program> _factory;

    public AulaCancelamentoEndpointTests(WebApplicationFactory<Program> factory)
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

    // Modelo Fixo (1): Vago (0) não permite alocação feita pelo Professor
    // (AlocacoesHorarioController, issue #8) — ver
    // AlocacaoHorarioService.GarantirModeloPermiteAlocacaoAsync.
    private static async Task DefinirModeloAgendamentoAsync(HttpClient client, Guid professorId, int modeloAgendamento = 1)
    {
        await client.PutAsJsonAsync(
            $"/professores/{professorId}/configuracao/modelo-agendamento",
            new DefinirModeloAgendamentoRequest(modeloAgendamento));
    }

    private static async Task<Guid> CriarHorarioAsync(HttpClient client, Guid professorId, int diaSemana, TimeOnly horaInicio)
    {
        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: diaSemana, HoraInicio: horaInicio, DuracaoMinutos: 60, LimiteAlunos: 2));
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

    private static async Task AlocarAsync(HttpClient professorClient, Guid horarioId, Guid matriculaId)
    {
        await professorClient.PostAsJsonAsync(
            $"/professores/horarios/{horarioId}/alocacoes",
            new CriarAlocacaoHorarioRequest(matriculaId));
    }

    /// <summary>
    /// Contorna a ausência de endpoint para definir <c>PrazoCancelamentoMinutos</c>
    /// (fora do escopo do task.md desta issue) escrevendo direto no
    /// DbContext, mesmo padrão de acesso usado por
    /// <see cref="AutenticacaoTestHelper"/> via <c>factory.Services</c>.
    /// </summary>
    private async Task DefinirPrazoCancelamentoAsync(Guid professorId, int prazoCancelamentoMinutos)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        var configuracao = await dbContext.ConfiguracoesProfessor.SingleAsync(c => c.ProfessorId == professorId);
        configuracao.AlterarPrazoCancelamento(prazoCancelamentoMinutos, _clock);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Post_Cancelamento_ReturnsOk_QuandoDentroDoPrazo()
    {
        var (professorClient, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(professorClient, professorId);
        // Quinta 18:00 (20/08) = 30h de antecedência a partir de terça 12:00.
        var horarioId = await CriarHorarioAsync(professorClient, professorId, diaSemana: 4, new TimeOnly(18, 0));
        var (alunoClient, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var matriculaId = await VincularAlunoAoProfessorAsync(professorId, alunoUsuarioId);
        await AlocarAsync(professorClient, horarioId, matriculaId);
        await DefinirPrazoCancelamentoAsync(professorId, prazoCancelamentoMinutos: 24 * 60);

        var response = await alunoClient.PostAsync(
            $"/professores/{professorId}/horarios/{horarioId}/aulas/2026-08-20/cancelamentos", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<CancelamentoAulaResponse>();
        corpo!.MatriculaId.Should().Be(matriculaId);
    }

    [Fact]
    public async Task Post_Cancelamento_ReturnsBadRequest_QuandoForaDoPrazo()
    {
        var (professorClient, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(professorClient, professorId);
        // Terça 22:00 (18/08) = 10h de antecedência a partir de terça 12:00.
        var horarioId = await CriarHorarioAsync(professorClient, professorId, diaSemana: 2, new TimeOnly(22, 0));
        var (alunoClient, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var matriculaId = await VincularAlunoAoProfessorAsync(professorId, alunoUsuarioId);
        await AlocarAsync(professorClient, horarioId, matriculaId);
        await DefinirPrazoCancelamentoAsync(professorId, prazoCancelamentoMinutos: 24 * 60);

        var response = await alunoClient.PostAsync(
            $"/professores/{professorId}/horarios/{horarioId}/aulas/2026-08-18/cancelamentos", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Cancelamento_ReturnsNotFound_QuandoHorarioInexistente()
    {
        var (professorClient, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var (alunoClient, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        await VincularAlunoAoProfessorAsync(professorId, alunoUsuarioId);

        var response = await alunoClient.PostAsync(
            $"/professores/{professorId}/horarios/{Guid.NewGuid()}/aulas/2026-08-20/cancelamentos", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_Cancelamento_ReturnsNotFound_QuandoAlunoNaoVinculadoAoProfessor()
    {
        var (professorClient, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(professorClient, professorId);
        var horarioId = await CriarHorarioAsync(professorClient, professorId, diaSemana: 4, new TimeOnly(18, 0));
        var (alunoClient, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);

        var response = await alunoClient.PostAsync(
            $"/professores/{professorId}/horarios/{horarioId}/aulas/2026-08-20/cancelamentos", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ProximasAulas_ReturnsOk_ComListaDeProximasAulas()
    {
        var (professorClient, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(professorClient, professorId);
        var horarioId = await CriarHorarioAsync(professorClient, professorId, diaSemana: 4, new TimeOnly(18, 0));
        var (alunoClient, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var matriculaId = await VincularAlunoAoProfessorAsync(professorId, alunoUsuarioId);
        await AlocarAsync(professorClient, horarioId, matriculaId);

        var response = await alunoClient.GetAsync($"/professores/{professorId}/horarios/proximas-aulas");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<AulaProximaResponse>>();
        corpo.Should().ContainSingle(a => a.HorarioId == horarioId && a.Data == "2026-08-20");
    }
}
