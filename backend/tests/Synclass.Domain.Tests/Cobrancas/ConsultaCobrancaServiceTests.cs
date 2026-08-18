using FluentAssertions;
using Synclass.Domain.Alocacoes;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Cobrancas;

/// <summary>
/// Cobre <see cref="ConsultaCobrancaService.ConsultarPorProfessorAsync"/>
/// (issue #12): matrícula sem regra, `RegraFixoMensal` (independe de
/// quantidade de aulas), `RegraFixoPorAula` (soma por horário alocado) e o
/// isolamento entre Professores (critério de aceite 4) — ver
/// implementation.md.
/// </summary>
public sealed class ConsultaCobrancaServiceTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero));

    // Agosto/2026: [Terca] 4 ocorrências (04, 11, 18, 25), [Quinta] 4 ocorrências (06, 13, 20, 27).
    private static readonly PeriodoConsulta PeriodoAgosto2026 =
        PeriodoConsulta.Criar(new DateOnly(2026, 8, 1), new DateOnly(2026, 9, 1));

    private static ConsultaCobrancaService CriarServico(
        FakeMatriculaRepository matriculas,
        FakeRegraDeCobrancaRepository regras,
        FakeAlocacaoHorarioRepository alocacoes,
        FakeHorarioRepository horarios)
    {
        return new ConsultaCobrancaService(matriculas, regras, alocacoes, horarios);
    }

    private static Matricula CriarMatriculaDoProfessor(FakeMatriculaRepository matriculas, Guid professorId)
    {
        var matricula = Matricula.CriarProvisoria(professorId, "Aluno Teste", $"aluno-{Guid.NewGuid()}", Clock);
        matriculas.AdicionarAsync(matricula, CancellationToken.None).GetAwaiter().GetResult();
        return matricula;
    }

    [Fact]
    public async Task ConsultarPorProfessorAsync_MatriculaSemRegra_RetornaSemRegraDefinidaEValorNulo()
    {
        var matriculas = new FakeMatriculaRepository();
        var professorId = Guid.NewGuid();
        var matricula = CriarMatriculaDoProfessor(matriculas, professorId);
        var servico = CriarServico(
            matriculas, new FakeRegraDeCobrancaRepository(), new FakeAlocacaoHorarioRepository(), new FakeHorarioRepository());

        var resultado = await servico.ConsultarPorProfessorAsync(professorId, PeriodoAgosto2026, CancellationToken.None);

        var valorDevido = resultado.Should().ContainSingle(v => v.MatriculaId == matricula.Id).Subject;
        valorDevido.SemRegraDefinida.Should().BeTrue();
        valorDevido.Valor.Should().BeNull();
    }

    [Fact]
    public async Task ConsultarPorProfessorAsync_MatriculaComRegraFixoMensal_RetornaValorFixoIndependenteDeAlocacoes()
    {
        var matriculas = new FakeMatriculaRepository();
        var professorId = Guid.NewGuid();
        var matricula = CriarMatriculaDoProfessor(matriculas, professorId);
        var regras = new FakeRegraDeCobrancaRepository();
        await regras.SalvarAsync(RegraFixoMensal.Criar(matricula.Id, 300m, Clock), CancellationToken.None);
        var horarios = new FakeHorarioRepository();
        var horario = Horario.Criar(professorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, Clock);
        await horarios.AdicionarAsync(horario, CancellationToken.None);
        var alocacoes = new FakeAlocacaoHorarioRepository();
        await alocacoes.AdicionarAsync(
            AlocacaoHorario.Criar(horario.Id, matricula.Id, OrigemAlocacao.Professor, Clock), CancellationToken.None);
        var servico = CriarServico(matriculas, regras, alocacoes, horarios);

        var resultado = await servico.ConsultarPorProfessorAsync(professorId, PeriodoAgosto2026, CancellationToken.None);

        var valorDevido = resultado.Should().ContainSingle(v => v.MatriculaId == matricula.Id).Subject;
        valorDevido.SemRegraDefinida.Should().BeFalse();
        valorDevido.Valor.Should().Be(300m);
    }

    [Fact]
    public async Task ConsultarPorProfessorAsync_MatriculaComRegraFixoPorAulaEDoisHorarios_SomaAsOcorrenciasDosDois()
    {
        var matriculas = new FakeMatriculaRepository();
        var professorId = Guid.NewGuid();
        var matricula = CriarMatriculaDoProfessor(matriculas, professorId);
        var regras = new FakeRegraDeCobrancaRepository();
        await regras.SalvarAsync(RegraFixoPorAula.Criar(matricula.Id, 50m, Clock), CancellationToken.None);
        var horarios = new FakeHorarioRepository();
        var horarioTerca = Horario.Criar(professorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, Clock);
        var horarioQuinta = Horario.Criar(professorId, DiaSemana.Quinta, new TimeOnly(14, 0), 60, Clock);
        await horarios.AdicionarAsync(horarioTerca, CancellationToken.None);
        await horarios.AdicionarAsync(horarioQuinta, CancellationToken.None);
        var alocacoes = new FakeAlocacaoHorarioRepository();
        await alocacoes.AdicionarAsync(
            AlocacaoHorario.Criar(horarioTerca.Id, matricula.Id, OrigemAlocacao.Professor, Clock), CancellationToken.None);
        await alocacoes.AdicionarAsync(
            AlocacaoHorario.Criar(horarioQuinta.Id, matricula.Id, OrigemAlocacao.Professor, Clock), CancellationToken.None);
        var servico = CriarServico(matriculas, regras, alocacoes, horarios);

        var resultado = await servico.ConsultarPorProfessorAsync(professorId, PeriodoAgosto2026, CancellationToken.None);

        // 4 terças + 4 quintas em agosto/2026 = 8 aulas * 50 = 400.
        var valorDevido = resultado.Should().ContainSingle(v => v.MatriculaId == matricula.Id).Subject;
        valorDevido.Valor.Should().Be(400m);
    }

    /// <summary>
    /// Prova formal (critério de aceite 4) de que a consulta é escopada por
    /// <c>professorId</c>: um mesmo Aluno com duas <see cref="Matricula"/>
    /// (Professor A e Professor B) só aparece/influencia o resultado do
    /// Professor a que aquela Matricula pertence.
    /// </summary>
    [Fact]
    public async Task ConsultarPorProfessorAsync_AlunoComMatriculaEmOutroProfessor_NaoApareceNemInfluenciaOValor()
    {
        var matriculas = new FakeMatriculaRepository();
        var alunoUsuarioId = Guid.NewGuid();
        var professorA = Guid.NewGuid();
        var professorB = Guid.NewGuid();
        var matriculaComA = Matricula.CriarVinculada(professorA, alunoUsuarioId, Clock);
        var matriculaComB = Matricula.CriarVinculada(professorB, alunoUsuarioId, Clock);
        await matriculas.AdicionarAsync(matriculaComA, CancellationToken.None);
        await matriculas.AdicionarAsync(matriculaComB, CancellationToken.None);
        var regras = new FakeRegraDeCobrancaRepository();
        await regras.SalvarAsync(RegraFixoMensal.Criar(matriculaComB.Id, 999m, Clock), CancellationToken.None);
        var servico = CriarServico(
            matriculas, regras, new FakeAlocacaoHorarioRepository(), new FakeHorarioRepository());

        var resultado = await servico.ConsultarPorProfessorAsync(professorA, PeriodoAgosto2026, CancellationToken.None);

        var valorDevido = resultado.Should().ContainSingle().Subject;
        valorDevido.MatriculaId.Should().Be(matriculaComA.Id);
        valorDevido.SemRegraDefinida.Should().BeTrue();
    }
}
