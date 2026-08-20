using FluentAssertions;
using Synclass.Domain.Alocacoes;
using Synclass.Domain.Configuracoes;
using Synclass.Domain.Horarios;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Tests.Fakes;

namespace Synclass.Domain.Tests.Alocacoes;

/// <summary>
/// Cobre <see cref="AlocacaoHorarioService.MarcarAsync"/> e
/// <see cref="AlocacaoHorarioService.ListarVagosAsync"/> (issue #9) — o Aluno
/// se marcando livremente em um horário vago, espelho de
/// <see cref="AlocacaoHorarioServiceTests"/> (issue #8, Professor). Arquivo
/// separado para não estourar o limite de 500 linhas do outro (ver
/// docs/spec/code-style.md#estilo-de-código).
/// </summary>
public sealed class AlocacaoHorarioServiceMarcarAsyncTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero));
    private static readonly Guid ProfessorId = Guid.NewGuid();

    private sealed record Cenario(
        HorarioService HorarioService,
        AlocacaoHorarioService AlocacaoHorarioService,
        FakeAlocacaoHorarioRepository Alocacoes,
        FakeMatriculaRepository Matriculas,
        FakeConfiguracaoProfessorRepository Configuracoes);

    private static Cenario CriarCenario(ModeloAgendamento? modelo, Guid? professorId = null)
    {
        var professor = professorId ?? ProfessorId;
        var configuracoes = new FakeConfiguracaoProfessorRepository();
        if (modelo is not null)
        {
            configuracoes.Configuracoes.Add(ConfiguracaoProfessor.Criar(professor, modelo.Value, Clock));
        }

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

    [Fact]
    public async Task MarcarAsync_HorarioLivreComVagaDisponivel_CriaAAlocacaoComOrigemAluno()
    {
        var cenario = CriarCenario(ModeloAgendamento.Vago);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None, limiteAlunos: 1);
        var matricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var alocacao = await cenario.AlocacaoHorarioService.MarcarAsync(
            ProfessorId, horario.Id, matricula.Id, CancellationToken.None);

        alocacao.HorarioId.Should().Be(horario.Id);
        alocacao.MatriculaId.Should().Be(matricula.Id);
        alocacao.OrigemAlocacao.Should().Be(OrigemAlocacao.Aluno);
    }

    [Fact]
    public async Task MarcarAsync_HorarioFixo_RejeitaComModeloNaoPermiteMarcacaoLivreException()
    {
        var cenario = CriarCenario(ModeloAgendamento.Fixo);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Fixo, CancellationToken.None);
        var matricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var acao = () => cenario.AlocacaoHorarioService.MarcarAsync(ProfessorId, horario.Id, matricula.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<ModeloNaoPermiteMarcacaoLivreException>();
        cenario.Alocacoes.Alocacoes.Should().BeEmpty();
    }

    [Fact]
    public async Task MarcarAsync_HorarioHibridoSemAtribuicaoFixa_CriaAAlocacao()
    {
        var cenario = CriarCenario(ModeloAgendamento.Hibrido);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Hibrido, CancellationToken.None, limiteAlunos: 1);
        var matricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var alocacao = await cenario.AlocacaoHorarioService.MarcarAsync(
            ProfessorId, horario.Id, matricula.Id, CancellationToken.None);

        alocacao.MatriculaId.Should().Be(matricula.Id);
    }

    /// <summary>
    /// Comportamento novo da issue #74 (AC3) — o Híbrido por Professor de
    /// hoje bloqueava a marcação livre quando já havia atribuição fixa no
    /// mesmo horário; o Híbrido por horário aceita as duas coisas sempre,
    /// até a capacidade esgotar.
    /// </summary>
    [Fact]
    public async Task MarcarAsync_HorarioHibridoComAtribuicaoFixaDoProfessor_CriaAAlocacao()
    {
        var cenario = CriarCenario(ModeloAgendamento.Hibrido);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Hibrido, CancellationToken.None, limiteAlunos: 2);
        var matriculaFixa = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);
        await cenario.AlocacaoHorarioService.AlocarAsync(ProfessorId, horario.Id, matriculaFixa.Id, CancellationToken.None);
        var matriculaAluno = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var alocacao = await cenario.AlocacaoHorarioService.MarcarAsync(
            ProfessorId, horario.Id, matriculaAluno.Id, CancellationToken.None);

        alocacao.MatriculaId.Should().Be(matriculaAluno.Id);
        cenario.Alocacoes.Alocacoes.Should().HaveCount(2);
    }

    [Fact]
    public async Task MarcarAsync_HorarioNoLimiteDeAlunos_RejeitaComHorarioLotadoException()
    {
        var cenario = CriarCenario(ModeloAgendamento.Vago);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None, limiteAlunos: 1);
        var primeiraMatricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);
        await cenario.AlocacaoHorarioService.MarcarAsync(ProfessorId, horario.Id, primeiraMatricula.Id, CancellationToken.None);
        var segundaMatricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var acao = () => cenario.AlocacaoHorarioService.MarcarAsync(
            ProfessorId, horario.Id, segundaMatricula.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioLotadoException>();
        cenario.Alocacoes.Alocacoes.Should().ContainSingle();
    }

    [Fact]
    public async Task MarcarAsync_MatriculaNaoVinculada_RejeitaComMatriculaNaoVinculadaAoProfessorException()
    {
        var cenario = CriarCenario(ModeloAgendamento.Vago);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);

        var acao = () => cenario.AlocacaoHorarioService.MarcarAsync(ProfessorId, horario.Id, Guid.NewGuid(), CancellationToken.None);

        await acao.Should().ThrowAsync<MatriculaNaoVinculadaAoProfessorException>();
    }

    [Fact]
    public async Task MarcarAsync_AlunoJaMarcadoNesteHorario_RejeitaComAlocacaoJaExisteException()
    {
        var cenario = CriarCenario(ModeloAgendamento.Vago);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None, limiteAlunos: 2);
        var matricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);
        await cenario.AlocacaoHorarioService.MarcarAsync(ProfessorId, horario.Id, matricula.Id, CancellationToken.None);

        var acao = () => cenario.AlocacaoHorarioService.MarcarAsync(ProfessorId, horario.Id, matricula.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<AlocacaoJaExisteException>();
        cenario.Alocacoes.Alocacoes.Should().ContainSingle();
    }

    [Fact]
    public async Task ListarVagosAsync_ModeloVago_RetornaTodosOsHorariosComVaga()
    {
        var cenario = CriarCenario(ModeloAgendamento.Vago);
        var primeiroHorario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var segundoHorario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Quarta, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var vagos = await cenario.AlocacaoHorarioService.ListarVagosAsync(ProfessorId, matricula.Id, CancellationToken.None);

        vagos.Should().HaveCount(2);
        vagos.Should().Contain(h => h.Horario.Id == primeiroHorario.Id);
        vagos.Should().Contain(h => h.Horario.Id == segundoHorario.Id);
    }

    [Fact]
    public async Task ListarVagosAsync_ModeloHibrido_FiltraOsHorariosComAtribuicaoFixaDoProfessor()
    {
        var cenario = CriarCenario(ModeloAgendamento.Hibrido);
        var horarioFixado = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None, limiteAlunos: 2);
        var horarioLivre = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Quarta, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matriculaFixa = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);
        await cenario.AlocacaoHorarioService.AlocarAsync(ProfessorId, horarioFixado.Id, matriculaFixa.Id, CancellationToken.None);
        var matriculaAluno = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var vagos = await cenario.AlocacaoHorarioService.ListarVagosAsync(ProfessorId, matriculaAluno.Id, CancellationToken.None);

        vagos.Should().ContainSingle(h => h.Horario.Id == horarioLivre.Id);
    }

    [Fact]
    public async Task ListarVagosAsync_ModeloFixo_RetornaListaVaziaSemLancar()
    {
        var cenario = CriarCenario(ModeloAgendamento.Fixo);
        await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var vagos = await cenario.AlocacaoHorarioService.ListarVagosAsync(ProfessorId, matricula.Id, CancellationToken.None);

        vagos.Should().BeEmpty();
    }

    [Fact]
    public async Task ListarVagosAsync_SemConfiguracaoProfessor_RetornaListaVaziaSemLancar()
    {
        var cenario = CriarCenario(modelo: null);
        var matricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var vagos = await cenario.AlocacaoHorarioService.ListarVagosAsync(ProfessorId, matricula.Id, CancellationToken.None);

        vagos.Should().BeEmpty();
    }

    [Fact]
    public async Task ListarVagosAsync_ExcluiHorariosSemVaga()
    {
        var cenario = CriarCenario(ModeloAgendamento.Vago);
        var horarioLotado = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None, limiteAlunos: 1);
        var horarioComVaga = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Quarta, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var primeiraMatricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);
        await cenario.AlocacaoHorarioService.MarcarAsync(ProfessorId, horarioLotado.Id, primeiraMatricula.Id, CancellationToken.None);
        var segundaMatricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var vagos = await cenario.AlocacaoHorarioService.ListarVagosAsync(ProfessorId, segundaMatricula.Id, CancellationToken.None);

        vagos.Should().ContainSingle(h => h.Horario.Id == horarioComVaga.Id);
    }

    [Fact]
    public async Task ListarVagosAsync_ExcluiHorarioOndeAMatriculaJaEstaAlocada()
    {
        var cenario = CriarCenario(ModeloAgendamento.Vago);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None, limiteAlunos: 2);
        var matricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);
        await cenario.AlocacaoHorarioService.MarcarAsync(ProfessorId, horario.Id, matricula.Id, CancellationToken.None);

        var vagos = await cenario.AlocacaoHorarioService.ListarVagosAsync(ProfessorId, matricula.Id, CancellationToken.None);

        vagos.Should().BeEmpty();
    }
}
