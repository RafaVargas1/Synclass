using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Synclass.Api.Controllers;
using Synclass.Api.Tests.Fakes;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Common;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Pagamentos;
using Synclass.Domain.Usuarios;
using Synclass.Infrastructure.Persistence;

namespace Synclass.Api.Tests;

/// <summary>
/// Teste de fumaça do endpoint de consulta de valor devido do Aluno (issue
/// #13): mês corrente como default, um vínculo por Professor sem soma,
/// vínculo sem regra, Aluno sem matrícula e request inválido — mesmo padrão
/// de <see cref="ValorDevidoEndpointTests"/> (EF Core InMemory), mas
/// autenticado/identificado via token (não parâmetro de rota).
/// </summary>
public sealed class ValorDevidoAlunoEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ValorDevidoAlunoEndpointTests(WebApplicationFactory<Program> factory)
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

    private async Task<Guid> CriarProfessorPersistidoAsync(string nome)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var professor = Usuario.Cadastrar(nome, $"{Guid.NewGuid()}@exemplo.com", PapelUsuario.Professor, null, clock);
        dbContext.Usuarios.Add(professor);
        await dbContext.SaveChangesAsync();
        return professor.Id;
    }

    private async Task<Guid> VincularMatriculaAsync(Guid professorId, Guid alunoUsuarioId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var matricula = Matricula.CriarVinculada(professorId, alunoUsuarioId, clock);
        dbContext.Matriculas.Add(matricula);
        await dbContext.SaveChangesAsync();
        return matricula.Id;
    }

    private async Task DefinirRegraFixoMensalAsync(HttpClient clientProfessor, Guid professorId, Guid matriculaId, decimal valor)
    {
        await clientProfessor.PutAsJsonAsync(
            $"/professores/{professorId}/matriculas/{matriculaId}/regra-de-cobranca",
            new DefinirRegraDeCobrancaRequest("FixoMensal", valor, null));
    }

    [Fact]
    public async Task Get_ValorDevidoAluno_SemInicioNemFim_UsaMesCorrenteEDevolveOkComListaDeProfessores()
    {
        var (client, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var professorId = await CriarProfessorPersistidoAsync("Professor Um");
        await VincularMatriculaAsync(professorId, alunoUsuarioId);

        var response = await client.GetAsync("/alunos/valor-devido");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<ValorDevidoResponse>>();
        corpo.Should().ContainSingle();
    }

    [Fact]
    public async Task Get_ValorDevidoAluno_DoisVinculosComRegrasDiferentes_UmaEntradaPorProfessorSemSomar()
    {
        var (client, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var clientProfessor = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorA = await CriarProfessorPersistidoAsync("Professor A");
        var professorB = await CriarProfessorPersistidoAsync("Professor B");
        var matriculaComA = await VincularMatriculaAsync(professorA, alunoUsuarioId);
        var matriculaComB = await VincularMatriculaAsync(professorB, alunoUsuarioId);
        await DefinirRegraFixoMensalAsync(clientProfessor, professorA, matriculaComA, 300m);
        await DefinirRegraFixoMensalAsync(clientProfessor, professorB, matriculaComB, 500m);

        var response = await client.GetAsync("/alunos/valor-devido");

        var corpo = await response.Content.ReadFromJsonAsync<List<ValorDevidoResponse>>();
        corpo.Should().HaveCount(2);
        corpo.Should().ContainSingle(v => v.MatriculaId == matriculaComA && v.Valor == 300m && v.Nome == "Professor A");
        corpo.Should().ContainSingle(v => v.MatriculaId == matriculaComB && v.Valor == 500m && v.Nome == "Professor B");
    }

    [Fact]
    public async Task Get_ValorDevidoAluno_VinculoSemRegra_AparaceComSemRegraDefinidaTrueEValorNull()
    {
        var (client, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var professorId = await CriarProfessorPersistidoAsync("Professor Sem Regra");
        var matriculaId = await VincularMatriculaAsync(professorId, alunoUsuarioId);

        var response = await client.GetAsync("/alunos/valor-devido");

        var corpo = await response.Content.ReadFromJsonAsync<List<ValorDevidoResponse>>();
        var valorDevido = corpo.Should().ContainSingle(v => v.MatriculaId == matriculaId).Subject;
        valorDevido.SemRegraDefinida.Should().BeTrue();
        valorDevido.Valor.Should().BeNull();
    }

    [Fact]
    public async Task Get_ValorDevidoAluno_AlunoSemNenhumaMatricula_DevolveOkComListaVazia()
    {
        var (client, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);

        var response = await client.GetAsync("/alunos/valor-devido");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var corpo = await response.Content.ReadFromJsonAsync<List<ValorDevidoResponse>>();
        corpo.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_ValorDevidoAluno_SoInicioInformado_DevolveBadRequest()
    {
        var (client, _) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);

        var response = await client.GetAsync("/alunos/valor-devido?inicio=2026-08-01");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <summary>
    /// Matrícula com um <see cref="Pagamento"/> <c>Confirmado</c> no período
    /// consultado sai do valor devido (issue #199) — o desconto é aplicado
    /// no fluxo do endpoint <c>GET /alunos/valor-devido</c>, não no
    /// <c>ConsultaCobrancaService</c> (Professor usa o mesmo serviço sem
    /// desconto nesta Task). Teste na camada de serviço (endpoint real, não
    /// HTTP puro).
    /// </summary>
    [Fact]
    public async Task Get_ValorDevidoAluno_MatriculaComPagamentoConfirmadoNoPeriodo_DescontaDaLista()
    {
        var (client, alunoUsuarioId) = await AutenticacaoTestHelper.ClienteAutenticadoComoAlunoPersistidoAsync(_factory);
        var clientProfessor = AutenticacaoTestHelper.ClienteAutenticadoComoProfessor(_factory);
        var professorId = await CriarProfessorPersistidoAsync("Professor Pago");
        var matriculaId = await VincularMatriculaAsync(professorId, alunoUsuarioId);
        await DefinirRegraFixoMensalAsync(clientProfessor, professorId, matriculaId, 300m);
        await PersistirPagamentoConfirmadoAsync(matriculaId, alunoUsuarioId, professorId);

        var response = await client.GetAsync("/alunos/valor-devido?inicio=2026-08-01&fim=2026-09-01");

        var corpo = await response.Content.ReadFromJsonAsync<List<ValorDevidoResponse>>();
        corpo!.Should().NotContain(v => v.MatriculaId == matriculaId);
    }

    private async Task PersistirPagamentoConfirmadoAsync(Guid matriculaId, Guid alunoUsuarioId, Guid professorId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SynclassDbContext>();
        var pagamento = new Pagamento(
            Guid.NewGuid(), matriculaId, alunoUsuarioId, professorId, 300m,
            new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1),
            "https://checkout.mercadopago.com/pref-pago", "pref-pago");
        pagamento.Confirmar();
        dbContext.Pagamentos.Add(pagamento);
        await dbContext.SaveChangesAsync();
    }
}
