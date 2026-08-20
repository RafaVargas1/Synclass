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
        var horarioService = new HorarioService(horarios, Clock);
        var alocacoes = new FakeAlocacaoHorarioRepository();
        var matriculas = new FakeMatriculaRepository();
        var alocacaoHorarioService = new AlocacaoHorarioService(alocacoes, matriculas, horarioService, Clock);
        return new Cenario(horarioService, alocacaoHorarioService, alocacoes, matriculas, configuracoes);
    }

    private static async Task<Matricula> CriarMatriculaAsync(FakeMatriculaRepository matriculas, Guid professorId)
    {
        var matricula = Matricula.CriarProvisoria(professorId, "Aluno Um", $"aluno-{Guid.NewGuid()}", Clock);
        await matriculas.AdicionarAsync(matricula, CancellationToken.None);
        return matricula;
    }

    [Theory]
    [InlineData(TipoMarcacao.Fixo)]
    [InlineData(TipoMarcacao.Hibrido)]
    public async Task AlocarAsync_ComVagaDisponivel_CriaAAlocacao(TipoMarcacao tipoMarcacao)
    {
        var cenario = CriarCenario(ModeloAgendamento.Fixo);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, tipoMarcacao, CancellationToken.None, limiteAlunos: 1);
        var matricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var alocacao = await cenario.AlocacaoHorarioService.AlocarAsync(
            ProfessorId, horario.Id, matricula.Id, CancellationToken.None);

        alocacao.HorarioId.Should().Be(horario.Id);
        alocacao.MatriculaId.Should().Be(matricula.Id);
        cenario.Alocacoes.Alocacoes.Should().ContainSingle(a => a.Id == alocacao.Id);
    }

    [Fact]
    public async Task AlocarAsync_HorarioLivre_RejeitaComModeloNaoPermiteAlocacaoException()
    {
        var cenario = CriarCenario(ModeloAgendamento.Fixo);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        var matricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var acao = () => cenario.AlocacaoHorarioService.AlocarAsync(ProfessorId, horario.Id, matricula.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<ModeloNaoPermiteAlocacaoException>();
        cenario.Alocacoes.Alocacoes.Should().BeEmpty();
    }

    [Fact]
    public async Task AlocarAsync_HorarioNoLimiteDeAlunos_RejeitaComHorarioLotadoException()
    {
        var cenario = CriarCenario(ModeloAgendamento.Fixo);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Fixo, CancellationToken.None, limiteAlunos: 1);
        var primeiraMatricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);
        await cenario.AlocacaoHorarioService.AlocarAsync(ProfessorId, horario.Id, primeiraMatricula.Id, CancellationToken.None);
        var segundaMatricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var acao = () => cenario.AlocacaoHorarioService.AlocarAsync(ProfessorId, horario.Id, segundaMatricula.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioLotadoException>();
        cenario.Alocacoes.Alocacoes.Should().ContainSingle();
    }

    [Fact]
    public async Task AlocarAsync_MatriculaInexistente_RejeitaComMatriculaNaoVinculadaAoProfessorException()
    {
        var cenario = CriarCenario(ModeloAgendamento.Fixo);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Fixo, CancellationToken.None);

        var acao = () => cenario.AlocacaoHorarioService.AlocarAsync(ProfessorId, horario.Id, Guid.NewGuid(), CancellationToken.None);

        await acao.Should().ThrowAsync<MatriculaNaoVinculadaAoProfessorException>();
        cenario.Alocacoes.Alocacoes.Should().BeEmpty();
    }

    [Fact]
    public async Task AlocarAsync_MatriculaDeOutroProfessor_RejeitaComMatriculaNaoVinculadaAoProfessorException()
    {
        var cenario = CriarCenario(ModeloAgendamento.Fixo);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Fixo, CancellationToken.None);
        var matriculaDeOutroProfessor = await CriarMatriculaAsync(cenario.Matriculas, Guid.NewGuid());

        var acao = () => cenario.AlocacaoHorarioService.AlocarAsync(
            ProfessorId, horario.Id, matriculaDeOutroProfessor.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<MatriculaNaoVinculadaAoProfessorException>();
        cenario.Alocacoes.Alocacoes.Should().BeEmpty();
    }

    [Fact]
    public async Task AlocarAsync_HorarioInexistente_RejeitaComHorarioNaoEncontradoException()
    {
        var cenario = CriarCenario(ModeloAgendamento.Fixo);
        var matricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var acao = () => cenario.AlocacaoHorarioService.AlocarAsync(ProfessorId, Guid.NewGuid(), matricula.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioNaoEncontradoException>();
    }

    [Fact]
    public async Task AlocarAsync_HorarioDeOutroProfessor_RejeitaComHorarioNaoEncontradoException()
    {
        var cenario = CriarCenario(ModeloAgendamento.Fixo);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Fixo, CancellationToken.None);
        var matricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var acao = () => cenario.AlocacaoHorarioService.AlocarAsync(Guid.NewGuid(), horario.Id, matricula.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioNaoEncontradoException>();
    }

    [Fact]
    public async Task AlocarAsync_MesmoAlunoJaAlocadoNoHorario_RejeitaComAlocacaoJaExisteException()
    {
        var cenario = CriarCenario(ModeloAgendamento.Fixo);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Fixo, CancellationToken.None, limiteAlunos: 2);
        var matricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);
        await cenario.AlocacaoHorarioService.AlocarAsync(ProfessorId, horario.Id, matricula.Id, CancellationToken.None);

        var acao = () => cenario.AlocacaoHorarioService.AlocarAsync(ProfessorId, horario.Id, matricula.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<AlocacaoJaExisteException>();
        cenario.Alocacoes.Alocacoes.Should().ContainSingle();
    }

    [Fact]
    public async Task DesalocarAsync_AlocacaoExistente_RemoveELiberaAVaga()
    {
        var cenario = CriarCenario(ModeloAgendamento.Fixo);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Fixo, CancellationToken.None, limiteAlunos: 1);
        var primeiraMatricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);
        await cenario.AlocacaoHorarioService.AlocarAsync(ProfessorId, horario.Id, primeiraMatricula.Id, CancellationToken.None);

        await cenario.AlocacaoHorarioService.DesalocarAsync(ProfessorId, horario.Id, primeiraMatricula.Id, CancellationToken.None);

        cenario.Alocacoes.Alocacoes.Should().BeEmpty();
        var segundaMatricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);
        var novaAlocacao = await cenario.AlocacaoHorarioService.AlocarAsync(
            ProfessorId, horario.Id, segundaMatricula.Id, CancellationToken.None);
        novaAlocacao.MatriculaId.Should().Be(segundaMatricula.Id);
    }

    [Fact]
    public async Task DesalocarAsync_DeUmHorario_NaoAfetaAlocacaoDoMesmoAlunoEmOutroHorario()
    {
        var cenario = CriarCenario(ModeloAgendamento.Fixo);
        var primeiroHorario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Fixo, CancellationToken.None);
        var segundoHorario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Quarta, new TimeOnly(10, 0), 60, TipoMarcacao.Fixo, CancellationToken.None);
        var matricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);
        await cenario.AlocacaoHorarioService.AlocarAsync(ProfessorId, primeiroHorario.Id, matricula.Id, CancellationToken.None);
        await cenario.AlocacaoHorarioService.AlocarAsync(ProfessorId, segundoHorario.Id, matricula.Id, CancellationToken.None);

        await cenario.AlocacaoHorarioService.DesalocarAsync(ProfessorId, primeiroHorario.Id, matricula.Id, CancellationToken.None);

        cenario.Alocacoes.Alocacoes.Should().ContainSingle(a => a.HorarioId == segundoHorario.Id);
    }

    [Fact]
    public async Task DesalocarAsync_AlocacaoInexistente_RejeitaComAlocacaoNaoEncontradaException()
    {
        var cenario = CriarCenario(ModeloAgendamento.Fixo);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Fixo, CancellationToken.None);
        var matricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);

        var acao = () => cenario.AlocacaoHorarioService.DesalocarAsync(ProfessorId, horario.Id, matricula.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<AlocacaoNaoEncontradaException>();
    }

    [Fact]
    public async Task DesalocarAsync_HorarioInexistenteOuDeOutroProfessor_RejeitaComHorarioNaoEncontradoException()
    {
        var cenario = CriarCenario(ModeloAgendamento.Fixo);

        var acao = () => cenario.AlocacaoHorarioService.DesalocarAsync(
            ProfessorId, Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioNaoEncontradoException>();
    }

    [Fact]
    public async Task ListarPorHorarioAsync_DevolveAsAlocacoesDoHorario()
    {
        var cenario = CriarCenario(ModeloAgendamento.Fixo);
        var horario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Fixo, CancellationToken.None, limiteAlunos: 2);
        var outroHorario = await cenario.HorarioService.CadastrarAsync(
            ProfessorId, DiaSemana.Quarta, new TimeOnly(10, 0), 60, TipoMarcacao.Fixo, CancellationToken.None);
        var primeiraMatricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);
        var segundaMatricula = await CriarMatriculaAsync(cenario.Matriculas, ProfessorId);
        await cenario.AlocacaoHorarioService.AlocarAsync(ProfessorId, horario.Id, primeiraMatricula.Id, CancellationToken.None);
        await cenario.AlocacaoHorarioService.AlocarAsync(ProfessorId, horario.Id, segundaMatricula.Id, CancellationToken.None);
        await cenario.AlocacaoHorarioService.AlocarAsync(ProfessorId, outroHorario.Id, primeiraMatricula.Id, CancellationToken.None);

        var alocacoes = await cenario.AlocacaoHorarioService.ListarPorHorarioAsync(ProfessorId, horario.Id, CancellationToken.None);

        alocacoes.Should().HaveCount(2);
        alocacoes.Should().OnlyContain(a => a.HorarioId == horario.Id);
    }
}
