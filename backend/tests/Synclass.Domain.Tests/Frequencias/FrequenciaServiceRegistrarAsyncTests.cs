using FluentAssertions;
using Synclass.Domain.Alocacoes;
using Synclass.Domain.Aulas;
using Synclass.Domain.Configuracoes;
using Synclass.Domain.Frequencias;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Frequencias;

/// <summary>
/// Cobre <see cref="FrequenciaService.RegistrarAsync"/> (issue #14) — o
/// Professor registra presença/ausência em lote para os Alunos alocados
/// num horário/data. Mesmo padrão de cenário de
/// <c>AulaServiceCancelarAsyncTests</c> (issue #10).
/// </summary>
public sealed class FrequenciaServiceRegistrarAsyncTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero));
    private static readonly Guid ProfessorId = Guid.NewGuid();

    private sealed record Cenario(
        FrequenciaService FrequenciaService,
        FakeAulaRepository Aulas,
        FakeRegistroFrequenciaRepository Registros,
        FakeAlocacaoHorarioRepository Alocacoes,
        FakeMatriculaRepository Matriculas,
        HorarioService HorarioService);

    private static Cenario CriarCenario()
    {
        var configuracoes = new FakeConfiguracaoProfessorRepository();
        configuracoes.Configuracoes.Add(
            ConfiguracaoProfessor.Criar(ProfessorId, ModeloAgendamento.Vago, Clock));

        var horarios = new FakeHorarioRepository();
        var horarioService = new HorarioService(horarios, configuracoes, Clock);
        var aulas = new FakeAulaRepository();
        var cancelamentos = new FakeCancelamentoAulaRepository();
        var alocacoes = new FakeAlocacaoHorarioRepository();
        var matriculas = new FakeMatriculaRepository();
        var aulaService = new AulaService(
            aulas, cancelamentos, alocacoes, matriculas, configuracoes, horarioService, Clock);
        var registros = new FakeRegistroFrequenciaRepository();
        var frequenciaService = new FrequenciaService(registros, alocacoes, aulaService, horarioService, Clock);
        return new Cenario(frequenciaService, aulas, registros, alocacoes, matriculas, horarioService);
    }

    private static async Task<Matricula> CriarMatriculaAlocadaAsync(Cenario cenario, Guid horarioId)
    {
        var matricula = Matricula.CriarProvisoria(ProfessorId, "Aluno Um", $"aluno-{Guid.NewGuid()}", Clock);
        await cenario.Matriculas.AdicionarAsync(matricula, CancellationToken.None);
        var alocacao = AlocacaoHorario.Criar(horarioId, matricula.Id, OrigemAlocacao.Aluno, Clock);
        await cenario.Alocacoes.AdicionarAsync(alocacao, CancellationToken.None);
        return matricula;
    }

    /// <summary>
    /// AC1 — N Alunos alocados, Professor registra presente/ausente para
    /// cada um: cada Aluno recebe seu próprio `RegistroFrequencia`.
    /// </summary>
    [Fact]
    public async Task RegistrarAsync_NenhumRegistroExistiaAinda_CriaUmRegistroParaCadaAlunoAlocado()
    {
        var cenario = CriarCenario();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None, limiteAlunos: 2);
        var alunoPresente = await CriarMatriculaAlocadaAsync(cenario, horario.Id);
        var alunoAusente = await CriarMatriculaAlocadaAsync(cenario, horario.Id);
        var data = new DateOnly(2026, 8, 20);
        var statusPorMatricula = new Dictionary<Guid, StatusFrequencia>
        {
            [alunoPresente.Id] = StatusFrequencia.Presente,
            [alunoAusente.Id] = StatusFrequencia.Ausente,
        };

        var registros = await cenario.FrequenciaService.RegistrarAsync(
            ProfessorId, horario.Id, data, statusPorMatricula, CancellationToken.None);

        registros.Should().HaveCount(2);
        cenario.Registros.Registros.Should().Contain(
            r => r.MatriculaId == alunoPresente.Id && r.StatusProfessor == StatusFrequencia.Presente);
        cenario.Registros.Registros.Should().Contain(
            r => r.MatriculaId == alunoAusente.Id && r.StatusProfessor == StatusFrequencia.Ausente);
    }

    /// <summary>
    /// Edge point — registrar para uma data ainda não referenciada (nenhum
    /// cancelamento nem registro anterior tocou essa ocorrência) instancia a
    /// `Aula` sob demanda antes de gravar o registro, mesmo mecanismo da
    /// issue #10 (`AulaService.ObterOuCriarAulaAsync`).
    /// </summary>
    [Fact]
    public async Task RegistrarAsync_AulaAindaNaoExisteParaEssaData_InstanciaSobDemanda()
    {
        var cenario = CriarCenario();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);
        var matricula = await CriarMatriculaAlocadaAsync(cenario, horario.Id);
        var data = new DateOnly(2026, 8, 20);
        var statusPorMatricula = new Dictionary<Guid, StatusFrequencia> { [matricula.Id] = StatusFrequencia.Presente };

        await cenario.FrequenciaService.RegistrarAsync(
            ProfessorId, horario.Id, data, statusPorMatricula, CancellationToken.None);

        cenario.Aulas.Aulas.Should().ContainSingle(a => a.HorarioId == horario.Id && a.Data == data);
    }

    /// <summary>
    /// AC2 — o Aluno já confirmou presença (issue #15) antes do Professor
    /// registrar: marcar o mesmo Aluno como presente reconcilia com a linha
    /// existente (mesmo fato, duas perspectivas), sem criar uma segunda.
    /// </summary>
    [Fact]
    public async Task RegistrarAsync_AlunoJaConfirmouPresencaEProfessorMarcaPresente_AtualizaAMesmaLinha()
    {
        var cenario = CriarCenario();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);
        var matricula = await CriarMatriculaAlocadaAsync(cenario, horario.Id);
        var data = new DateOnly(2026, 8, 20);
        var aula = Aula.Criar(horario.Id, data, Clock);
        await cenario.Aulas.AdicionarAsync(aula, CancellationToken.None);
        var confirmacaoDoAluno = RegistroFrequencia.CriarComConfirmacaoDoAluno(aula.Id, matricula.Id, confirmadoPeloAluno: true, Clock);
        await cenario.Registros.AdicionarAsync(confirmacaoDoAluno, CancellationToken.None);
        var statusPorMatricula = new Dictionary<Guid, StatusFrequencia> { [matricula.Id] = StatusFrequencia.Presente };

        var registros = await cenario.FrequenciaService.RegistrarAsync(
            ProfessorId, horario.Id, data, statusPorMatricula, CancellationToken.None);

        cenario.Registros.Registros.Should().ContainSingle(r => r.MatriculaId == matricula.Id);
        registros.Should().ContainSingle(r => r.Id == confirmacaoDoAluno.Id);
        var registro = registros.Single();
        registro.StatusProfessor.Should().Be(StatusFrequencia.Presente);
        registro.ConfirmadoPeloAluno.Should().BeTrue();
    }

    /// <summary>
    /// AC3 — divergência: Aluno confirmou presente, Professor marca ausente.
    /// Ambos os valores são preservados na mesma linha (auditoria) — o
    /// Professor decide a "verdade" oficial (`StatusProfessor`), mas
    /// `ConfirmadoPeloAluno` não é sobrescrito.
    /// </summary>
    [Fact]
    public async Task RegistrarAsync_AlunoConfirmouPresenteEProfessorMarcaAusente_PreservaOsDoisValores()
    {
        var cenario = CriarCenario();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);
        var matricula = await CriarMatriculaAlocadaAsync(cenario, horario.Id);
        var data = new DateOnly(2026, 8, 20);
        var aula = Aula.Criar(horario.Id, data, Clock);
        await cenario.Aulas.AdicionarAsync(aula, CancellationToken.None);
        var confirmacaoDoAluno = RegistroFrequencia.CriarComConfirmacaoDoAluno(aula.Id, matricula.Id, confirmadoPeloAluno: true, Clock);
        await cenario.Registros.AdicionarAsync(confirmacaoDoAluno, CancellationToken.None);
        var statusPorMatricula = new Dictionary<Guid, StatusFrequencia> { [matricula.Id] = StatusFrequencia.Ausente };

        var registros = await cenario.FrequenciaService.RegistrarAsync(
            ProfessorId, horario.Id, data, statusPorMatricula, CancellationToken.None);

        var registro = registros.Single();
        registro.StatusProfessor.Should().Be(StatusFrequencia.Ausente);
        registro.ConfirmadoPeloAluno.Should().BeTrue();
        cenario.Registros.Registros.Should().ContainSingle(r => r.MatriculaId == matricula.Id);
    }

    /// <summary>
    /// AC4 — registrar de novo para a mesma (Aula, Matricula) sobrescreve o
    /// `StatusProfessor` anterior (upsert), não duplica a linha.
    /// </summary>
    [Fact]
    public async Task RegistrarAsync_RegistrarDeNovoParaMesmaAulaEMatricula_SobrescreveOStatusAnterior()
    {
        var cenario = CriarCenario();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);
        var matricula = await CriarMatriculaAlocadaAsync(cenario, horario.Id);
        var data = new DateOnly(2026, 8, 20);
        await cenario.FrequenciaService.RegistrarAsync(
            ProfessorId, horario.Id, data,
            new Dictionary<Guid, StatusFrequencia> { [matricula.Id] = StatusFrequencia.Ausente },
            CancellationToken.None);

        var registros = await cenario.FrequenciaService.RegistrarAsync(
            ProfessorId, horario.Id, data,
            new Dictionary<Guid, StatusFrequencia> { [matricula.Id] = StatusFrequencia.Presente },
            CancellationToken.None);

        registros.Should().ContainSingle();
        registros.Single().StatusProfessor.Should().Be(StatusFrequencia.Presente);
        cenario.Registros.Registros.Should().ContainSingle(r => r.MatriculaId == matricula.Id);
    }

    /// <summary>
    /// `matriculaId` do request que não está alocada neste horário é
    /// rejeitada — mesma exceção da issue #10 para o mesmo tipo de checagem.
    /// </summary>
    [Fact]
    public async Task RegistrarAsync_MatriculaNaoAlocadaNesteHorario_RejeitaComAlocacaoNaoEncontradaException()
    {
        var cenario = CriarCenario();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);
        var matriculaNaoAlocada = Matricula.CriarProvisoria(ProfessorId, "Aluno Dois", "aluno-2", Clock);
        await cenario.Matriculas.AdicionarAsync(matriculaNaoAlocada, CancellationToken.None);
        var statusPorMatricula = new Dictionary<Guid, StatusFrequencia> { [matriculaNaoAlocada.Id] = StatusFrequencia.Presente };

        var acao = () => cenario.FrequenciaService.RegistrarAsync(
            ProfessorId, horario.Id, new DateOnly(2026, 8, 20), statusPorMatricula, CancellationToken.None);

        await acao.Should().ThrowAsync<Synclass.Domain.Aulas.AlocacaoNaoEncontradaException>();
        cenario.Registros.Registros.Should().BeEmpty();
    }
}
