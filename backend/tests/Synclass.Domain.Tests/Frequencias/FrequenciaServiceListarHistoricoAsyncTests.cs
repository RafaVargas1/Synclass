using FluentAssertions;
using Synclass.Domain.Alocacoes;
using Synclass.Domain.Aulas;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Configuracoes;
using Synclass.Domain.Frequencias;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Frequencias;

/// <summary>
/// Cobre <see cref="FrequenciaService.ListarHistoricoAsync"/> (issue #16) —
/// consulta 100% leitura, nunca instancia <see cref="Aula"/> como
/// side-effect (ver implementation.md#edge-points). Mesmo padrão de cenário
/// de <see cref="FrequenciaServiceRegistrarAsyncTests"/>.
/// </summary>
public sealed class FrequenciaServiceListarHistoricoAsyncTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero));
    private static readonly Guid AlunoUsuarioId = Guid.NewGuid();
    private static readonly Guid ProfessorId = Guid.NewGuid();

    private sealed record Cenario(
        FrequenciaService FrequenciaService,
        FakeAulaRepository Aulas,
        FakeRegistroFrequenciaRepository Registros,
        FakeCancelamentoAulaRepository Cancelamentos,
        FakeAlocacaoHorarioRepository Alocacoes,
        FakeMatriculaRepository Matriculas,
        FakeHorarioRepository Horarios,
        FakeUsuarioRepository Usuarios);

    private static Cenario CriarCenario()
    {
        var configuracoes = new FakeConfiguracaoProfessorRepository();
        configuracoes.Configuracoes.Add(
            ConfiguracaoProfessor.Criar(ProfessorId, ModeloAgendamento.Vago, Clock));

        var horarios = new FakeHorarioRepository();
        var horarioService = new HorarioService(horarios, Clock);
        var aulas = new FakeAulaRepository();
        var cancelamentos = new FakeCancelamentoAulaRepository();
        var alocacoes = new FakeAlocacaoHorarioRepository();
        var matriculas = new FakeMatriculaRepository();
        var usuarios = new FakeUsuarioRepository();
        var aulaService = new AulaService(
            aulas, cancelamentos, alocacoes, matriculas, configuracoes, horarioService, Clock);
        var alocacaoHorarioService = new AlocacaoHorarioService(alocacoes, matriculas, horarioService, Clock);
        var registros = new FakeRegistroFrequenciaRepository();
        var frequenciaService = new FrequenciaService(
            registros, alocacoes, aulaService, alocacaoHorarioService, cancelamentos, horarioService, Clock,
            matriculas, usuarios, horarios, aulas);
        return new Cenario(frequenciaService, aulas, registros, cancelamentos, alocacoes, matriculas, horarios, usuarios);
    }

    private static async Task<Horario> CriarHorarioAsync(Cenario cenario, DiaSemana diaSemana)
    {
        var horario = Horario.Criar(ProfessorId, diaSemana, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, Clock);
        await cenario.Horarios.AdicionarAsync(horario, CancellationToken.None);
        return horario;
    }

    private static async Task<Matricula> CriarMatriculaVinculadaAlocadaAsync(Cenario cenario, Guid horarioId, Guid alunoUsuarioId)
    {
        var matricula = Matricula.CriarVinculada(ProfessorId, alunoUsuarioId, Clock);
        await cenario.Matriculas.AdicionarAsync(matricula, CancellationToken.None);
        var alocacao = AlocacaoHorario.Criar(horarioId, matricula.Id, OrigemAlocacao.Aluno, Clock);
        await cenario.Alocacoes.AdicionarAsync(alocacao, CancellationToken.None);
        return matricula;
    }

    /// <summary>
    /// AC5 — 1 aula agendada (terça, dentro do período) e nenhum registro
    /// nem `Aula` ainda: status `NaoRegistrada`, nunca `Ausente` por padrão.
    /// </summary>
    [Fact]
    public async Task ListarHistoricoAsync_AulaAgendadaSemNenhumRegistro_StatusNaoRegistrada()
    {
        var cenario = CriarCenario();
        var horario = await CriarHorarioAsync(cenario, DiaSemana.Terca);
        await CriarMatriculaVinculadaAlocadaAsync(cenario, horario.Id, AlunoUsuarioId);
        var periodo = PeriodoConsulta.Criar(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1));

        var historico = await cenario.FrequenciaService.ListarHistoricoAsync(AlunoUsuarioId, periodo, CancellationToken.None);

        historico.Should().ContainSingle();
        historico.Single().Aulas.Should().OnlyContain(a => a.Status == StatusHistoricoFrequencia.NaoRegistrada);
        cenario.Aulas.Aulas.Should().BeEmpty("consulta é 100% leitura, nunca cria Aula como side-effect");
    }

    /// <summary>
    /// AC2 — Professor registrou frequência (issue #14) para uma aula:
    /// histórico reflete `StatusProfessor`.
    /// </summary>
    [Fact]
    public async Task ListarHistoricoAsync_ProfessorRegistrouPresenca_HistoricoReflete()
    {
        var cenario = CriarCenario();
        var horario = await CriarHorarioAsync(cenario, DiaSemana.Terca);
        var matricula = await CriarMatriculaVinculadaAlocadaAsync(cenario, horario.Id, AlunoUsuarioId);
        var data = new DateOnly(2026, 8, 4);
        var aula = Aula.Criar(horario.Id, data, Clock);
        await cenario.Aulas.AdicionarAsync(aula, CancellationToken.None);
        var registro = RegistroFrequencia.Criar(aula.Id, matricula.Id, Clock);
        registro.RegistrarProfessor(StatusFrequencia.Presente, Clock);
        await cenario.Registros.AdicionarAsync(registro, CancellationToken.None);
        var periodo = PeriodoConsulta.Criar(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1));

        var historico = await cenario.FrequenciaService.ListarHistoricoAsync(AlunoUsuarioId, periodo, CancellationToken.None);

        historico.Single().Aulas.Should().Contain(a => a.Data == data && a.Status == StatusHistoricoFrequencia.Presente);
    }

    /// <summary>
    /// AC3 — Aluno confirmou presença (issue #15) e Professor também marcou
    /// presente: histórico mostra `Presente` (mesmo fato por duas fontes).
    /// </summary>
    [Fact]
    public async Task ListarHistoricoAsync_AlunoConfirmouEProfessorMarcouPresente_StatusPresente()
    {
        var cenario = CriarCenario();
        var horario = await CriarHorarioAsync(cenario, DiaSemana.Terca);
        var matricula = await CriarMatriculaVinculadaAlocadaAsync(cenario, horario.Id, AlunoUsuarioId);
        var data = new DateOnly(2026, 8, 4);
        var aula = Aula.Criar(horario.Id, data, Clock);
        await cenario.Aulas.AdicionarAsync(aula, CancellationToken.None);
        var registro = RegistroFrequencia.CriarComConfirmacaoDoAluno(aula.Id, matricula.Id, confirmadoPeloAluno: true, Clock);
        registro.RegistrarProfessor(StatusFrequencia.Presente, Clock);
        await cenario.Registros.AdicionarAsync(registro, CancellationToken.None);
        var periodo = PeriodoConsulta.Criar(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1));

        var historico = await cenario.FrequenciaService.ListarHistoricoAsync(AlunoUsuarioId, periodo, CancellationToken.None);

        historico.Single().Aulas.Should().Contain(a => a.Data == data && a.Status == StatusHistoricoFrequencia.Presente);
    }

    /// <summary>
    /// AC4 — divergência: Aluno confirmou presente, Professor marcou
    /// ausente. Histórico mostra `Ausente`, sem indicar conflito —
    /// `StatusProfessor` prevalece.
    /// </summary>
    [Fact]
    public async Task ListarHistoricoAsync_DivergenciaEntreAlunoEProfessor_PrevaleceStatusDoProfessor()
    {
        var cenario = CriarCenario();
        var horario = await CriarHorarioAsync(cenario, DiaSemana.Terca);
        var matricula = await CriarMatriculaVinculadaAlocadaAsync(cenario, horario.Id, AlunoUsuarioId);
        var data = new DateOnly(2026, 8, 4);
        var aula = Aula.Criar(horario.Id, data, Clock);
        await cenario.Aulas.AdicionarAsync(aula, CancellationToken.None);
        var registro = RegistroFrequencia.CriarComConfirmacaoDoAluno(aula.Id, matricula.Id, confirmadoPeloAluno: true, Clock);
        registro.RegistrarProfessor(StatusFrequencia.Ausente, Clock);
        await cenario.Registros.AdicionarAsync(registro, CancellationToken.None);
        var periodo = PeriodoConsulta.Criar(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1));

        var historico = await cenario.FrequenciaService.ListarHistoricoAsync(AlunoUsuarioId, periodo, CancellationToken.None);

        historico.Single().Aulas.Should().Contain(a => a.Data == data && a.Status == StatusHistoricoFrequencia.Ausente);
    }

    /// <summary>
    /// Edge point — aula cancelada pelo próprio Aluno (issue #10): status
    /// `Cancelada`, não aparece como `NaoRegistrada` nem `Ausente`, mesmo
    /// que exista um `RegistroFrequencia` associado.
    /// </summary>
    [Fact]
    public async Task ListarHistoricoAsync_AulaCanceladaPeloAluno_StatusCancelada()
    {
        var cenario = CriarCenario();
        var horario = await CriarHorarioAsync(cenario, DiaSemana.Terca);
        var matricula = await CriarMatriculaVinculadaAlocadaAsync(cenario, horario.Id, AlunoUsuarioId);
        var data = new DateOnly(2026, 8, 4);
        var aula = Aula.Criar(horario.Id, data, Clock);
        await cenario.Aulas.AdicionarAsync(aula, CancellationToken.None);
        var cancelamento = CancelamentoAula.Criar(aula.Id, matricula.Id, Clock);
        await cenario.Cancelamentos.AdicionarAsync(cancelamento, CancellationToken.None);
        var periodo = PeriodoConsulta.Criar(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1));

        var historico = await cenario.FrequenciaService.ListarHistoricoAsync(AlunoUsuarioId, periodo, CancellationToken.None);

        historico.Single().Aulas.Should().Contain(a => a.Data == data && a.Status == StatusHistoricoFrequencia.Cancelada);
    }

    /// <summary>
    /// Aluno com matrículas em mais de um Professor: histórico agrupado por
    /// Professor, sem misturar/somar entre eles (mesma RN de
    /// <see cref="ConsultaCobrancaService"/>, issue #13).
    /// </summary>
    [Fact]
    public async Task ListarHistoricoAsync_AlunoComDoisProfessores_AgrupaSemMisturar()
    {
        var cenario = CriarCenario();
        var professorDois = Guid.NewGuid();
        var horarioUm = await CriarHorarioAsync(cenario, DiaSemana.Terca);
        var horarioDois = Horario.Criar(professorDois, DiaSemana.Quarta, new TimeOnly(11, 0), 60, TipoMarcacao.Livre, Clock);
        await cenario.Horarios.AdicionarAsync(horarioDois, CancellationToken.None);
        await CriarMatriculaVinculadaAlocadaAsync(cenario, horarioUm.Id, AlunoUsuarioId);

        var matriculaDois = Matricula.CriarVinculada(professorDois, AlunoUsuarioId, Clock);
        await cenario.Matriculas.AdicionarAsync(matriculaDois, CancellationToken.None);
        var alocacaoDois = AlocacaoHorario.Criar(horarioDois.Id, matriculaDois.Id, OrigemAlocacao.Aluno, Clock);
        await cenario.Alocacoes.AdicionarAsync(alocacaoDois, CancellationToken.None);

        var periodo = PeriodoConsulta.Criar(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1));

        var historico = await cenario.FrequenciaService.ListarHistoricoAsync(AlunoUsuarioId, periodo, CancellationToken.None);

        historico.Should().HaveCount(2);
        historico.Should().Contain(h => h.ProfessorId == ProfessorId && h.Aulas.All(a => a.DiaSemana == DiaSemana.Terca));
        historico.Should().Contain(h => h.ProfessorId == professorDois && h.Aulas.All(a => a.DiaSemana == DiaSemana.Quarta));
    }
}
