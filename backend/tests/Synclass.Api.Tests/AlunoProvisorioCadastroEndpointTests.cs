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
/// Teste de fumaça do endpoint de cadastro de Aluno provisório (issue #3):
/// sucesso, nome inválido e identificador duplicado. Usa EF Core InMemory
/// (banco isolado por teste) no lugar de um Postgres real — mesmo padrão de
/// <see cref="ProfessorCadastroEndpointTests"/>.
/// </summary>
public sealed class AlunoProvisorioCadastroEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AlunoProvisorioCadastroEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task Post_Cadastro_ReturnsOk_QuandoDadosValidos()
    {
        var client = _factory.CreateClient();
        var professorId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/alunos-provisorios",
            new CadastroAlunoProvisorioRequest("João Pedro", "2024-013"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<CadastroAlunoProvisorioResponse>();
        corpo!.Nome.Should().Be("João Pedro");
        corpo.Identificador.Should().Be("2024-013");
        corpo.MatriculaId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Post_Cadastro_ReturnsBadRequest_QuandoNomeVazio()
    {
        var client = _factory.CreateClient();
        var professorId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/alunos-provisorios",
            new CadastroAlunoProvisorioRequest("   ", "2024-013"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<CadastroAlunoProvisorioErrorResponse>();
        corpo!.Mensagem.Should().Contain("Nome inválido");
    }

    [Fact]
    public async Task Post_Cadastro_ReturnsBadRequest_QuandoIdentificadorJaUsadoPeloMesmoProfessor()
    {
        var client = _factory.CreateClient();
        var professorId = Guid.NewGuid();
        var request = new CadastroAlunoProvisorioRequest("João Pedro", "2024-013");
        await client.PostAsJsonAsync($"/professores/{professorId}/alunos-provisorios", request);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/alunos-provisorios",
            new CadastroAlunoProvisorioRequest("Outro Aluno", "2024-013"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<CadastroAlunoProvisorioErrorResponse>();
        corpo!.Mensagem.Should().Contain("2024-013");
    }

    [Fact]
    public async Task Post_Cadastro_ReturnsOk_QuandoIdentificadorRepetidoEmOutroProfessor()
    {
        var client = _factory.CreateClient();
        var professorId1 = Guid.NewGuid();
        var professorId2 = Guid.NewGuid();
        var request = new CadastroAlunoProvisorioRequest("João Pedro", "2024-013");
        await client.PostAsJsonAsync($"/professores/{professorId1}/alunos-provisorios", request);

        var response = await client.PostAsJsonAsync($"/professores/{professorId2}/alunos-provisorios", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
