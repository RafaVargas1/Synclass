using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synclass.Api.Controllers;
using Synclass.Api.Tests.Fakes;
using Synclass.Domain.Alocacoes;
using Synclass.Domain.Common;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Usuarios;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Api.Tests;

/// <summary>
/// Teste de fumaça do endpoint de histórico de frequência do Aluno (issue
/// #16): mês corrente como default, `inicio`/`fim` explícitos e período
/// incompleto/invertido rejeitado — mesmo padrão de
/// <see cref="ValorDevidoAlunoEndpointTests"/> (EF Core InMemory).
/// </summary>
public sealed class HistoricoFrequenciaEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HistoricoFrequenciaEndpointTests(WebApplicationFactory<Program> factory)
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

    private async Task<Guid> CriarProfessorPersistidoAsync(string nome)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var professor = Usuario.Cadastrar(nome, $"{Guid.NewGuid()}@exemplo.com", PapelUsuario.Professor, clock);
        dbContext.Usuarios.Add(professor);
        await dbContext.SaveChangesAsync();
        return professor.Id;
    }

    private async Task<Guid> VincularMatriculaComHorarioAlocadoAsync(Guid professorId, Guid alunoUsuarioId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var matricula = Matricula.CriarVinculada(professorId, alunoUsuarioId, clock);
        var horario = Horario.Criar(professorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, clock);
        var alocacao = AlocacaoHorario.Criar(horario.Id, matricula.Id, OrigemAlocacao.Aluno, clock);
        dbContext.Matriculas.Add(matricula);
        dbContext.Horarios.Add(horario);
        dbContext.AlocacoesHorario.Add(alocacao);
        await dbContext.SaveChangesAsync();
        return matricula.Id;
    }

    [Fact]
    public async Task Get_HistoricoFrequencia_SemInicioNemFim_UsaMesCorrenteEDevolveOkComListaDeProfessores()
    {
        var (client, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var professorId = await CriarProfessorPersistidoAsync("Professor Um");
        await VincularMatriculaComHorarioAlocadoAsync(professorId, alunoUsuarioId);

        var response = await client.GetAsync("/alunos/historico-frequencia");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<HistoricoFrequenciaPorProfessorResponse>>();
        corpo.Should().ContainSingle(h => h.ProfessorId == professorId && h.NomeProfessor == "Professor Um");
    }

    [Fact]
    public async Task Get_HistoricoFrequencia_InicioEFimExplicitos_DevolveOkComAulasNoPeriodo()
    {
        var (client, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var professorId = await CriarProfessorPersistidoAsync("Professor Dois");
        await VincularMatriculaComHorarioAlocadoAsync(professorId, alunoUsuarioId);

        var response = await client.GetAsync("/alunos/historico-frequencia?inicio=2026-08-01&fim=2026-09-01");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<HistoricoFrequenciaPorProfessorResponse>>();
        var historico = corpo.Should().ContainSingle().Subject;
        historico.Aulas.Should().OnlyContain(a => a.Status == "NaoRegistrada");
    }

    [Fact]
    public async Task Get_HistoricoFrequencia_AlunoSemNenhumaMatricula_DevolveOkComListaVazia()
    {
        var (client, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);

        var response = await client.GetAsync("/alunos/historico-frequencia");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<HistoricoFrequenciaPorProfessorResponse>>();
        corpo.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_HistoricoFrequencia_SoInicioInformado_DevolveBadRequest()
    {
        var (client, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);

        var response = await client.GetAsync("/alunos/historico-frequencia?inicio=2026-08-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_HistoricoFrequencia_PeriodoInvertido_DevolveBadRequest()
    {
        var (client, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);

        var response = await client.GetAsync("/alunos/historico-frequencia?inicio=2026-09-01&fim=2026-08-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
