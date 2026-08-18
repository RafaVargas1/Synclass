using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Matriculas;

namespace Synclass.Api.Controllers;

/// <summary>
/// Define e consulta a regra de cobrança vigente de uma matrícula (issue
/// #11). Antes de qualquer operação, resolve a <see cref="Matricula"/> por
/// <c>matriculaId</c> e confere <c>ProfessorId == professorId</c> da rota —
/// 404 se não bater ou não existir (ver implementation.md).
/// </summary>
[ApiController]
[Route("professores/{professorId:guid}/matriculas/{matriculaId:guid}/regra-de-cobranca")]
public sealed class RegraDeCobrancaController : ControllerBase
{
    private readonly RegraDeCobrancaService _regraDeCobrancaService;
    private readonly IMatriculaRepository _matriculas;
    private readonly ILogger<RegraDeCobrancaController> _logger;

    public RegraDeCobrancaController(
        RegraDeCobrancaService regraDeCobrancaService, IMatriculaRepository matriculas, ILogger<RegraDeCobrancaController> logger)
    {
        _regraDeCobrancaService = regraDeCobrancaService;
        _matriculas = matriculas;
        _logger = logger;
    }

    [HttpPut]
    public async Task<IActionResult> Definir(
        Guid professorId, Guid matriculaId, [FromBody] DefinirRegraDeCobrancaRequest request, CancellationToken cancellationToken)
    {
        if (!await MatriculaPertenceAoProfessorAsync(professorId, matriculaId, cancellationToken))
        {
            return NotFound();
        }

        return await AplicarDefinicaoAsync(matriculaId, request, cancellationToken);
    }

    private async Task<IActionResult> AplicarDefinicaoAsync(
        Guid matriculaId, DefinirRegraDeCobrancaRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        try
        {
            var tipo = (TipoRegraDeCobranca)Enum.Parse(typeof(TipoRegraDeCobranca), request.Tipo);
            var resultado = await _regraDeCobrancaService.DefinirAsync(
                matriculaId, tipo, request.Valor, request.FrequenciaSemanalContratada, cancellationToken);
            LogRegraDefinida(trackId, matriculaId, tipo, resultado.ValorAnterior);
            return Ok(ParaResponse(resultado.Regra));
        }
        catch (Exception ex) when (ex is ArgumentException or FrequenciaSemanalContratadaInvalidaException
            or FrequenciaSemanalContratadaAusenteException or FrequenciaSemanalContratadaNaoEsperadaException
            or ValorDeRegraDeCobrancaInvalidoException)
        {
            return BadRequest(new RegraDeCobrancaErrorResponse(ex.Message));
        }
        catch (MatriculaNaoEncontradaException)
        {
            return NotFound();
        }
        catch (RegraDeCobrancaConflitanteException ex)
        {
            return Conflict(new RegraDeCobrancaErrorResponse(ex.Message));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Buscar(Guid professorId, Guid matriculaId, CancellationToken cancellationToken)
    {
        if (!await MatriculaPertenceAoProfessorAsync(professorId, matriculaId, cancellationToken))
        {
            return NotFound();
        }

        var regra = await _regraDeCobrancaService.BuscarVigenteAsync(matriculaId, cancellationToken);
        if (regra is null)
        {
            return NotFound();
        }

        return Ok(ParaResponse(regra));
    }

    private async Task<bool> MatriculaPertenceAoProfessorAsync(Guid professorId, Guid matriculaId, CancellationToken cancellationToken)
    {
        var matricula = await _matriculas.BuscarPorIdAsync(matriculaId, cancellationToken);
        return matricula is not null && matricula.ProfessorId == professorId;
    }

    private static RegraDeCobrancaResponse ParaResponse(RegraDeCobranca regra)
    {
        var frequenciaSemanalContratada = regra is RegraValorPorAula regraValorPorAula ? regraValorPorAula.FrequenciaSemanalContratada : (int?)null;
        return new RegraDeCobrancaResponse(regra.MatriculaId, TipoDaRegra(regra), regra.Valor, frequenciaSemanalContratada);
    }

    private static string TipoDaRegra(RegraDeCobranca regra)
    {
        return regra switch
        {
            RegraValorPorAula => nameof(TipoRegraDeCobranca.ValorPorAula),
            RegraFixoMensal => nameof(TipoRegraDeCobranca.FixoMensal),
            RegraFixoPorAula => nameof(TipoRegraDeCobranca.FixoPorAula),
            _ => throw new ArgumentOutOfRangeException(nameof(regra), regra.GetType().Name, "Implementação de RegraDeCobranca não mapeada para resposta."),
        };
    }

    private void LogRegraDefinida(string trackId, Guid matriculaId, TipoRegraDeCobranca tipo, decimal? valorAnterior)
    {
        _logger.LogInformation(
            "RegraDeCobrancaDefinida {TrackId} {MatriculaId} {Tipo} {ValorAnterior}",
            trackId, matriculaId, tipo, valorAnterior);
    }
}

public sealed record DefinirRegraDeCobrancaRequest(string Tipo, decimal Valor, int? FrequenciaSemanalContratada);

public sealed record RegraDeCobrancaResponse(Guid MatriculaId, string Tipo, decimal Valor, int? FrequenciaSemanalContratada);

public sealed record RegraDeCobrancaErrorResponse(string Mensagem);
