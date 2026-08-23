using Synclass.Domain.Common;
using Synclass.Domain.Matriculas;

namespace Synclass.Domain.Cobrancas;

/// <summary>
/// Orquestra a definição da regra de cobrança de uma matrícula (issue #11):
/// valida que a <see cref="Matricula"/> existe, busca a regra anterior (para
/// o log de <c>ValorAnterior</c> emitido pelo controller), constrói a
/// implementação concreta certa a partir de <see cref="TipoRegraDeCobranca"/>
/// e persiste via <see cref="IRegraDeCobrancaRepository.SalvarAsync"/>
/// (upsert — nunca duas linhas para a mesma matrícula).
/// </summary>
public sealed class RegraDeCobrancaService
{
    private readonly IRegraDeCobrancaRepository _regras;
    private readonly IMatriculaRepository _matriculas;
    private readonly IClock _clock;

    public RegraDeCobrancaService(IRegraDeCobrancaRepository regras, IMatriculaRepository matriculas, IClock clock)
    {
        _regras = regras;
        _matriculas = matriculas;
        _clock = clock;
    }

    public async Task<DefinicaoDeRegraDeCobrancaResultado> DefinirAsync(
        Guid matriculaId,
        TipoRegraDeCobranca tipo,
        decimal valor,
        int? frequenciaSemanalContratada,
        BaseDeContagemAula? baseDeContagemAula,
        CancellationToken cancellationToken)
    {
        await GarantirMatriculaExisteAsync(matriculaId, cancellationToken);
        GarantirFrequenciaCoerenteComTipo(tipo, frequenciaSemanalContratada);
        GarantirBaseDeContagemCoerenteComTipo(tipo, baseDeContagemAula);

        var regraAnterior = await _regras.BuscarPorMatriculaAsync(matriculaId, cancellationToken);
        var novaRegra = ConstruirRegra(matriculaId, tipo, valor, frequenciaSemanalContratada, baseDeContagemAula);

        await _regras.SalvarAsync(novaRegra, cancellationToken);
        return new DefinicaoDeRegraDeCobrancaResultado(novaRegra, regraAnterior?.Valor);
    }

    public Task<RegraDeCobranca?> BuscarVigenteAsync(Guid matriculaId, CancellationToken cancellationToken)
    {
        return _regras.BuscarPorMatriculaAsync(matriculaId, cancellationToken);
    }

    private async Task GarantirMatriculaExisteAsync(Guid matriculaId, CancellationToken cancellationToken)
    {
        var matricula = await _matriculas.BuscarPorIdAsync(matriculaId, cancellationToken);
        if (matricula is null)
        {
            throw new MatriculaNaoEncontradaException(matriculaId);
        }
    }

    private RegraDeCobranca ConstruirRegra(
        Guid matriculaId, TipoRegraDeCobranca tipo, decimal valor, int? frequenciaSemanalContratada, BaseDeContagemAula? baseDeContagemAula)
    {
        var baseDeContagemResolvida = baseDeContagemAula ?? BaseDeContagemAula.Agendamento;
        return tipo switch
        {
            TipoRegraDeCobranca.FixoMensal => RegraFixoMensal.Criar(matriculaId, valor, _clock),
            TipoRegraDeCobranca.FixoPorAula => RegraFixoPorAula.Criar(matriculaId, valor, _clock, baseDeContagemResolvida),
            TipoRegraDeCobranca.ValorPorAula => RegraValorPorAula.Criar(
                matriculaId,
                valor,
                frequenciaSemanalContratada ?? throw new FrequenciaSemanalContratadaAusenteException(),
                _clock,
                baseDeContagemResolvida),
            _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, $"Tipo de regra de cobrança inválido: {tipo}."),
        };
    }

    private static void GarantirFrequenciaCoerenteComTipo(TipoRegraDeCobranca tipo, int? frequenciaSemanalContratada)
    {
        if (tipo != TipoRegraDeCobranca.ValorPorAula && frequenciaSemanalContratada is not null)
        {
            throw new FrequenciaSemanalContratadaNaoEsperadaException(tipo);
        }
    }

    private static void GarantirBaseDeContagemCoerenteComTipo(TipoRegraDeCobranca tipo, BaseDeContagemAula? baseDeContagemAula)
    {
        if (tipo == TipoRegraDeCobranca.FixoMensal && baseDeContagemAula is not null)
        {
            throw new BaseDeContagemAulaNaoEsperadaException(tipo);
        }
    }
}

/// <summary>
/// Resultado de <see cref="RegraDeCobrancaService.DefinirAsync"/>:
/// <see cref="ValorAnterior"/> é <c>null</c> quando a matrícula não tinha
/// regra configurada antes desta chamada — insumo do evento de log
/// <c>RegraDeCobrancaDefinida</c> (ver architecture.md#logs-estruturados-e-track-id).
/// </summary>
public sealed record DefinicaoDeRegraDeCobrancaResultado(RegraDeCobranca Regra, decimal? ValorAnterior);
