using FluentAssertions;
using Synclass.Domain.Tests.Fakes;
using Synclass.Domain.Horarios;

namespace Synclass.Domain.Tests.Horarios;

/// <summary>
/// Cobre os 3 casos de uso orquestrados por <see cref="HorarioService"/>
/// (issue #6): cadastro com checagem de conflito, listagem e remoção com
/// bloqueio por Alunos alocados.
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

        var horario = await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);

        horario.DiaSemana.Should().Be(DiaSemana.Terca);
        repositorio.Horarios.Should().ContainSingle(h => h.Id == horario.Id);
    }

    [Fact]
    public async Task CadastrarAsync_SobrepoeHorarioExistente_RejeitaComHorarioConflitanteException()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);
        await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);

        var acao = () => servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 30), 60, CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioConflitanteException>();
        repositorio.Horarios.Should().ContainSingle();
    }

    [Fact]
    public async Task CadastrarAsync_DuracaoInvalida_RejeitaSemConsultarConflito()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);

        var acao = () => servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 0, CancellationToken.None);

        await acao.Should().ThrowAsync<DuracaoInvalidaException>();
        repositorio.Horarios.Should().BeEmpty();
    }

    [Fact]
    public async Task ListarAsync_DevolveApenasHorariosDoProfessor()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);
        await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);
        var outroProfessorId = Guid.NewGuid();
        await servico.CadastrarAsync(outroProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);

        var horarios = await servico.ListarAsync(ProfessorId, CancellationToken.None);

        horarios.Should().ContainSingle(h => h.ProfessorId == ProfessorId);
    }

    [Fact]
    public async Task RemoverAsync_HorarioExistenteSemAlunos_Remove()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);
        var horario = await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);

        await servico.RemoverAsync(ProfessorId, horario.Id, CancellationToken.None);

        repositorio.Horarios.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoverAsync_ComAlunosAlocados_RejeitaComHorarioComAlunosAlocadosException()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);
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
        var servico = new HorarioService(repositorio, Clock);

        var acao = () => servico.RemoverAsync(ProfessorId, Guid.NewGuid(), CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioNaoEncontradoException>();
    }

    [Fact]
    public async Task RemoverAsync_HorarioDeOutroProfessor_RejeitaComHorarioNaoEncontradoException()
    {
        var repositorio = new FakeHorarioRepository();
        var servico = new HorarioService(repositorio, Clock);
        var horario = await servico.CadastrarAsync(ProfessorId, DiaSemana.Terca, new TimeOnly(10, 0), 60, CancellationToken.None);

        var acao = () => servico.RemoverAsync(Guid.NewGuid(), horario.Id, CancellationToken.None);

        await acao.Should().ThrowAsync<HorarioNaoEncontradoException>();
        repositorio.Horarios.Should().ContainSingle();
    }
}
