using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Cobrancas;
using Synclass.Domain.Common;
using Synclass.Domain.Frequencias;
using Synclass.Domain.Horarios;

namespace Synclass.Api.Controllers;

/// <summary>
/// Consulta do histórico de frequência do Aluno autenticado, detalhado por
/// Professor (issue #16) — reaproveita <see cref="FrequenciaService"/>
/// (issues #14/#15) e o mesmo contrato de período de
/// <see cref="ValorDevidoAlunoController"/> (issue #13): <c>inicio</c>/
/// <c>fim</c> ausentes os dois usa <see cref="PeriodoConsulta.MesCorrente"/>,
/// só um dos dois é request inválido. <c>alunoUsuarioId</c> vem de
/// <see cref="ClaimsPrincipalExtensions.GetUsuarioId"/> (token da sessão),
/// mesmo padrão de <see cref="ValorDevidoAlunoController"/> — não existe
/// "histórico de outro Aluno" a proteger.
/// </summary>
[Authorize(Roles = "Aluno")]
[ApiController]
[Route("alunos/historico-frequencia")]
public sealed class HistoricoFrequenciaController : ControllerBase
{
    private readonly FrequenciaService _frequenciaService;
    private readonly IClock _clock;
    private readonly ILogger<HistoricoFrequenciaController> _logger;

    public HistoricoFrequenciaController(FrequenciaService frequenciaService, IClock clock, ILogger<HistoricoFrequenciaController> logger)
    {
        _frequenciaService = frequenciaService;
        _clock = clock;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Consultar([FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, CancellationToken cancellationToken)
    {
        if (!TentarResolverPeriodo(inicio, fim, out var periodo, out var erro))
        {
            return BadRequest(new HistoricoFrequenciaErrorResponse(erro!));
        }

        var alunoUsuarioId = User.GetUsuarioId();
        var historico = await _frequenciaService.ListarHistoricoAsync(alunoUsuarioId, periodo!, cancellationToken);
        LogConsultaRealizada(alunoUsuarioId, periodo!);
        return Ok(historico.Select(ParaResponse));
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

    private static HistoricoFrequenciaPorProfessorResponse ParaResponse(HistoricoFrequenciaPorProfessor historico)
    {
        return new HistoricoFrequenciaPorProfessorResponse(
            historico.ProfessorId, historico.NomeProfessor, historico.Aulas.Select(ParaResponse).ToList());
    }

    private static AulaFrequenciaHistoricoResponse ParaResponse(AulaFrequenciaHistorico aula)
    {
        return new AulaFrequenciaHistoricoResponse(
            aula.HorarioId, aula.Data, (int)aula.DiaSemana, aula.HoraInicio, aula.Status.ToString());
    }

    private void LogConsultaRealizada(Guid alunoUsuarioId, PeriodoConsulta periodo)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        _logger.LogInformation(
            "HistoricoFrequenciaConsultado {TrackId} {UsuarioId} {PeriodoInicio} {PeriodoFim}",
            trackId, alunoUsuarioId, periodo.Inicio, periodo.FimExclusivo);
    }
}

public sealed record HistoricoFrequenciaPorProfessorResponse(
    Guid ProfessorId, string NomeProfessor, IReadOnlyCollection<AulaFrequenciaHistoricoResponse> Aulas);

public sealed record AulaFrequenciaHistoricoResponse(Guid HorarioId, DateOnly Data, int DiaSemana, TimeOnly HoraInicio, string Status);

public sealed record HistoricoFrequenciaErrorResponse(string Mensagem);
