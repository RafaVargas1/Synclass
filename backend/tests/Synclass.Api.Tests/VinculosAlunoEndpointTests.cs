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
using Synclass.Domain.Usuarios;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Api.Tests;

/// <summary>
/// Teste de fumaça do endpoint de listagem de vínculos (Professores) do
/// Aluno autenticado (issue #165, `GET /alunos/professores`): alimenta o
/// resumo de "próximo horário" do Painel, que precisa agregar as próximas
/// aulas por todos os Professores do Aluno. Mesmo padrão de
/// <see cref="HistoricoFrequenciaEndpointTests"/> (EF Core InMemory,
/// autenticado via token).
/// </summary>
public sealed class VinculosAlunoEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public VinculosAlunoEndpointTests(WebApplicationFactory<Program> factory)
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
        var professor = Usuario.Cadastrar(nome, $"{Guid.NewGuid()}@exemplo.com", PapelUsuario.Professor, null, clock);
        dbContext.Usuarios.Add(professor);
        await dbContext.SaveChangesAsync();
        return professor.Id;
    }

    private async Task VincularMatriculaAsync(Guid professorId, Guid alunoUsuarioId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var matricula = Matricula.CriarVinculada(professorId, alunoUsuarioId, clock);
        dbContext.Matriculas.Add(matricula);
        await dbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task Get_VinculosAluno_ComMatriculaEmDoisProfessores_DevolveOsDoisSemDuplicar()
    {
        var (client, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var professorA = await CriarProfessorPersistidoAsync("Professor A");
        var professorB = await CriarProfessorPersistidoAsync("Professor B");
        await VincularMatriculaAsync(professorA, alunoUsuarioId);
        await VincularMatriculaAsync(professorB, alunoUsuarioId);
        // Mata mais de uma matrícula com o mesmo Professor (não deveria pela
        // regra de negócio, mas o Distinct() do controller protege).
        await VincularMatriculaAsync(professorA, alunoUsuarioId);

        var response = await client.GetAsync("/alunos/professores");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<VinculoProfessorResponse>>();
        corpo.Should().HaveCount(2);
        corpo.Should().ContainSingle(v => v.ProfessorId == professorA && v.Nome == "Professor A");
        corpo.Should().ContainSingle(v => v.ProfessorId == professorB && v.Nome == "Professor B");
    }

    [Fact]
    public async Task Get_VinculosAluno_SemNenhumaMatricula_DevolveOkComListaVazia()
    {
        var (client, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);

        var response = await client.GetAsync("/alunos/professores");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<VinculoProfessorResponse>>();
        corpo.Should().BeEmpty();
    }
}
