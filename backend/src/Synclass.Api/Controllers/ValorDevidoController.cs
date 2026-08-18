using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Common;

namespace Synclass.Api.Controllers;

/// <summary>
/// Consulta do valor devido por cada Aluno de um Professor num período
/// (issue #12). <c>inicio</c>/<c>fim</c> são opcionais — ausentes os dois,
/// usa <see cref="PeriodoConsulta.MesCorrente"/>; informado só um dos dois,
/// é request inválido (não há "meio-padrão", ver implementation.md).
/// </summary>
[Authorize(Roles = "Professor")]
[ApiController]
[Route("professores/{professorId:guid}/valor-devido")]
public sealed class ValorDevidoController : ControllerBase
{
    private readonly ConsultaCobrancaService _consultaCobranca;
    private readonly IClock _clock;
    private readonly ILogger<ValorDevidoController> _logger;

    public ValorDevidoController(ConsultaCobrancaService consultaCobranca, IClock clock, ILogger<ValorDevidoController> logger)
    {
        _consultaCobranca = consultaCobranca;
        _clock = clock;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Consultar(
        Guid professorId, [FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, CancellationToken cancellationToken)
    {
        if (!TentarResolverPeriodo(inicio, fim, out var periodo, out var erro))
        {
            return BadRequest(new ValorDevidoErrorResponse(erro!));
        }

        var valoresDevidos = await _consultaCobranca.ConsultarPorProfessorAsync(professorId, periodo!, cancellationToken);
        LogConsultaRealizada(professorId, periodo!);
        return Ok(valoresDevidos.Select(ParaResponse));
    }

    private bool TentarResolverPeriodo(DateOnly? inicio, DateOnly? fim, out PeriodoConsulta? periodo, out string? erro)
    {
        erro = null;
        if (inicio is null && fim is null)
        {
            periodo = PeriodoConsulta.MesCorrente(_clock);
            return true;
        }

        if (inicio is null || fim is null)
        {
            periodo = null;
            erro = MensagemPeriodoIncompleto(inicio, fim);
            return false;
        }

        return TentarCriarPeriodo(inicio.Value, fim.Value, out periodo, out erro);
    }

    private static bool TentarCriarPeriodo(DateOnly inicio, DateOnly fim, out PeriodoConsulta? periodo, out string? erro)
    {
        try
        {
            periodo = PeriodoConsulta.Criar(inicio, fim);
            erro = null;
            return true;
        }
        catch (PeriodoConsultaInvalidoException ex)
        {
            periodo = null;
            erro = ex.Message;
            return false;
        }
    }

    private static string MensagemPeriodoIncompleto(DateOnly? inicio, DateOnly? fim)
    {
        return $"Período incompleto: inicio={inicio}, fim={fim}. Informe os dois parâmetros ou nenhum (usa o mês corrente).";
    }

    private static ValorDevidoResponse ParaResponse(ValorDevidoPorMatricula valorDevido)
    {
        return new ValorDevidoResponse(
            valorDevido.MatriculaId, valorDevido.AlunoUsuarioId, valorDevido.Nome, valorDevido.Valor, valorDevido.SemRegraDefinida);
    }

    private void LogConsultaRealizada(Guid professorId, PeriodoConsulta periodo)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        _logger.LogInformation(
            "ConsultaValorDevidoRealizada {TrackId} {ProfessorId} {PeriodoInicio} {PeriodoFim}",
            trackId, professorId, periodo.Inicio, periodo.FimExclusivo);
    }
}

public sealed record ValorDevidoResponse(Guid MatriculaId, Guid? AlunoUsuarioId, string Nome, decimal? Valor, bool SemRegraDefinida);

public sealed record ValorDevidoErrorResponse(string Mensagem);
