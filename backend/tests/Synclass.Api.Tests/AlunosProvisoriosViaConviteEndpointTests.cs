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
/// Regressão: um Aluno que se cadastra via código de convite (sem passar
/// por uma matrícula provisória prévia) nasce com uma <c>Matricula</c> sem
/// <c>NomeProvisorio</c> (<c>Matricula.CriarVinculada</c>) — antes desta
/// correção, "Meus Alunos"/o seletor de alocação mostravam esse Aluno com
/// nome em branco, mesmo o vínculo com o Professor tendo sido criado
/// corretamente (bug reportado pelo usuário). Mesmo padrão de
/// <see cref="AlunosProvisoriosListagemEndpointTests"/>.
/// </summary>
public sealed class AlunosProvisoriosViaConviteEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AlunosProvisoriosViaConviteEndpointTests(WebApplicationFactory<Program> factory)
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
    public async Task Get_AlunosProvisorios_MostraNomeDoAlunoQueAceitouConvitePorCodigo()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);

        var gerarResponse = await client.PostAsJsonAsync(
            $"/professores/{professorId}/convites", new GerarConviteRequest("11987654321", null));
        gerarResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var convite = await gerarResponse.Content.ReadFromJsonAsync<GerarConviteResponse>();

        var aceiteResponse = await client.PostAsJsonAsync(
            $"/convites/codigo/{convite!.Codigo}/aceite",
            new AceitarConviteRequest("Maria Aluna", "11987654321"));
        aceiteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listagem = await client.GetAsync("/professores/alunos-provisorios");
        listagem.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await listagem.Content.ReadFromJsonAsync<List<AlunoProvisorioResponse>>();

        corpo.Should().ContainSingle(a => a.Nome == "Maria Aluna");
    }
}
