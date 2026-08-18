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
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CadastrarProfessorAsync(client, "professor1@exemplo.com");

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
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CadastrarProfessorAsync(client, "professor2@exemplo.com");

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
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CadastrarProfessorAsync(client, "professor3@exemplo.com");
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
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId1 = await CadastrarProfessorAsync(client, "professor4@exemplo.com");
        var professorId2 = await CadastrarProfessorAsync(client, "professor5@exemplo.com");
        var request = new CadastroAlunoProvisorioRequest("João Pedro", "2024-013");
        await client.PostAsJsonAsync($"/professores/{professorId1}/alunos-provisorios", request);

        var response = await client.PostAsJsonAsync($"/professores/{professorId2}/alunos-provisorios", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Regressão do achado de dev-review no PR #22: um professorId que não
    /// corresponde a nenhum Usuario cadastrado deve retornar 404 com um erro
    /// específico, não o 400 genérico de "conflito, tente novamente" que só
    /// apareceria antes por violação de foreign key em um Postgres real — o
    /// EF Core InMemory usado nestes testes nunca a aplicava.
    /// </summary>
    [Fact]
    public async Task Post_Cadastro_ReturnsNotFound_QuandoProfessorIdInexistente()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorIdInexistente = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorIdInexistente}/alunos-provisorios",
            new CadastroAlunoProvisorioRequest("João Pedro", "2024-013"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var corpo = await response.Content.ReadFromJsonAsync<CadastroAlunoProvisorioErrorResponse>();
        corpo!.Mensagem.Should().Contain(professorIdInexistente.ToString());
    }

    private static async Task<Guid> CadastrarProfessorAsync(HttpClient client, string contato)
    {
        var response = await client.PostAsJsonAsync(
            "/professores/cadastro", new CadastroProfessorRequest("Professor Teste", contato));
        var corpo = await response.Content.ReadFromJsonAsync<CadastroProfessorResponse>();
        return corpo!.UsuarioId;
    }
}
