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
/// Teste de fumaça do endpoint de cadastro independente de Aluno
/// (issue #61): sucesso, contato inválido, contato já cadastrado como
/// Aluno, e reaproveitamento de identidade já existente como Professor —
/// espelha <see cref="ProfessorCadastroEndpointTests"/> trocando o papel.
/// </summary>
public sealed class AlunoCadastroEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AlunoCadastroEndpointTests(WebApplicationFactory<Program> factory)
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

        var response = await client.PostAsJsonAsync(
            "/alunos/cadastro",
            new CadastroUsuarioRequest("João Souza", "joao@exemplo.com"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<CadastroUsuarioResponse>();
        corpo!.Nome.Should().Be("João Souza");
    }

    [Fact]
    public async Task Post_Cadastro_ReturnsBadRequest_QuandoContatoInvalido()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/alunos/cadastro",
            new CadastroUsuarioRequest("João Souza", "nao-e-um-contato-valido"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<CadastroUsuarioErrorResponse>();
        corpo!.Mensagem.Should().Contain("Contato inválido");
    }

    [Fact]
    public async Task Post_Cadastro_ReturnsBadRequest_QuandoContatoJaCadastradoComoAluno()
    {
        var client = _factory.CreateClient();
        var request = new CadastroUsuarioRequest("João Souza", "duplicada@exemplo.com");
        await client.PostAsJsonAsync("/alunos/cadastro", request);

        var response = await client.PostAsJsonAsync("/alunos/cadastro", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<CadastroUsuarioErrorResponse>();
        corpo!.Mensagem.Should().Contain("Aluno");
    }

    [Fact]
    public async Task Post_Cadastro_AdicionaPapelAluno_QuandoContatoJaCadastradoComoProfessor()
    {
        var client = _factory.CreateClient();
        var contato = "joao-professor@exemplo.com";
        await client.PostAsJsonAsync("/professores/cadastro", new CadastroUsuarioRequest("João Souza", contato));

        var response = await client.PostAsJsonAsync("/alunos/cadastro", new CadastroUsuarioRequest("João Souza", contato));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<CadastroUsuarioResponse>();
        corpo!.Nome.Should().Be("João Souza");
    }

    [Fact]
    public async Task Get_VerificarContato_ReturnsIdentidadeExistenteTrue_QuandoAlunoJaCadastrado()
    {
        var client = _factory.CreateClient();
        var contato = "existente-aluno@exemplo.com";
        await client.PostAsJsonAsync("/alunos/cadastro", new CadastroUsuarioRequest("Maria Aluna", contato));

        var response = await client.GetAsync($"/alunos/verificar-contato?contato={contato}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<VerificarContatoResponse>();
        corpo!.IdentidadeExistente.Should().BeTrue();
        corpo.Nome.Should().Be("Maria Aluna");
    }

    [Fact]
    public async Task Get_VerificarContato_ReturnsIdentidadeExistenteFalse_QuandoContatoNovo()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/alunos/verificar-contato?contato=novo-aluno@exemplo.com");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<VerificarContatoResponse>();
        corpo!.IdentidadeExistente.Should().BeFalse();
    }
}
