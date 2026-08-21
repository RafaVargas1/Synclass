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
/// Teste de fumaça dos endpoints de alocação de Aluno a horário (issue #8):
/// criar, listar e desfazer. Usa EF Core InMemory, mesmo padrão de
/// <see cref="HorarioEndpointTests"/>. Rota sem <c>professorId</c> desde a
/// issue #23 — usa <see cref="AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync"/>
/// (o Professor precisa existir de fato no banco, já que o controller
/// deriva `professorId` do token). `HorariosController`/`ConfiguracoesController`
/// continuam fora de escopo (mantêm `professorId` de rota).
/// </summary>
public sealed class AlocacaoHorarioEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AlocacaoHorarioEndpointTests(WebApplicationFactory<Program> factory)
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
    /// Define o modelo de agendamento do Professor autenticado (0 = Vago,
    /// 1 = Fixo, 2 = Híbrido — ver <c>ModeloAgendamento</c>). Endpoint fora
    /// de escopo da issue #23 (<c>ConfiguracoesController</c>), continua
    /// recebendo <c>professorId</c> na rota.
    /// </summary>
    private static async Task DefinirModeloAgendamentoAsync(HttpClient client, Guid professorId, int modeloAgendamento = 1)
    {
        await client.PutAsJsonAsync(
            $"/professores/{professorId}/configuracao/modelo-agendamento",
            new DefinirModeloAgendamentoRequest(modeloAgendamento));
    }

    /// <summary>
    /// <paramref name="tipoMarcacao"/> é quem controla, desde a issue #74, se
    /// a alocação/marcação é aceita neste horário (0 = Livre, 1 = Fixo,
    /// 2 = Híbrido) — o `PUT .../configuracao/modelo-agendamento` continua
    /// sendo pré-requisito do cadastro do horário (issue #7), mas deixou de
    /// controlar o cenário testado aqui.
    /// </summary>
    private static async Task<Guid> CriarHorarioAsync(HttpClient client, Guid professorId, int limiteAlunos = 1, int tipoMarcacao = 1)
    {
        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: tipoMarcacao, LimiteAlunos: limiteAlunos));
        var corpo = await response.Content.ReadFromJsonAsync<HorarioResponse>();
        return corpo!.Id;
    }

    private static async Task<Guid> CriarMatriculaAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/professores/alunos-provisorios",
            new CadastroAlunoProvisorioRequest("Aluno Teste", $"aluno-{Guid.NewGuid()}"));
        var corpo = await response.Content.ReadFromJsonAsync<CadastroAlunoProvisorioResponse>();
        return corpo!.MatriculaId;
    }

    [Fact]
    public async Task Post_Alocacao_ReturnsOk_QuandoDadosValidos()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId);
        var horarioId = await CriarHorarioAsync(client, professorId);
        var matriculaId = await CriarMatriculaAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/professores/horarios/{horarioId}/alocacoes",
            new CriarAlocacaoHorarioRequest(matriculaId));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<AlocacaoHorarioResponse>();
        corpo!.HorarioId.Should().Be(horarioId);
        corpo.MatriculaId.Should().Be(matriculaId);
    }

    [Fact]
    public async Task Post_Alocacao_ReturnsBadRequest_QuandoHorarioLivre()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId, modeloAgendamento: 0);
        var horarioId = await CriarHorarioAsync(client, professorId, tipoMarcacao: 0);
        var matriculaId = await CriarMatriculaAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/professores/horarios/{horarioId}/alocacoes",
            new CriarAlocacaoHorarioRequest(matriculaId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Alocacao_ReturnsBadRequest_QuandoHorarioLotado()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId);
        var horarioId = await CriarHorarioAsync(client, professorId, limiteAlunos: 1);
        var primeiraMatriculaId = await CriarMatriculaAsync(client);
        await client.PostAsJsonAsync(
            $"/professores/horarios/{horarioId}/alocacoes",
            new CriarAlocacaoHorarioRequest(primeiraMatriculaId));
        var segundaMatriculaId = await CriarMatriculaAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/professores/horarios/{horarioId}/alocacoes",
            new CriarAlocacaoHorarioRequest(segundaMatriculaId));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Alocacao_ReturnsBadRequest_QuandoMatriculaNaoVinculada()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId);
        var horarioId = await CriarHorarioAsync(client, professorId);

        var response = await client.PostAsJsonAsync(
            $"/professores/horarios/{horarioId}/alocacoes",
            new CriarAlocacaoHorarioRequest(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Regressão da checagem de posse (issue #23): uma Matrícula que existe
    /// de fato, mas pertence a OUTRO Professor, é rejeitada como se não
    /// existisse — mesma resposta de "matrícula não vinculada", já que
    /// `professorId` agora vem do token e não pode mais ser adulterado pelo
    /// cliente para apontar para o horário/matrícula de outra pessoa.
    /// </summary>
    [Fact]
    public async Task Post_Alocacao_ReturnsBadRequest_QuandoMatriculaPertenceAOutroProfessor()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId);
        var horarioId = await CriarHorarioAsync(client, professorId);
        var (outroClient, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        var matriculaDeOutroProfessor = await CriarMatriculaAsync(outroClient);

        var response = await client.PostAsJsonAsync(
            $"/professores/horarios/{horarioId}/alocacoes",
            new CriarAlocacaoHorarioRequest(matriculaDeOutroProfessor));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Alocacao_ReturnsNotFound_QuandoHorarioInexistente()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId);
        var matriculaId = await CriarMatriculaAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/professores/horarios/{Guid.NewGuid()}/alocacoes",
            new CriarAlocacaoHorarioRequest(matriculaId));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// Regressão da checagem de posse (issue #23): um `horarioId` que
    /// existe mas pertence a outro Professor é tratado como inexistente
    /// (404), mesma resposta de <c>HorarioNaoEncontradoException</c> — não
    /// se distingue "não existe" de "não é seu".
    /// </summary>
    [Fact]
    public async Task Post_Alocacao_ReturnsNotFound_QuandoHorarioPertenceAOutroProfessor()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId);
        var matriculaId = await CriarMatriculaAsync(client);
        var (outroClient, outroProfessorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(outroClient, outroProfessorId);
        var horarioDeOutroProfessor = await CriarHorarioAsync(outroClient, outroProfessorId);

        var response = await client.PostAsJsonAsync(
            $"/professores/horarios/{horarioDeOutroProfessor}/alocacoes",
            new CriarAlocacaoHorarioRequest(matriculaId));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Alocacoes_ListaAlocacoesDoHorario()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId);
        var horarioId = await CriarHorarioAsync(client, professorId, limiteAlunos: 2);
        var matriculaId = await CriarMatriculaAsync(client);
        await client.PostAsJsonAsync(
            $"/professores/horarios/{horarioId}/alocacoes",
            new CriarAlocacaoHorarioRequest(matriculaId));

        var response = await client.GetAsync($"/professores/horarios/{horarioId}/alocacoes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<AlocacaoHorarioResponse>>();
        corpo.Should().ContainSingle(a => a.MatriculaId == matriculaId);
    }

    [Fact]
    public async Task Delete_Alocacao_ReturnsNoContent_QuandoRemovida()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId);
        var horarioId = await CriarHorarioAsync(client, professorId);
        var matriculaId = await CriarMatriculaAsync(client);
        await client.PostAsJsonAsync(
            $"/professores/horarios/{horarioId}/alocacoes",
            new CriarAlocacaoHorarioRequest(matriculaId));

        var response = await client.DeleteAsync($"/professores/horarios/{horarioId}/alocacoes/{matriculaId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var listagem = await client.GetFromJsonAsync<List<AlocacaoHorarioResponse>>(
            $"/professores/horarios/{horarioId}/alocacoes");
        listagem.Should().BeEmpty();
    }

    [Fact]
    public async Task Delete_Alocacao_ReturnsNotFound_QuandoInexistente()
    {
        var (client, professorId) = await AutenticacaoTestHelper.ClienteAutenticadoComoProfessorPersistidoAsync(_factory);
        await DefinirModeloAgendamentoAsync(client, professorId);
        var horarioId = await CriarHorarioAsync(client, professorId);

        var response = await client.DeleteAsync($"/professores/horarios/{horarioId}/alocacoes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
