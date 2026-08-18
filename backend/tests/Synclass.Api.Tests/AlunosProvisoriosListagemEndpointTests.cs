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
/// Teste de fumaça do endpoint de listagem de Matrículas do Professor
/// (issue #8) — alimenta o seletor de Aluno do frontend na alocação a
/// horário. Usa EF Core InMemory, mesmo padrão de
/// <see cref="AlunoProvisorioCadastroEndpointTests"/>. Rota sem
/// <c>professorId</c> desde a issue #23 — "outro Professor" nos testes vira
/// "outro cliente autenticado".
/// </summary>
public sealed class AlunosProvisoriosListagemEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AlunosProvisoriosListagemEndpointTests(WebApplicationFactory<Program> factory)
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

    [Fact]
    public async Task Get_AlunosProvisorios_ListaMatriculasDoProfessor()
    {
        var (client, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var (outroClient, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await client.PostAsJsonAsync(
            "/professores/alunos-provisorios",
            new CadastroAlunoProvisorioRequest("João Pedro", "2024-001"));
        await outroClient.PostAsJsonAsync(
            "/professores/alunos-provisorios",
            new CadastroAlunoProvisorioRequest("Outro Aluno", "2024-002"));

        var response = await client.GetAsync("/professores/alunos-provisorios");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<AlunoProvisorioResponse>>();
        corpo.Should().ContainSingle(a => a.Nome == "João Pedro" && a.Identificador == "2024-001");
    }
}
