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
/// Teste de fumaça dos endpoints de horários disponíveis (issue #6):
/// criar, listar e remover. Usa EF Core InMemory, mesmo padrão de
/// <see cref="ProfessorCadastroEndpointTests"/>. Também cobre o PATCH de
/// alteração da política de marcação (issue #71).
/// </summary>
public sealed class HorarioEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HorarioEndpointTests(WebApplicationFactory<Program> factory)
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
    /// Cria um Professor e já define o modelo de agendamento (Vago) — a
    /// partir da issue #7, <c>HorarioService.CadastrarAsync</c> exige essa
    /// configuração antes de aceitar o cadastro de um horário. O caso sem
    /// configuração é coberto separadamente em
    /// <see cref="ConfiguracaoEndpointTests.Post_Horario_ReturnsBadRequest_QuandoProfessorNaoDefiniuModelo"/>.
    /// </summary>
    private static async Task<Guid> CriarProfessorAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/professores/cadastro",
            new CadastroUsuarioRequest("Maria Silva", $"{Guid.NewGuid()}@exemplo.com"));
        var corpo = await response.Content.ReadFromJsonAsync<CadastroUsuarioResponse>();
        var professorId = corpo!.UsuarioId;

        await client.PutAsJsonAsync(
            $"/professores/{professorId}/configuracao/modelo-agendamento",
            new DefinirModeloAgendamentoRequest(ModeloAgendamento: 0));

        return professorId;
    }

    [Fact]
    public async Task Post_Horario_ReturnsOk_QuandoDadosValidos()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: 0));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<HorarioResponse>();
        corpo!.DuracaoMinutos.Should().Be(60);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Post_Horario_ReturnsOk_QuandoTipoMarcacaoValido(int tipoMarcacao)
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: tipoMarcacao));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<HorarioResponse>();
        corpo!.TipoMarcacao.Should().Be(tipoMarcacao);
    }

    [Fact]
    public async Task Post_Horario_ReturnsBadRequest_QuandoTipoMarcacaoInvalido()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: 99));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Horario_SemInformarLimiteAlunos_AplicaDefault1()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: 0));

        var corpo = await response.Content.ReadFromJsonAsync<HorarioResponse>();
        corpo!.LimiteAlunos.Should().Be(1);
    }

    [Fact]
    public async Task Post_Horario_ComLimiteAlunosInformado_UsaOValorInformado()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: 0, LimiteAlunos: 4));

        var corpo = await response.Content.ReadFromJsonAsync<HorarioResponse>();
        corpo!.LimiteAlunos.Should().Be(4);
    }

    [Fact]
    public async Task Post_Horario_ReturnsBadRequest_QuandoLimiteAlunosZeroOuNegativo()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: 0, LimiteAlunos: 0));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Horario_ReturnsBadRequest_QuandoDuracaoInvalida()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 0, TipoMarcacao: 0));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Horario_ReturnsBadRequest_QuandoDiaSemanaInvalido()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 99, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: 0));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_Horario_ReturnsBadRequest_QuandoConflitaComHorarioExistente()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);
        await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: 0));

        var response = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 30), DuracaoMinutos: 60, TipoMarcacao: 0));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var corpo = await response.Content.ReadFromJsonAsync<HorarioErrorResponse>();
        corpo!.Mensagem.Should().Contain("conflita");
    }

    [Fact]
    public async Task Get_Horarios_ListaHorariosCadastrados()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);
        await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: 0));

        var response = await client.GetAsync($"/professores/{professorId}/horarios");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<HorarioResponse>>();
        corpo.Should().ContainSingle();
    }

    [Fact]
    public async Task Delete_Horario_ReturnsNoContent_QuandoRemovido()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);
        var criado = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: 0));
        var horario = await criado.Content.ReadFromJsonAsync<HorarioResponse>();

        var response = await client.DeleteAsync($"/professores/{professorId}/horarios/{horario!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_Horario_ReturnsNotFound_QuandoHorarioNaoExiste()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);

        var response = await client.DeleteAsync($"/professores/{professorId}/horarios/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_Horario_TipoMarcacao_ReturnsOkComHorarioAtualizado_QuandoPoliticaMuda()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);
        var criado = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: 0));
        var horario = await criado.Content.ReadFromJsonAsync<HorarioResponse>();

        var response = await client.PatchAsJsonAsync(
            $"/professores/{professorId}/horarios/{horario!.Id}",
            new AlterarPoliticaHorarioRequest(TipoMarcacao: 1));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<HorarioResponse>();
        corpo!.TipoMarcacao.Should().Be(1);
        corpo.Id.Should().Be(horario.Id);
    }

    [Fact]
    public async Task Patch_Horario_TipoMarcacao_ReturnsNotFound_QuandoHorarioNaoExiste()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);

        var response = await client.PatchAsJsonAsync(
            $"/professores/{professorId}/horarios/{Guid.NewGuid()}",
            new AlterarPoliticaHorarioRequest(TipoMarcacao: 1));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_Horario_TipoMarcacao_ReturnsNotFound_QuandoHorarioDeOutroProfessor()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);
        var criado = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: 0));
        var horario = await criado.Content.ReadFromJsonAsync<HorarioResponse>();
        var outroProfessorId = await CriarProfessorAsync(client);

        var response = await client.PatchAsJsonAsync(
            $"/professores/{outroProfessorId}/horarios/{horario!.Id}",
            new AlterarPoliticaHorarioRequest(TipoMarcacao: 1));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_Horario_TipoMarcacao_ReturnsBadRequest_QuandoTipoMarcacaoForaDoEnum()
    {
        var client = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorAsync(client);
        var criado = await client.PostAsJsonAsync(
            $"/professores/{professorId}/horarios",
            new CriarHorarioRequest(DiaSemana: 2, HoraInicio: new TimeOnly(10, 0), DuracaoMinutos: 60, TipoMarcacao: 0));
        var horario = await criado.Content.ReadFromJsonAsync<HorarioResponse>();

        var response = await client.PatchAsJsonAsync(
            $"/professores/{professorId}/horarios/{horario!.Id}",
            new AlterarPoliticaHorarioRequest(TipoMarcacao: 99));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
