using FluentAssertions;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Horarios;

namespace Synclass.Domain.Tests.Horarios;

/// <summary>
/// Cobre os 3 casos de uso orquestrados por <see cref="HorarioService"/>
/// (issue #6): cadastro com checagem de conflito, listagem e remoção com
/// bloqueio por Alunos alocados. Não exige mais nenhuma <c>ConfiguracaoProfessor</c>
/// prévia (issue #76, achado do dev-review no PR #85 — ver docstring de
/// <see cref="HorarioService"/>). Também cobre a alteração da política de
/// marcação de um horário já cadastrado (issue #71).
/// </summary>
public sealed class HorarioServiceTests
{
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 8, 11, 12, 0, 0, TimeSpan.Zero));
    private static readonly Guid ProfessorId = Guid.NewGuid();

    [Fact]
    public async Task CadastrarAsync_SemConflito_CriaEPersisteHorario()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);

        var horario = await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);

        horario.DiaSemana.Should().Be(DiaSemana.Terca);
        repositorio.Horarios.Should().ContainSingle(h => h.Id == horario.Id);
    }

    [Fact]
    public async Task CadastrarAsync_SobrepoeHorarioExistente_RejeitaComHorarioConflitanteException()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);
        await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);

        var acao = () => servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 30), 60, TipoMarcacao.Livre, CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioConflitanteException>();
        repositorio.Horarios.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarAsync_DuracaoInvalida_RejeitaSemConsultarConflito()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);

        var acao = () => servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 0, TipoMarcacao.Livre, CancellationToken.None);

        await acao.Should().ThrowAsync<DuracaoInvalidaException>();
        repositorio.Horarios.Should().BeEmpty();
    }

    [Fact]
    public async Task CadastrarAsync_DiaSemanaInvalido_RejeitaSemConsultarConflito()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);

        var acao = () => servico.CadastrarAsync(ProfessorId, (DiaSemana)99, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);

        await acao.Should().ThrowAsync<DiaSemanaInvalidoException>();
        repositorio.Horarios.Should().BeEmpty();
    }

    [Fact]
    public async Task CadastrarAsync_ProfessorSemConfiguracaoPrevia_CriaEPersisteHorarioNormalmente()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);

        var horario = await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);

        repositorio.Horarios.Should().ContainSingle(h => h.Id == horario.Id);
    }

    [Fact]
    public async Task CadastrarAsync_SemInformarLimiteAlunos_AplicaDefault1()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);

        var horario = await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);

        horario.LimiteAlunos.Should().Be(1);
    }

    [Fact]
    public async Task CadastrarAsync_ComLimiteAlunosInformado_UsaOValorInformado()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);

        var horario = await servico.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None, limiteAlunos: 4);

        horario.LimiteAlunos.Should().Be(4);
    }

    [Fact]
    public async Task CadastrarAsync_LimiteAlunosZeroOuNegativo_RejeitaComLimiteAlunosInvalidoException()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);

        var acao = () => servico.CadastrarAsync(
            ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None, limiteAlunos: 0);

        await acao.Should().ThrowAsync<LimiteAlunosInvalidoException>();
        repositorio.Horarios.Should().BeEmpty();
    }

    [Fact]
    public async Task ListarAsync_DevolveApenasHorariosDoProfessor()
    {
        var repositorio = new FakeHorarioRepository();
        var outroProfessorId = Guid.NewGuid();
        var servico = new HorarioService(repositorio, Clock);
        await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        await servico.CadastrarAsync(outroProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);

        var horarios = await servico.ListarAsync(ProfessorId, CancellationToken.None);

        horarios.Should().ContainSingle(h => h.ProfessorId == ProfessorId);
    }

    [Fact]
    public async Task RemoverAsync_HorarioExistenteSemAlunos_Remove()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);
        var horario = await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);

        await servico.RemoverAsync(ProfessorId, horario.Id, CancellationToken.None);

        repositorio.Horarios.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoverAsync_ComAlunosAlocados_RejeitaComHorarioComAlunosAlocadosException()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);
        var horario = await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);
        repositorio.AlunosAlocadosPorHorario.Add(horario.Id);

        var acao = () => servico.RemoverAsync(ProfessorId, horario.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioComAlunosAlocadosException>();
        repositorio.Horarios.Should().ContainSingle();
    }

    [Fact]
    public async Task RemoverAsync_HorarioInexistente_RejeitaComHorarioNaoEncontradoException()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);

        var acao = () => servico.RemoverAsync(ProfessorId, Guid.NewGuid(), CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioNaoEncontradoException>();
    }

    [Fact]
    public async Task RemoverAsync_HorarioDeOutroProfessor_RejeitaComHorarioNaoEncontradoException()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);
        var horario = await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);

        var acao = () => servico.RemoverAsync(Guid.NewGuid(), horario.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioNaoEncontradoException>();
        repositorio.Horarios.Should().ContainSingle();
    }

    [Fact]
    public async Task AlterarPoliticaAsync_QuandoHorarioPertenceAoProfessor_PersisteANovaPolitica()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);
        var horario = await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);

        var atualizado = await servico.AlterarPoliticaAsync(ProfessorId, horario.Id, TipoMarcacao.Fixo, CancellationToken.None);

        atualizado.TipoMarcacao.Should().Be(TipoMarcacao.Fixo);
        repositorio.Horarios.Should().ContainSingle(h => h.Id == horario.Id && h.TipoMarcacao == TipoMarcacao.Fixo);
    }

    [Fact]
    public async Task AlterarPoliticaAsync_HorarioInexistente_RejeitaComHorarioNaoEncontradoException()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);

        var acao = () => servico.AlterarPoliticaAsync(ProfessorId, Guid.NewGuid(), TipoMarcacao.Fixo, CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioNaoEncontradoException>();
    }

    [Fact]
    public async Task AlterarPoliticaAsync_HorarioDeOutroProfessor_RejeitaComHorarioNaoEncontradoException()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);
        var horario = await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, TipoMarcacao.Livre, CancellationToken.None);

        var acao = () => servico.AlterarPoliticaAsync(Guid.NewGuid(), horario.Id, TipoMarcacao.Fixo, CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioNaoEncontradoException>();
        repositorio.Horarios.Should().ContainSingle(h => h.Id == horario.Id && h.TipoMarcacao == TipoMarcacao.Livre);
    }
}
