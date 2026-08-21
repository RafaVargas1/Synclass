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
/// Teste de fumaça do endpoint de confirmação de presença pelo Aluno (issue
/// #15). Mesmo padrão de <see cref="AulaCancelamentoEndpointTests"/> (EF
/// Core InMemory, seed direto de <see cref="Matricula"/> vinculada).
/// </summary>
public sealed class AulaConfirmacaoPresencaEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly AdvanceableClock _clock = new() { UtcNow = new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero) };
    private readonly WebApplicationFactory<Program> _factory;

    public AulaConfirmacaoPresencaEndpointTests(WebApplicationFactory<Program> factory)
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
            new CriarHorarioRequest(DiaSemana: diaSemana, HoraInicio: horaInicio, DuracaoMinutos: 60, TipoMarcacao: 1, LimiteAlunos: 2));
        var corpo = await response.Content.ReadFromJsonAsync<HorarioResponse>();
        return corpo!.Id;
    }

    /// <summary>
    /// Mesmo helper de seed de <see cref="AulaCancelamentoEndpointTests.VincularAlunoAoProfessorAsync"/>.
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

    [Fact]
    public async Task Post_ConfirmacaoPresenca_ReturnsOk_NoCaminhoFeliz()
    {
        var (professorClient, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(professorClient, professorId);
        var horarioId = await CriarHorarioAsync(professorClient, professorId, diaSemana: 2, new TimeOnly(10, 0));
        var (alunoClient, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var matriculaId = await VincularAlunoAoProfessorAsync(professorId, alunoUsuarioId);
        await AlocarAsync(professorClient, horarioId, matriculaId);

        var response = await alunoClient.PostAsync(
            $"/professores/{professorId}/horarios/{horarioId}/aulas/2026-08-18/confirmacao-presenca", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<ConfirmacaoPresencaResponse>();
        corpo!.MatriculaId.Should().Be(matriculaId);
        corpo.ConfirmadoPeloAluno.Should().BeTrue();
    }

    [Fact]
    public async Task Post_ConfirmacaoPresenca_ReturnsBadRequest_QuandoAlunoJaCancelouEssaOcorrencia()
    {
        var (professorClient, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(professorClient, professorId);
        // Quinta 18:00 (20/08) = ainda dentro do prazo (0 min) a partir de terça 12:00.
        var horarioId = await CriarHorarioAsync(professorClient, professorId, diaSemana: 4, new TimeOnly(18, 0));
        var (alunoClient, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var matriculaId = await VincularAlunoAoProfessorAsync(professorId, alunoUsuarioId);
        await AlocarAsync(professorClient, horarioId, matriculaId);
        var cancelamentoResponse = await alunoClient.PostAsync(
            $"/professores/{professorId}/horarios/{horarioId}/aulas/2026-08-20/cancelamentos", null);
        cancelamentoResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await alunoClient.PostAsync(
            $"/professores/{professorId}/horarios/{horarioId}/aulas/2026-08-20/confirmacao-presenca", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ConfirmacaoPresenca_ReturnsNotFound_QuandoHorarioInexistente()
    {
        var (professorClient, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var (alunoClient, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        await VincularAlunoAoProfessorAsync(professorId, alunoUsuarioId);

        var response = await alunoClient.PostAsync(
            $"/professores/{professorId}/horarios/{Guid.NewGuid()}/aulas/2026-08-18/confirmacao-presenca", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ConfirmacaoPresenca_ReturnsNotFound_QuandoAlunoNaoVinculadoAoProfessor()
    {
        var (professorClient, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(professorClient, professorId);
        var horarioId = await CriarHorarioAsync(professorClient, professorId, diaSemana: 2, new TimeOnly(10, 0));
        var (alunoClient, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);

        var response = await alunoClient.PostAsync(
            $"/professores/{professorId}/horarios/{horarioId}/aulas/2026-08-18/confirmacao-presenca", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
