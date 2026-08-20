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
/// Teste de fumaça da issue #70 (todo Aluno recebe um identificador único e
/// human-readable): os endpoints que já expõem dados do Aluno criado —
/// <see cref="AlunosController"/> (/alunos/cadastro),
/// <see cref="UsuariosController"/> (/usuarios/me) e
/// <see cref="AlunosProvisoriosController"/> (/professores/alunos-provisorios)
/// — continuam funcionando com o novo campo <c>IdentificadorAluno</c>
/// presente na entidade, sem quebrar o contrato HTTP existente (o campo não
/// é exposto na resposta — ver docs/specs/70-identificador-aluno/implementation.md).
/// Contrato existente preservado é validado conferindo o próprio banco (o
/// campo foi de fato gerado e persistido) além do status/body das rotas.
/// </summary>
public sealed class IdentificadorAlunoEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public IdentificadorAlunoEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task Post_AlunosCadastro_GeraIdentificadorAluno_PersisteNaEntidade()
    {
        var client = _factory.CreateClient();
        var contato = $"aluno-{Guid.NewGuid():N}@exemplo.com";

        var response = await client.PostAsJsonAsync(
            "/alunos/cadastro",
            new CadastroUsuarioRequest("João Aluno", contato));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<CadastroUsuarioResponse>();
        corpo!.Nome.Should().Be("João Aluno");

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        var usuario = await dbContext.Usuarios.SingleAsync(u => u.Id == corpo.UsuarioId);
        usuario.IdentificadorAluno.Should().NotBeNullOrEmpty();
        usuario.IdentificadorAluno.Should().MatchRegex("^ALU-[2-9A-HJ-NP-Z]{4}$");
    }

    [Fact]
    public async Task Get_UsuariosMe_ContratoPreservado_ParaAlunoComIdentificadorGravado()
    {
        var (client, usuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);

        var response = await client.GetAsync("/usuarios/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<UsuarioPerfilResponse>();
        corpo!.UsuarioId.Should().Be(usuarioId);
        corpo.Nome.Should().Be("Usuário de Teste");
    }

    [Fact]
    public async Task Post_AlunosProvisorios_GeraIdentificadorNaMatricula_EListaSemQuebrarContrato()
    {
        var (client, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);

        var response = await client.PostAsJsonAsync(
            "/professores/alunos-provisorios",
            new CadastroAlunoProvisorioRequest("João Pedro", "2024-014"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<CadastroAlunoProvisorioResponse>();
        corpo!.Nome.Should().Be("João Pedro");
        corpo.Identificador.Should().Be("2024-014");

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
            var matricula = await dbContext.Matriculas.SingleAsync(m => m.Id == corpo.MatriculaId);
            matricula.IdentificadorAluno.Should().NotBeNullOrEmpty();
            matricula.IdentificadorAluno.Should().MatchRegex("^ALU-[2-9A-HJ-NP-Z]{4}$");
        }

        // Listagem (issue #8) continua funcionando sem quebrar o contrato
        // existente com o novo campo presente na entidade.
        var listagem = await client.GetAsync("/professores/alunos-provisorios");
        listagem.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await listagem.Content.ReadFromJsonAsync<List<AlunoProvisorioResponse>>();
        lista.Should().ContainSingle(a => a.Nome == "João Pedro" && a.Identificador == "2024-014");
    }
}
