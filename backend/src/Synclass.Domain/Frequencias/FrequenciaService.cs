using Synclass.Domain.Alocacoes;
using Synclass.Domain.Aulas;
using Synclass.Domain.Common;
using Synclass.Domain.Horarios;

namespace Synclass.Domain.Frequencias;

/// <summary>
/// Orquestra o único caso de uso do card (issue #14): o Professor registra,
/// em lote, a presença/ausência de cada Aluno alocado num horário/data.
/// Reaproveita <see cref="AulaService.ObterOuCriarAulaAsync"/> (instanciação
/// sob demanda, issue #10) e <see cref="HorarioService.BuscarDoProfessorAsync"/>
/// (checagem de posse), mesmo padrão de <see cref="AulaService.CancelarAsync"/>
/// — ver docs/specs/14-registro-frequencia/implementation.md.
/// </summary>
public sealed class FrequenciaService
{
    private readonly IRegistroFrequenciaRepository _registros;
    private readonly IAlocacaoHorarioRepository _alocacoes;
    private readonly AulaService _aulaService;
    private readonly HorarioService _horarioService;
    private readonly IClock _clock;

    public FrequenciaService(
        IRegistroFrequenciaRepository registros,
        IAlocacaoHorarioRepository alocacoes,
        AulaService aulaService,
        HorarioService horarioService,
        IClock clock)
    {
        _registros = registros;
        _alocacoes = alocacoes;
        _aulaService = aulaService;
        _horarioService = horarioService;
        _clock = clock;
    }

    public async Task<IReadOnlyCollection<RegistroFrequencia>> RegistrarAsync(
        Guid professorId,
        Guid horarioId,
        DateOnly data,
        IReadOnlyDictionary<Guid, StatusFrequencia> statusPorMatricula,
        CancellationToken cancellationToken)
    {
        await _horarioService.BuscarDoProfessorAsync(professorId, horarioId, cancellationToken);
        await GarantirMatriculasAlocadasAsync(horarioId, statusPorMatricula.Keys, cancellationToken);

        var aula = await _aulaService.ObterOuCriarAulaAsync(horarioId, data, cancellationToken);

        var registros = new List<RegistroFrequencia>();
        foreach (var (matriculaId, status) in statusPorMatricula)
        {
            var registro = await RegistrarUmAsync(aula.Id, matriculaId, status, cancellationToken);
            registros.Add(registro);
        }

        return registros;
    }

    private async Task<RegistroFrequencia> RegistrarUmAsync(
        Guid aulaId, Guid matriculaId, StatusFrequencia status, CancellationToken cancellationToken)
    {
        var registro = await _registros.BuscarAsync(aulaId, matriculaId, cancellationToken);
        if (registro is null)
        {
            registro = RegistroFrequencia.Criar(aulaId, matriculaId, _clock);
            await _registros.AdicionarAsync(registro, cancellationToken);
        }

        registro.RegistrarProfessor(status, _clock);
        await _registros.SalvarAsync(cancellationToken);
        return registro;
    }

    private async Task GarantirMatriculasAlocadasAsync(
        Guid horarioId, IEnumerable<Guid> matriculaIds, CancellationToken cancellationToken)
    {
        foreach (var matriculaId in matriculaIds)
        {
            var alocacao = await _alocacoes.BuscarAsync(horarioId, matriculaId, cancellationToken);
            if (alocacao is null)
            {
                // Nome ambíguo com Synclass.Domain.Alocacoes.AlocacaoNaoEncontradaException
                // (mesmo namespace importado neste arquivo) — reaproveita a
                // exceção de Synclass.Domain.Aulas, mesmo padrão de
                // AulaService.GarantirMatriculaAlocadaAsync (issue #10).
                throw new Synclass.Domain.Aulas.AlocacaoNaoEncontradaException(horarioId, matriculaId);
            }
        }
    }
}
