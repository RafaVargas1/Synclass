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
/// Teste de fumaça do endpoint de registro de frequência (issue #14): o
/// Professor registra presença/ausência em lote para os Alunos alocados num
/// horário/data. Mesmo padrão de <see cref="AulaCancelamentoEndpointTests"/>
/// (EF Core InMemory, <see cref="AdvanceableClock"/> fixo).
/// </summary>
public sealed class FrequenciaEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly AdvanceableClock _clock = new() { UtcNow = new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero) };
    private readonly WebApplicationFactory<Program> _factory;

    public FrequenciaEndpointTests(WebApplicationFactory<Program> factory)
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

    private static async Task<Guid> CriarHorarioAsync(HttpClient professorClient, Guid professorId, int diaSemana, TimeOnly horaInicio)
    {
        var response = await professorClient.PostAsJsonAsync(
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
    public async Task Post_Frequencias_ReturnsOk_ComRegistroEmLote()
    {
        var (professorClient, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(professorClient, professorId);
        var horarioId = await CriarHorarioAsync(professorClient, professorId, diaSemana: 2, new TimeOnly(10, 0));
        var (_, alunoUsuarioIdUm) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var matriculaPresente = await VincularAlunoAoProfessorAsync(professorId, alunoUsuarioIdUm);
        await AlocarAsync(professorClient, horarioId, matriculaPresente);
        var (_, alunoUsuarioIdDois) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var matriculaAusente = await VincularAlunoAoProfessorAsync(professorId, alunoUsuarioIdDois);
        await AlocarAsync(professorClient, horarioId, matriculaAusente);

        var response = await professorClient.PostAsJsonAsync(
            $"/professores/horarios/{horarioId}/aulas/2026-08-18/frequencias",
            new RegistrarFrequenciaRequest(new[]
            {
                new RegistroFrequenciaItemRequest(matriculaPresente, Presente: true),
                new RegistroFrequenciaItemRequest(matriculaAusente, Presente: false),
            }));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<RegistroFrequenciaResponse>>();
        corpo.Should().Contain(r => r.MatriculaId == matriculaPresente && r.Presente);
        corpo.Should().Contain(r => r.MatriculaId == matriculaAusente && !r.Presente);
    }

    [Fact]
    public async Task Post_Frequencias_ReturnsBadRequest_QuandoMatriculaNaoAlocadaNoHorario()
    {
        var (professorClient, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(professorClient, professorId);
        var horarioId = await CriarHorarioAsync(professorClient, professorId, diaSemana: 2, new TimeOnly(10, 0));
        var (_, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var matriculaNaoAlocada = await VincularAlunoAoProfessorAsync(professorId, alunoUsuarioId);

        var response = await professorClient.PostAsJsonAsync(
            $"/professores/horarios/{horarioId}/aulas/2026-08-18/frequencias",
            new RegistrarFrequenciaRequest(new[] { new RegistroFrequenciaItemRequest(matriculaNaoAlocada, Presente: true) }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Frequencias_ReturnsNotFound_QuandoHorarioInexistente()
    {
        var (professorClient, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);

        var response = await professorClient.PostAsJsonAsync(
            $"/professores/horarios/{Guid.NewGuid()}/aulas/2026-08-18/frequencias",
            new RegistrarFrequenciaRequest(Array.Empty<RegistroFrequenciaItemRequest>()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_Frequencias_ReturnsNotFound_QuandoHorarioNaoPertenceAoProfessor()
    {
        var (professorClient, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(professorClient, professorId);
        var horarioId = await CriarHorarioAsync(professorClient, professorId, diaSemana: 2, new TimeOnly(10, 0));
        var (outroProfessorClient, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);

        var response = await outroProfessorClient.PostAsJsonAsync(
            $"/professores/horarios/{horarioId}/aulas/2026-08-18/frequencias",
            new RegistrarFrequenciaRequest(Array.Empty<RegistroFrequenciaItemRequest>()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
