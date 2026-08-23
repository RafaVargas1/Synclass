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
/// Teste de fumaça dos endpoints de regra de cobrança (issue #11): definir
/// (PUT) e consultar (GET) a regra vigente de uma matrícula. Mesmo padrão de
/// <see cref="HorarioEndpointTests"/> (EF Core InMemory).
/// </summary>
public sealed class RegraDeCobrancaEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public RegraDeCobrancaEndpointTests(WebApplicationFactory<Program> factory)
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
    /// explícito.
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
    public async Task Put_RegraDeCobranca_ReturnsOk_QuandoValorPorAulaComFrequenciaValida()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var matriculaId = await CriarMatriculaAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/professores/{professorId}/matriculas/{matriculaId}/regra-de-cobranca",
            new DefinirRegraDeCobrancaRequest("ValorPorAula", 50m, 3));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<RegraDeCobrancaResponse>();
        corpo!.Tipo.Should().Be("ValorPorAula");
        corpo.Valor.Should().Be(50m);
        corpo.FrequenciaSemanalContratada.Should().Be(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task Put_RegraDeCobranca_ReturnsBadRequest_QuandoValorMenorOuIgualAZero(decimal valor)
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var matriculaId = await CriarMatriculaAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/professores/{professorId}/matriculas/{matriculaId}/regra-de-cobranca",
            new DefinirRegraDeCobrancaRequest("FixoMensal", valor, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Put_RegraDeCobranca_ReturnsNotFound_QuandoMatriculaNaoPertenceAoProfessor()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var (outroClient, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var matriculaDeOutroProfessor = await CriarMatriculaAsync(outroClient);

        var response = await client.PutAsJsonAsync(
            $"/professores/{professorId}/matriculas/{matriculaDeOutroProfessor}/regra-de-cobranca",
            new DefinirRegraDeCobrancaRequest("FixoMensal", 300m, null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_RegraDeCobranca_ReturnsNotFound_QuandoSemRegraConfigurada()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var matriculaId = await CriarMatriculaAsync(client);

        var response = await client.GetAsync($"/professores/{professorId}/matriculas/{matriculaId}/regra-de-cobranca");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_RegraDeCobranca_ReturnsOk_ComDadosDaRegraVigente_AposPutBemSucedido()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var matriculaId = await CriarMatriculaAsync(client);
        await client.PutAsJsonAsync(
            $"/professores/{professorId}/matriculas/{matriculaId}/regra-de-cobranca",
            new DefinirRegraDeCobrancaRequest("FixoMensal", 300m, null));

        var response = await client.GetAsync($"/professores/{professorId}/matriculas/{matriculaId}/regra-de-cobranca");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<RegraDeCobrancaResponse>();
        corpo!.Tipo.Should().Be("FixoMensal");
        corpo.Valor.Should().Be(300m);
    }

    [Fact]
    public async Task Put_RegraDeCobranca_ReturnsOk_ComBaseDeContagemPresencaConfirmada_QuandoFixoPorAula()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var matriculaId = await CriarMatriculaAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/professores/{professorId}/matriculas/{matriculaId}/regra-de-cobranca",
            new DefinirRegraDeCobrancaRequest("FixoPorAula", 50m, null, "PresencaConfirmada"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<RegraDeCobrancaResponse>();
        corpo!.BaseDeContagemAula.Should().Be("PresencaConfirmada");
    }

    [Fact]
    public async Task Put_RegraDeCobranca_ReturnsOk_SemBaseDeContagemInformada_AplicaDefaultAgendamento()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var matriculaId = await CriarMatriculaAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/professores/{professorId}/matriculas/{matriculaId}/regra-de-cobranca",
            new DefinirRegraDeCobrancaRequest("FixoPorAula", 50m, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<RegraDeCobrancaResponse>();
        corpo!.BaseDeContagemAula.Should().Be("Agendamento");
    }

    [Fact]
    public async Task Put_RegraDeCobranca_ReturnsBadRequest_QuandoBaseDeContagemInformadaParaFixoMensal()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var matriculaId = await CriarMatriculaAsync(client);

        var response = await client.PutAsJsonAsync(
            $"/professores/{professorId}/matriculas/{matriculaId}/regra-de-cobranca",
            new DefinirRegraDeCobrancaRequest("FixoMensal", 300m, null, "PresencaConfirmada"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
