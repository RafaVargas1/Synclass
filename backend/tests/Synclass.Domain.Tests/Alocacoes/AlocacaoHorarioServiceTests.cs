using FluentAssertions;
using Synclass.Domain.Alocacoes;
using Synclass.Domain.Configuracoes;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Alocacoes;

/// <summary>
/// Cobre os casos de uso orquestrados por <see cref="AlocacaoHorarioService"/>
/// (issue #8): alocar, desalocar e listar Alunos alocados a um horário
/// específico (template recorrente, nunca uma ocorrência datada).
/// </summary>
public sealed class AlocacaoHorarioServiceTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 17, 12, 0, 0, TimeSpan.Zero));
    private static readonly Guid ProfessorId = Guid.NewGuid();

    private sealed record Cenario(
        HorarioService HorarioService,
        AlocacaoHorarioService AlocacaoHorarioService,
        FakeAlocacaoHorarioRepository Alocacoes,
        FakeMatriculaRepository Matriculas,
        FakeConfiguracaoProfessorRepository Configuracoes);

    /// <summary>
    /// Monta os dois serviços já com <see cref="ConfiguracaoProfessor"/>
    /// definida no modelo informado — atalho para os testes que não são
    /// sobre a regra de modelo em si.
    /// </summary>
    private static Cenario CriarCenario(ModeloAgendamento modelo, Guid? professorId = null)
    {
        var professor = professorId ?? ProfessorId;
        var configuracoes = new FakeConfiguracaoProfessorRepository();
        configuracoes.Configuracoes.Add(ConfiguracaoProfessor.Criar(professor, modelo, Clock));
        var horarios = new FakeHorarioRepository();
        var horarioService = new HorarioService(horarios, configuracoes, Clock);
        var alocacoes = new FakeAlocacaoHorarioRepository();
        var matriculas = new FakeMatriculaRepository();
        var alocacaoHorarioService = new AlocacaoHorarioService(alocacoes, matriculas, configuracoes, horarioService, Clock);
        return new Cenario(horarioService, alocacaoHorarioService, alocacoes, matriculas, configuracoes);
    }

    private static async Task<Matricula> CriarMatriculaAsync(FakeMatriculaRepository matriculas, Guid professorId)
    {
        var matricula = Matricula.CriarProvisoria(professorId, "Aluno Um", $"aluno-{Guid.NewGuid()}", Clock);
        await matriculas.AdicionarAsync(matricula, CancellationToken.None);
        return matricula;
    }

    [Theory]
    [InlineData(ModeloAgendamento.Fixo)]
    [InlineData(ModeloAgendamento.Hibrido)]
    public async Task AlocarAsync_ComVagaDisponivel_CriaAAlocacao(ModeloAgendamento modelo)
    {
        var cenario = CriarCenario(modelo);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None, limiteAlunos: 1);
        var matricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var alocacao = await cenario.AlocacaoHorarioService.AlocarAsync(
            ProfessorId, horario.Id, matricula.Id, CancellationToken.None);

        alocacao.HorarioId.Should().Be(horario.Id);
        alocacao.MatriculaId.Should().Be(matricula.Id);
        cenario.Alocacoes.Alocacoes.Should().ContainSingle(a => a.Id == alocacao.Id);
    }

    [Fact]
    public async Task AlocarAsync_ModeloVago_RejeitaComModeloNaoPermiteAlocacaoException()
    {
        var cenario = CriarCenario(ModeloAgendamento.Vago);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);
        var matricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var acao = () => cenario.AlocacaoHorarioService.AlocarAsync(ProfessorId, horario.Id, matricula.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<ModeloNaoPermiteAlocacaoException>();
        cenario.Alocacoes.Alocacoes.Should().BeEmpty();
    }
}
