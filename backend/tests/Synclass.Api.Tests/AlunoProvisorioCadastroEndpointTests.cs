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
/// <see cref="ProfessorCadastroEndpointTests"/>. Rota sem <c>professorId</c>
/// desde a issue #23 — cada cliente autenticado (persistido, ver
/// <see cref="AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync"/>)
/// só cadastra/lista os próprios Alunos, então "outro Professor" nos testes
/// vira "outro cliente autenticado", não mais um segundo `professorId` na
/// URL do mesmo cliente.
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
        var (client, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);

        var response = await client.PostAsJsonAsync(
            "/professores/alunos-provisorios",
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
        var (client, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);

        var response = await client.PostAsJsonAsync(
            "/professores/alunos-provisorios",
            new CadastroAlunoProvisorioRequest("   ", "2024-013"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<CadastroAlunoProvisorioErrorResponse>();
        corpo!.Mensagem.Should().Contain("Nome inválido");
    }

    [Fact]
    public async Task Post_Cadastro_ReturnsBadRequest_QuandoIdentificadorJaUsadoPeloMesmoProfessor()
    {
        var (client, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var request = new CadastroAlunoProvisorioRequest("João Pedro", "2024-013");
        await client.PostAsJsonAsync("/professores/alunos-provisorios", request);

        var response = await client.PostAsJsonAsync(
            "/professores/alunos-provisorios",
            new CadastroAlunoProvisorioRequest("Outro Aluno", "2024-013"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<CadastroAlunoProvisorioErrorResponse>();
        corpo!.Mensagem.Should().Contain("2024-013");
    }

    [Fact]
    public async Task Post_Cadastro_ReturnsOk_QuandoIdentificadorRepetidoEmOutroProfessor()
    {
        var (client1, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var (client2, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var request = new CadastroAlunoProvisorioRequest("João Pedro", "2024-013");
        await client1.PostAsJsonAsync("/professores/alunos-provisorios", request);

        var response = await client2.PostAsJsonAsync("/professores/alunos-provisorios", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Regressão do achado de dev-review no PR #22 — "professorId inexistente"
    /// não é mais alcançável via rota (issue #23, `professorId` vem do token
    /// já validado por <c>[Authorize]</c>). O caso equivalente hoje é o token
    /// de um Professor cujo `Usuario` nunca foi persistido (achado de defesa,
    /// não de fluxo de produto — o token só existiria assim em teste, ver
    /// <see cref="AutenticacaoTestHelper.ClienteAutenticadoComoProfessor"/>).
    /// </summary>
    [Fact]
    public async Task Post_Cadastro_ReturnsNotFound_QuandoProfessorDoTokenNaoPersistido()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);

        var response = await client.PostAsJsonAsync(
            "/professores/alunos-provisorios",
            new CadastroAlunoProvisorioRequest("João Pedro", "2024-013"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
