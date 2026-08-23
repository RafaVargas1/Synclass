using Synclass.Domain.Cobrancas;
using Synclass.Domain.Frequencias;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Repositório em memória usado nos testes de unidade do Domain, no lugar de
/// um banco real (ver docs/spec/code-style.md#testes). Mesmo padrão de
/// <see cref="FakeCancelamentoAulaRepository"/>. <paramref name="aulas"/>
/// (opcional, issue #186) resolve <c>Aula.Data</c> por <c>AulaId</c> para
/// <see cref="ContarPresencasNoPeriodoAsync"/> — só precisa ser informado
/// pelos testes que de fato exercitam esse método.
/// </summary>
public sealed class FakeRegistroFrequenciaRepository : IRegistroFrequenciaRepository
{
    private readonly List<RegistroFrequencia> _registros = new();
    private readonly FakeAulaRepository? _aulas;

    public FakeRegistroFrequenciaRepository(FakeAulaRepository? aulas = null)
    {
        _aulas = aulas;
    }

    public IReadOnlyCollection<RegistroFrequencia> Registros => _registros.AsReadOnly();

    public Task<RegistroFrequencia?> BuscarAsync(Guid aulaId, Guid matriculaId, CancellationToken cancellationToken)
    {
        var registro = _registros.FirstOrDefault(r => r.AulaId == aulaId && r.MatriculaId == matriculaId);
        return Task.FromResult(registro);
    }

    public Task<int> ContarPresencasNoPeriodoAsync(Guid matriculaId, PeriodoConsulta periodo, CancellationToken cancellationToken)
    {
        if (_aulas is null)
        {
            throw new InvalidOperationException(
                "FakeRegistroFrequenciaRepository precisa de um FakeAulaRepository para resolver Aula.Data.");
        }

        var quantidade = _registros.Count(registro =>
        {
            if (registro.MatriculaId != matriculaId || registro.StatusProfessor != StatusFrequencia.Presente)
            {
                return false;
            }

            var aula = _aulas.Aulas.FirstOrDefault(a => a.Id == registro.AulaId);
            return aula is not null && aula.Data >= periodo.Inicio && aula.Data < periodo.FimExclusivo;
        });
        return Task.FromResult(quantidade);
    }

    public Task AdicionarAsync(RegistroFrequencia registro, CancellationToken cancellationToken)
    {
        _registros.Add(registro);
        return Task.CompletedTask;
    }

    public Task SalvarAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
