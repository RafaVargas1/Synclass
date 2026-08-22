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
/// Teste de fumaça do endpoint de consulta de valor devido por Professor
/// (issue #12): mês corrente como default, matrícula sem regra, request
/// inválido (só um dos dois parâmetros de período) e isolamento entre
/// Professores (critério de aceite 4). Mesmo padrão de
/// <see cref="RegraDeCobrancaEndpointTests"/> (EF Core InMemory).
/// </summary>
public sealed class ValorDevidoEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ValorDevidoEndpointTests(WebApplicationFactory<Program> factory)
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

    /// <summary>
    /// Matrícula criada via <c>/professores/alunos-provisorios</c> (issue
    /// #23: sem <c>professorId</c> de rota) — pertence ao Professor
    /// autenticado em <paramref name="client"/>, não a um parâmetro
    /// explícito. Mesmo padrão de <c>RegraDeCobrancaEndpointTests</c>.
    /// </summary>
    private static async Task<Guid> CriarMatriculaAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/professores/alunos-provisorios",
            new CadastroAlunoProvisorioRequest("Aluno Teste"));
        var corpo = await response.Content.ReadFromJsonAsync<CadastroAlunoProvisorioResponse>();
        return corpo!.MatriculaId;
    }

    [Fact]
    public async Task Get_ValorDevido_SemInicioNemFim_UsaMesCorrenteEDevolveOkComListaDeAlunos()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await CriarMatriculaAsync(client);

        var response = await client.GetAsync($"/professores/{professorId}/valor-devido");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<ValorDevidoResponse>>();
        corpo.Should().ContainSingle();
    }

    [Fact]
    public async Task Get_ValorDevido_MatriculaSemRegra_AparaceComSemRegraDefinidaTrueEValorNull()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var matriculaId = await CriarMatriculaAsync(client);

        var response = await client.GetAsync($"/professores/{professorId}/valor-devido");

        var corpo = await response.Content.ReadFromJsonAsync<List<ValorDevidoResponse>>();
        var valorDevido = corpo.Should().ContainSingle(v => v.MatriculaId == matriculaId).Subject;
        valorDevido.SemRegraDefinida.Should().BeTrue();
        valorDevido.Valor.Should().BeNull();
    }

    [Fact]
    public async Task Get_ValorDevido_SoInicioInformado_DevolveBadRequest()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);

        var response = await client.GetAsync($"/professores/{professorId}/valor-devido?inicio=2026-08-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ValorDevido_AlunoVinculadoADoisProfessores_CadaProfessorVeSoOProprioValor()
    {
        var (clientA, professorA) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var (clientB, professorB) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var matriculaComA = await CriarMatriculaAsync(clientA);
        var matriculaComB = await CriarMatriculaAsync(clientB);
        await clientA.PutAsJsonAsync(
            $"/professores/{professorA}/matriculas/{matriculaComA}/regra-de-cobranca",
            new DefinirRegraDeCobrancaRequest("FixoMensal", 300m, null));
        await clientB.PutAsJsonAsync(
            $"/professores/{professorB}/matriculas/{matriculaComB}/regra-de-cobranca",
            new DefinirRegraDeCobrancaRequest("FixoMensal", 500m, null));

        var responseA = await clientA.GetAsync($"/professores/{professorA}/valor-devido");
        var responseB = await clientB.GetAsync($"/professores/{professorB}/valor-devido");

        var corpoA = await responseA.Content.ReadFromJsonAsync<List<ValorDevidoResponse>>();
        var corpoB = await responseB.Content.ReadFromJsonAsync<List<ValorDevidoResponse>>();
        corpoA.Should().ContainSingle(v => v.MatriculaId == matriculaComA && v.Valor == 300m);
        corpoB.Should().ContainSingle(v => v.MatriculaId == matriculaComB && v.Valor == 500m);
    }
}
