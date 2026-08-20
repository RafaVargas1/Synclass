using FluentAssertions;
using Synclass.Domain.Alocacoes;
using Synclass.Domain.Aulas;
using Synclass.Domain.Configuracoes;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Aulas;

/// <summary>
/// Cobre <see cref="AulaService.ListarProximasAsync"/> (issue #10) — calcula
/// a próxima ocorrência futura de cada <c>AlocacaoHorario</c> da matrícula,
/// sem exigir <c>Aula</c> pré-existente, pulando ocorrências já canceladas.
/// Mesmo padrão de cenário de <see cref="AulaServiceCancelarAsyncTests"/>.
/// </summary>
public sealed class AulaServiceListarProximasAsyncTests
{
    // Terça-feira, 12:00 — usado como "agora" em todos os testes.
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
        var horarioService = new HorarioService(horarios, Clock);
        var aulas = new FakeAulaRepository();
        var cancelamentos = new FakeCancelamentoAulaRepository();
        var alocacoes = new FakeAlocacaoHorarioRepository();
        var matriculas = new FakeMatriculaRepository();
        var aulaService = new AulaService(
            aulas, cancelamentos, alocacoes, matriculas, configuracoes, horarioService, Clock);
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
    public async Task ListarProximasAsync_HorarioAindaNaoTemAulaInstanciada_CalculaAProximaOcorrenciaMesmoAssim()
    {
        var cenario = CriarCenario();
        // Terça 10:00 já passou hoje (agora é terça 12:00) -> próxima é a
        // terça seguinte, 25/08.
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = await CriarMatriculaAlocadaAsync(cenario, horario.Id);

        var proximas = await cenario.AulaService.ListarProximasAsync(ProfessorId, matricula.Id, CancellationToken.None);

        proximas.Should().ContainSingle();
        proximas.Single().HorarioId.Should().Be(horario.Id);
        proximas.Single().Data.Should().Be(new DateOnly(2026, 8, 25));
    }

    [Fact]
    public async Task ListarProximasAsync_MesmoDiaComHoraAindaNaoIniciada_UsaAOcorrenciaDeHoje()
    {
        var cenario = CriarCenario();
        // Terça 18:00 ainda não começou hoje (agora é terça 12:00).
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(18, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = await CriarMatriculaAlocadaAsync(cenario, horario.Id);

        var proximas = await cenario.AulaService.ListarProximasAsync(ProfessorId, matricula.Id, CancellationToken.None);

        proximas.Single().Data.Should().Be(new DateOnly(2026, 8, 18));
    }

    [Fact]
    public async Task ListarProximasAsync_OcorrenciaMaisProximaJaCancelada_PulaParaASeguinte()
    {
        var cenario = CriarCenario();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(18, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = await CriarMatriculaAlocadaAsync(cenario, horario.Id);
        await cenario.AulaService.CancelarAsync(
            ProfessorId, horario.Id, new DateOnly(2026, 8, 18), matricula.Id, CancellationToken.None);

        var proximas = await cenario.AulaService.ListarProximasAsync(ProfessorId, matricula.Id, CancellationToken.None);

        proximas.Single().Data.Should().Be(new DateOnly(2026, 8, 25));
    }

    [Fact]
    public async Task ListarProximasAsync_DuasAlocacoesEmHorariosDiferentes_RetornaUmaOcorrenciaPorAlocacao()
    {
        var cenario = CriarCenario();
        var primeiroHorario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(18, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var segundoHorario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Quarta, new TimeOnly(9, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = Matricula.CriarProvisoria(ProfessorId, "Aluno Um", $"aluno-{Guid.NewGuid()}", Clock);
        await cenario.Matriculas.AdicionarAsync(matricula, CancellationToken.None);
        await cenario.Alocacoes.AdicionarAsync(
            AlocacaoHorario.Criar(primeiroHorario.Id, matricula.Id, OrigemAlocacao.Aluno, Clock), CancellationToken.None);
        await cenario.Alocacoes.AdicionarAsync(
            AlocacaoHorario.Criar(segundoHorario.Id, matricula.Id, OrigemAlocacao.Aluno, Clock), CancellationToken.None);

        var proximas = await cenario.AulaService.ListarProximasAsync(ProfessorId, matricula.Id, CancellationToken.None);

        proximas.Should().HaveCount(2);
        proximas.Should().Contain(a => a.HorarioId == primeiroHorario.Id);
        proximas.Should().Contain(a => a.HorarioId == segundoHorario.Id);
    }

    [Fact]
    public async Task ListarProximasAsync_ForaDoPrazoDeCancelamento_PodeCancelarFalse()
    {
        var cenario = CriarCenario(prazoCancelamentoMinutos: 24 * 60);
        // Terça 13:00 (hoje) = 1h de antecedência, prazo exige 24h.
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(13, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = await CriarMatriculaAlocadaAsync(cenario, horario.Id);

        var proximas = await cenario.AulaService.ListarProximasAsync(ProfessorId, matricula.Id, CancellationToken.None);

        proximas.Single().PodeCancelar.Should().BeFalse();
    }
}
