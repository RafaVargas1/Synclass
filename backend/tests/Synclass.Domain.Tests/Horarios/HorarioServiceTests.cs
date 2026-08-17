using FluentAssertions;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Configuracoes;
using Synclass.Domain.Horarios;

namespace Synclass.Domain.Tests.Horarios;

/// <summary>
/// Cobre os 3 casos de uso orquestrados por <see cref="HorarioService"/>
/// (issue #6): cadastro com checagem de conflito, listagem e remoção com
/// bloqueio por Alunos alocados. A partir da issue #7, cadastro também exige
/// que o Professor já tenha definido um <see cref="ConfiguracaoProfessor"/>.
/// </summary>
public sealed class HorarioServiceTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));
    private static readonly Guid ProfessorId = Guid.NewGuid();

    /// <summary>
    /// Monta um <see cref="HorarioService"/> já com <see cref="ConfiguracaoProfessor"/>
    /// definida (modelo Vago) para cada Professor informado — atalho para os
    /// testes que não são sobre a regra da issue #7 em si, só precisam que o
    /// cadastro não seja bloqueado por ela.
    /// </summary>
    private static HorarioService CriarServicoComConfiguracao(IHorarioRepository repositorio, params Guid[] professoresComConfiguracao)
    {
        var configuracoes = new FakeConfiguracaoProfessorRepository();
        foreach (var professorId in professoresComConfiguracao)
        {
            configuracoes.Configuracoes.Add(ConfiguracaoProfessor.Criar(professorId, ModeloAgendamento.Vago, Clock));
        }

        return new HorarioService(repositorio, configuracoes, Clock);
    }

    [Fact]
    public async Task CadastrarAsync_SemConflito_CriaEPersisteHorario()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = CriarServicoComConfiguracao(repositorio, ProfessorId);

        var horario = await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);

        horario.DiaSemana.Should().Be(DiaSemana.Terca);
        repositorio.Horarios.Should().ContainSingle(h => h.Id == horario.Id);
    }

    [Fact]
    public async Task CadastrarAsync_SobrepoeHorarioExistente_RejeitaComHorarioConflitanteException()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = CriarServicoComConfiguracao(repositorio, ProfessorId);
        await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);

        var acao = () => servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 30), 60, CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioConflitanteException>();
        repositorio.Horarios.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarAsync_DuracaoInvalida_RejeitaSemConsultarConflito()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = CriarServicoComConfiguracao(repositorio, ProfessorId);

        var acao = () => servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 0, CancellationToken.None);

        await acao.Should().ThrowAsync<DuracaoInvalidaException>();
        repositorio.Horarios.Should().BeEmpty();
    }

    [Fact]
    public async Task CadastrarAsync_DiaSemanaInvalido_RejeitaSemConsultarConflito()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = CriarServicoComConfiguracao(repositorio, ProfessorId);

        var acao = () => servico.CadastrarAsync(ProfessorId, (DiaSemana)99, new TimeOnly(10, 0), 60, CancellationToken.None);

        await acao.Should().ThrowAsync<DiaSemanaInvalidoException>();
        repositorio.Horarios.Should().BeEmpty();
    }

    [Fact]
    public async Task CadastrarAsync_ProfessorSemConfiguracao_RejeitaComModeloAgendamentoNaoDefinidoException()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, new FakeConfiguracaoProfessorRepository(), Clock);

        var acao = () => servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);

        await acao.Should().ThrowAsync<ModeloAgendamentoNaoDefinidoException>();
        repositorio.Horarios.Should().BeEmpty();
    }

    [Theory]
    [InlineData(ModeloAgendamento.Vago)]
    [InlineData(ModeloAgendamento.Fixo)]
    [InlineData(ModeloAgendamento.Hibrido)]
    public async Task CadastrarAsync_ProfessorComConfiguracaoExistente_SegueNormalmenteEmQualquerModelo(ModeloAgendamento modelo)
    {
        var repositorio = new FakeHorarioRepository();
        var configuracoes = new FakeConfiguracaoProfessorRepository();
        configuracoes.Configuracoes.Add(ConfiguracaoProfessor.Criar(ProfessorId, modelo, Clock));
        var servico = new HorarioService(repositorio, configuracoes, Clock);

        var horario = await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);

        repositorio.Horarios.Should().ContainSingle(h => h.Id == horario.Id);
    }

    [Fact]
    public async Task CadastrarAsync_SemInformarLimiteAlunos_AplicaDefault1()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = CriarServicoComConfiguracao(repositorio, ProfessorId);

        var horario = await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);

        horario.LimiteAlunos.Should().Be(1);
    }

    [Fact]
    public async Task CadastrarAsync_ComLimiteAlunosInformado_UsaOValorInformado()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = CriarServicoComConfiguracao(repositorio, ProfessorId);

        var horario = await servico.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None, limiteAlunos: 4);

        horario.LimiteAlunos.Should().Be(4);
    }

    [Fact]
    public async Task CadastrarAsync_LimiteAlunosZeroOuNegativo_RejeitaComLimiteAlunosInvalidoException()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = CriarServicoComConfiguracao(repositorio, ProfessorId);

        var acao = () => servico.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None, limiteAlunos: 0);

        await acao.Should().ThrowAsync<LimiteAlunosInvalidoException>();
        repositorio.Horarios.Should().BeEmpty();
    }

    [Fact]
    public async Task ListarAsync_DevolveApenasHorariosDoProfessor()
    {
        var repositorio = new FakeHorarioRepository();
        var outroProfessorId = Guid.NewGuid();
        var servico = CriarServicoComConfiguracao(repositorio, ProfessorId, outroProfessorId);
        await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);
        await servico.CadastrarAsync(outroProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);

        var horarios = await servico.ListarAsync(ProfessorId, CancellationToken.None);

        horarios.Should().ContainSingle(h => h.ProfessorId == ProfessorId);
    }

    [Fact]
    public async Task RemoverAsync_HorarioExistenteSemAlunos_Remove()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = CriarServicoComConfiguracao(repositorio, ProfessorId);
        var horario = await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);

        await servico.RemoverAsync(ProfessorId, horario.Id, CancellationToken.None);

        repositorio.Horarios.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoverAsync_ComAlunosAlocados_RejeitaComHorarioComAlunosAlocadosException()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = CriarServicoComConfiguracao(repositorio, ProfessorId);
        var horario = await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);
        repositorio.AlunosAlocadosPorHorario.Add(horario.Id);

        var acao = () => servico.RemoverAsync(ProfessorId, horario.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioComAlunosAlocadosException>();
        repositorio.Horarios.Should().ContainSingle();
    }

    [Fact]
    public async Task RemoverAsync_HorarioInexistente_RejeitaComHorarioNaoEncontradoException()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = CriarServicoComConfiguracao(repositorio, ProfessorId);

        var acao = () => servico.RemoverAsync(ProfessorId, Guid.NewGuid(), CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioNaoEncontradoException>();
    }

    [Fact]
    public async Task RemoverAsync_HorarioDeOutroProfessor_RejeitaComHorarioNaoEncontradoException()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = CriarServicoComConfiguracao(repositorio, ProfessorId);
        var horario = await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);

        var acao = () => servico.RemoverAsync(Guid.NewGuid(), horario.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioNaoEncontradoException>();
        repositorio.Horarios.Should().ContainSingle();
    }
}
