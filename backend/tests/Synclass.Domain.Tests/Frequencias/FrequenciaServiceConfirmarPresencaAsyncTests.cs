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
/// Cobre <see cref="FrequenciaService.ConfirmarPresencaAsync"/> (issue #15) —
/// o Aluno confirma a própria presença numa aula. Mesmo padrão de cenário de
/// <see cref="FrequenciaServiceRegistrarAsyncTests"/>.
/// </summary>
public sealed class FrequenciaServiceConfirmarPresencaAsyncTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero));
    private static readonly Guid ProfessorId = Guid.NewGuid();

    private sealed record Cenario(
        FrequenciaService FrequenciaService,
        FakeAulaRepository Aulas,
        FakeRegistroFrequenciaRepository Registros,
        FakeAlocacaoHorarioRepository Alocacoes,
        FakeMatriculaRepository Matriculas,
        FakeCancelamentoAulaRepository Cancelamentos,
        HorarioService HorarioService);

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
        var aulaService = new AulaService(
            aulas, cancelamentos, alocacoes, matriculas, configuracoes, horarioService, Clock);
        var alocacaoHorarioService = new AlocacaoHorarioService(alocacoes, matriculas, horarioService, Clock);
        var registros = new FakeRegistroFrequenciaRepository();
        var usuarios = new FakeUsuarioRepository();
        var frequenciaService = new FrequenciaService(
            registros, alocacoes, aulaService, alocacaoHorarioService, cancelamentos, horarioService, Clock,
            matriculas, usuarios, horarios, aulas);
        return new Cenario(frequenciaService, aulas, registros, alocacoes, matriculas, cancelamentos, horarioService);
    }

    private static async Task<Matricula> CriarMatriculaVinculadaEAlocadaAsync(Cenario cenario, Guid horarioId, Guid alunoUsuarioId)
    {
        var matricula = Matricula.CriarVinculada(ProfessorId, alunoUsuarioId, Clock);
        await cenario.Matriculas.AdicionarAsync(matricula, CancellationToken.None);
        var alocacao = AlocacaoHorario.Criar(horarioId, matricula.Id, OrigemAlocacao.Aluno, Clock);
        await cenario.Alocacoes.AdicionarAsync(alocacao, CancellationToken.None);
        return matricula;
    }

    /// <summary>
    /// AC1 — nenhuma linha existia ainda: confirmar cria a Aula sob demanda
    /// (reaproveita `ObterOuCriarAulaAsync`) e o `RegistroFrequencia` com
    /// `ConfirmadoPeloAluno = true`.
    /// </summary>
    [Fact]
    public async Task ConfirmarPresencaAsync_NenhumRegistroExistiaAinda_CriaAulaERegistroComConfirmacao()
    {
        var cenario = CriarCenario();
        var alunoUsuarioId = Guid.NewGuid();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = await CriarMatriculaVinculadaEAlocadaAsync(cenario, horario.Id, alunoUsuarioId);
        var data = new DateOnly(2026, 8, 25);

        var registro = await cenario.FrequenciaService.ConfirmarPresencaAsync(
            ProfessorId, horario.Id, data, alunoUsuarioId, CancellationToken.None);

        cenario.Aulas.Aulas.Should().ContainSingle(a => a.HorarioId == horario.Id && a.Data == data);
        registro.MatriculaId.Should().Be(matricula.Id);
        registro.ConfirmadoPeloAluno.Should().BeTrue();
        registro.StatusProfessor.Should().BeNull();
        cenario.Registros.Registros.Should().ContainSingle();
    }

    /// <summary>
    /// AC2 — confirmar de novo para a mesma aula é um upsert idempotente: não
    /// cria uma segunda linha.
    /// </summary>
    [Fact]
    public async Task ConfirmarPresencaAsync_ConfirmaDeNovoParaMesmaAula_NaoDuplicaALinha()
    {
        var cenario = CriarCenario();
        var alunoUsuarioId = Guid.NewGuid();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        await CriarMatriculaVinculadaEAlocadaAsync(cenario, horario.Id, alunoUsuarioId);
        var data = new DateOnly(2026, 8, 25);

        var primeiraConfirmacao = await cenario.FrequenciaService.ConfirmarPresencaAsync(
            ProfessorId, horario.Id, data, alunoUsuarioId, CancellationToken.None);
        var segundaConfirmacao = await cenario.FrequenciaService.ConfirmarPresencaAsync(
            ProfessorId, horario.Id, data, alunoUsuarioId, CancellationToken.None);

        segundaConfirmacao.Id.Should().Be(primeiraConfirmacao.Id);
        cenario.Registros.Registros.Should().ContainSingle();
    }

    /// <summary>
    /// Aluno confirma antes do Professor registrar — `StatusProfessor`
    /// permanece nulo, diferenciando "não registrada" de "ausente".
    /// </summary>
    [Fact]
    public async Task ConfirmarPresencaAsync_AntesDoProfessorRegistrar_StatusProfessorPermaneceNulo()
    {
        var cenario = CriarCenario();
        var alunoUsuarioId = Guid.NewGuid();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        await CriarMatriculaVinculadaEAlocadaAsync(cenario, horario.Id, alunoUsuarioId);

        var registro = await cenario.FrequenciaService.ConfirmarPresencaAsync(
            ProfessorId, horario.Id, new DateOnly(2026, 8, 25), alunoUsuarioId, CancellationToken.None);

        registro.StatusProfessor.Should().BeNull();
    }

    /// <summary>
    /// Aluno cancelou a própria ocorrência (issue #10) antes de tentar
    /// confirmar — rejeitado, não silenciosamente ignorado.
    /// </summary>
    [Fact]
    public async Task ConfirmarPresencaAsync_AlunoJaCancelouEssaOcorrencia_RejeitaComAulaCanceladaPeloAlunoException()
    {
        var cenario = CriarCenario();
        var alunoUsuarioId = Guid.NewGuid();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = await CriarMatriculaVinculadaEAlocadaAsync(cenario, horario.Id, alunoUsuarioId);
        var data = new DateOnly(2026, 8, 25);
        var aula = Aula.Criar(horario.Id, data, Clock);
        await cenario.Aulas.AdicionarAsync(aula, CancellationToken.None);
        var cancelamento = CancelamentoAula.Criar(aula.Id, matricula.Id, Clock);
        await cenario.Cancelamentos.AdicionarAsync(cancelamento, CancellationToken.None);

        var acao = () => cenario.FrequenciaService.ConfirmarPresencaAsync(
            ProfessorId, horario.Id, data, alunoUsuarioId, CancellationToken.None);

        await acao.Should().ThrowAsync<AulaCanceladaPeloAlunoException>();
        cenario.Registros.Registros.Should().BeEmpty();
    }

    /// <summary>
    /// Aluno não vinculado ao Professor/horário — rejeitado com a mesma
    /// exceção de `AulaService.CancelarAsync`.
    /// </summary>
    [Fact]
    public async Task ConfirmarPresencaAsync_AlunoNaoVinculadoAoProfessor_RejeitaComAlunoNaoVinculadoAoProfessorException()
    {
        var cenario = CriarCenario();
        var alunoUsuarioId = Guid.NewGuid();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);

        var acao = () => cenario.FrequenciaService.ConfirmarPresencaAsync(
            ProfessorId, horario.Id, new DateOnly(2026, 8, 25), alunoUsuarioId, CancellationToken.None);

        await acao.Should().ThrowAsync<AlunoNaoVinculadoAoProfessorException>();
    }

    /// <summary>
    /// Aluno vinculado ao Professor mas não alocado *neste horário
    /// específico* — rejeitado com a mesma exceção usada por
    /// `AulaService.CancelarAsync`.
    /// </summary>
    [Fact]
    public async Task ConfirmarPresencaAsync_AlunoVinculadoMasNaoAlocadoNesteHorario_RejeitaComAlocacaoNaoEncontradaException()
    {
        var cenario = CriarCenario();
        var alunoUsuarioId = Guid.NewGuid();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = Matricula.CriarVinculada(ProfessorId, alunoUsuarioId, Clock);
        await cenario.Matriculas.AdicionarAsync(matricula, CancellationToken.None);

        var acao = () => cenario.FrequenciaService.ConfirmarPresencaAsync(
            ProfessorId, horario.Id, new DateOnly(2026, 8, 25), alunoUsuarioId, CancellationToken.None);

        await acao.Should().ThrowAsync<Synclass.Domain.Aulas.AlocacaoNaoEncontradaException>();
    }

    /// <summary>
    /// AC3 (ordem inversa) — Professor registra depois que o Aluno já
    /// confirmou: a reconciliação já é coberta por
    /// <see cref="FrequenciaServiceRegistrarAsyncTests"/>; aqui cobrimos só a
    /// ordem "Aluno confirma, depois Professor registra" ponta a ponta pelos
    /// dois serviços, sem seed manual do `RegistroFrequencia`.
    /// </summary>
    [Fact]
    public async Task ConfirmarPresencaAsync_DepoisProfessorRegistraPresente_PreservaConfirmacaoDoAlunoNaMesmaLinha()
    {
        var cenario = CriarCenario();
        var alunoUsuarioId = Guid.NewGuid();
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = await CriarMatriculaVinculadaEAlocadaAsync(cenario, horario.Id, alunoUsuarioId);
        var data = new DateOnly(2026, 8, 25);
        await cenario.FrequenciaService.ConfirmarPresencaAsync(
            ProfessorId, horario.Id, data, alunoUsuarioId, CancellationToken.None);

        var registros = await cenario.FrequenciaService.RegistrarAsync(
            ProfessorId, horario.Id, data,
            new Dictionary<Guid, StatusFrequencia> { [matricula.Id] = StatusFrequencia.Presente },
            CancellationToken.None);

        registros.Should().ContainSingle();
        var registro = registros.Single();
        registro.ConfirmadoPeloAluno.Should().BeTrue();
        registro.StatusProfessor.Should().Be(StatusFrequencia.Presente);
        cenario.Registros.Registros.Should().ContainSingle();
    }
}
