using Synclass.Domain.Horarios;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Repositório em memória usado nos testes de unidade do Domain, no lugar de
/// um banco real (ver docs/spec/code-style.md#testes). O flag
/// <see cref="AlunosAlocadosPorHorario"/> simula, para teste, o resultado
/// que só uma consulta real (issue #8) devolveria em produção — ver
/// docs/specs/6-horarios-disponiveis/implementation.md#dependência-da-issue-8.
/// </summary>
public sealed class FakeHorarioRepository : IHorarioRepository
{
    private readonly List<Horario> _horarios = new();

    public IReadOnlyCollection<Horario> Horarios => _horarios.AsReadOnly();

    public HashSet<Guid> AlunosAlocadosPorHorario { get; } = new();

    public Task<IReadOnlyCollection<Horario>> ListarPorProfessorEDiaAsync(Guid professorId, DiaSemana diaSemana, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Horario> resultado = _horarios
            .Where(h => h.ProfessorId == professorId && h.DiaSemana == diaSemana)
            .ToList();
        return Task.FromResult(resultado);
    }

    public Task<IReadOnlyCollection<Horario>> ListarPorProfessorAsync(Guid professorId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Horario> resultado = _horarios
            .Where(h => h.ProfessorId == professorId)
            .ToList();
        return Task.FromResult(resultado);
    }

    public Task<Horario?> BuscarPorIdAsync(Guid horarioId, CancellationToken cancellationToken)
    {
        var horario = _horarios.FirstOrDefault(h => h.Id == horarioId);
        return Task.FromResult(horario);
    }

    public Task<bool> PossuiAlunosAlocadosAsync(Guid horarioId, CancellationToken cancellationToken)
    {
        return Task.FromResult(AlunosAlocadosPorHorario.Contains(horarioId));
    }

    public Task AdicionarAsync(Horario horario, CancellationToken cancellationToken)
    {
        _horarios.Add(horario);
        return Task.CompletedTask;
    }

    public Task RemoverAsync(Horario horario, CancellationToken cancellationToken)
    {
        _horarios.Remove(horario);
        return Task.CompletedTask;
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
