using FluentAssertions;
using Synclass.Domain.Alocacoes;
using Synclass.Domain.Aulas;
using Synclass.Domain.Configuracoes;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Aulas;

/// <summary>
/// Cobre <see cref="AulaService.CancelarAsync"/> (issue #10) — o Aluno
/// desmarca uma aula já alocada, respeitando o prazo configurado pelo
/// Professor. Mesmo padrão de cenário de
/// <c>AlocacaoHorarioServiceMarcarAsyncTests</c> (issue #9).
/// </summary>
public sealed class AulaServiceCancelarAsyncTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero));
    private static readonly Guid ProfessorId = Guid.NewGuid();

    private sealed record Cenario(
        AulaService AulaService,
        FakeAulaRepository Aulas,
        FakeCancelamentoAulaRepository Cancelamentos,
        FakeAlocacaoHorarioRepository Alocacoes,
        FakeMatriculaRepository Matriculas,
        FakeConfiguracaoProfessorRepository Configuracoes,
        HorarioService HorarioService);

    private static Cenario CriarCenario(int prazoCancelamentoMinutos = 0)
    {
        var configuracoes = new FakeConfiguracaoProfessorRepository();
        configuracoes.Configuracoes.Add(
            ConfiguracaoProfessor.Criar(ProfessorId, ModeloAgendamento.Vago, Clock, prazoCancelamentoMinutos));

        var horarios = new FakeHorarioRepository();
        var horarioService = new HorarioService(horarios, configuracoes, Clock);
        var aulas = new FakeAulaRepository();
        var cancelamentos = new FakeCancelamentoAulaRepository();
        var alocacoes = new FakeAlocacaoHorarioRepository();
        var matriculas = new FakeMatriculaRepository();
        var aulaService = new AulaService(
            aulas, cancelamentos, alocacoes, configuracoes, horarioService, Clock);
        return new Cenario(aulaService, aulas, cancelamentos, alocacoes, matriculas, configuracoes, horarioService);
    }

    private static async Task<Matricula> CriarMatriculaAlocadaAsync(Cenario cenario, Guid horarioId)
    {
        var matricula = Matricula.CriarProvisoria(ProfessorId, "Aluno Um", $"aluno-{Guid.NewGuid()}", Clock);
        await cenario.Matriculas.AdicionarAsync(matricula, CancellationToken.None);
        var alocacao = AlocacaoHorario.Criar(horarioId, matricula.Id, OrigemAlocacao.Aluno, Clock);
        await cenario.Alocacoes.AdicionarAsync(alocacao, CancellationToken.None);
        return matricula;
    }

    [Fact]
    public async Task CancelarAsync_AulaAindaNaoExisteParaEssaData_InstanciaSobDemanda()
    {
        var cenario = CriarCenario();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);
        var matricula = await CriarMatriculaAlocadaAsync(cenario, horario.Id);
        var data = new DateOnly(2026, 8, 20);

        await cenario.AulaService.CancelarAsync(ProfessorId, horario.Id, data, matricula.Id, CancellationToken.None);

        cenario.Aulas.Aulas.Should().ContainSingle(a => a.HorarioId == horario.Id && a.Data == data);
    }
}
