using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Common;
using Synclass.Domain.Pagamentos;

namespace Synclass.Api.Controllers;

/// <summary>
/// Consulta do valor devido pelo Aluno autenticado, detalhado por Professor
/// (issue #13) — reaproveita <see cref="ConsultaCobrancaService"/> criado
/// pela issue #12 (ver implementation.md#reaproveitamento-do-serviço-de-domínio-da-issue-12).
/// Desde a issue #199, após a consulta o resultado passa por
/// <see cref="ValorDevidoService.DescontarPagamentosConfirmadosAsync"/>:
/// matrículas com um <see cref="Pagamentos.Pagamento"/> <c>Confirmado</c> no
/// mesmo (MatriculaId, período) saem da lista — o <c>ConsultaCobrancaService</c>
/// em si não é alterado (Professor usa o mesmo serviço sem desconto nesta
/// Task; ver implementation.md#desconto-de-pagamentos-confirmados-no-get-alunos-valor-devido).
/// <c>alunoUsuarioId</c> vem de
/// <see cref="ClaimsPrincipalExtensions.GetUsuarioId"/> (token da sessão),
/// não de parâmetro de rota — mesmo padrão de
/// <see cref="MarcacoesHorarioController"/>: não existe "lista de valor
/// devido de outro Aluno" a proteger, então nem faz sentido expor o
/// parâmetro. <c>inicio</c>/<c>fim</c> seguem o mesmo contrato de
/// <see cref="ValorDevidoController"/> — ausentes os dois usa
/// <see cref="PeriodoConsulta.MesCorrente"/>, só um dos dois é request
/// inválido.
/// </summary>
[Authorize(Roles = "Aluno")]
[ApiController]
[Route("alunos/valor-devido")]
public sealed class ValorDevidoAlunoController : ControllerBase
{
    private readonly ConsultaCobrancaService _consultaCobranca;
    private readonly ValorDevidoService _valorDevido;
    private readonly IClock _clock;
    private readonly ILogger<ValorDevidoAlunoController> _logger;

    public ValorDevidoAlunoController(
        ConsultaCobrancaService consultaCobranca,
        ValorDevidoService valorDevido,
        IClock clock,
        ILogger<ValorDevidoAlunoController> logger)
    {
        _consultaCobranca = consultaCobranca;
        _valorDevido = valorDevido;
        _clock = clock;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Consultar([FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, CancellationToken cancellationToken)
    {
        if (!TentarResolverPeriodo(inicio, fim, out var periodo, out var erro))
        {
            return BadRequest(new ValorDevidoErrorResponse(erro!));
        }

        var alunoUsuarioId = User.GetUsuarioId();
        var valoresDevidos = await _consultaCobranca.ConsultarPorAlunoAsync(alunoUsuarioId, periodo!, cancellationToken);
        var semDescontar = await _valorDevido.DescontarPagamentosConfirmadosAsync(
            valoresDevidos.ToList(), alunoUsuarioId, periodo!.Inicio, periodo.FimExclusivo, cancellationToken);
        LogConsultaRealizada(alunoUsuarioId, periodo!);
        return Ok(semDescontar.Select(ParaResponse));
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

    private void LogConsultaRealizada(Guid alunoUsuarioId, PeriodoConsulta periodo)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        _logger.LogInformation(
            "ConsultaTotalDevidoRealizada {TrackId} {UsuarioId} {PeriodoInicio} {PeriodoFim}",
            trackId, alunoUsuarioId, periodo.Inicio, periodo.FimExclusivo);
    }
}
