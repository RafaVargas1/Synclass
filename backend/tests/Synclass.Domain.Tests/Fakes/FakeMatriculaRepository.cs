using Synclass.Domain.Matriculas;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Repositório em memória usado nos testes de unidade do Domain, no lugar de
/// um banco real (ver docs/spec/code-style.md#testes — mocke I/O externo com
/// classes fake nomeadas, não stubs inline).
/// </summary>
public sealed class FakeMatriculaRepository : IMatriculaRepository
{
    private readonly List<Matricula> _matriculas = new();

    public IReadOnlyCollection<Matricula> Matriculas => _matriculas.AsReadOnly();

    public Task<Matricula?> BuscarPorIdentificadorAsync(
        Guid professorId, string identificadorProvisorio, CancellationToken cancellationToken)
    {
        var matricula = _matriculas.FirstOrDefault(
            m => m.ProfessorId == professorId && m.IdentificadorProvisorio == identificadorProvisorio);
        return Task.FromResult(matricula);
    }

    public Task AdicionarAsync(Matricula matricula, CancellationToken cancellationToken)
    {
        _matriculas.Add(matricula);
        return Task.CompletedTask;
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
