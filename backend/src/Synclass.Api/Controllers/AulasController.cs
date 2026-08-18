using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Alocacoes;
using Synclass.Domain.Aulas;
using Synclass.Domain.Horarios;

namespace Synclass.Api.Controllers;

/// <summary>
/// Cancelamento de aula pelo Aluno, com antecedência mínima configurável
/// pelo Professor (issue #10) — o Aluno consulta as próximas aulas em que
/// está alocado e cancela uma delas, respeitando o prazo. Mesmo padrão de
/// rota/autorização de <see cref="MarcacoesHorarioController"/> (issue #9).
/// </summary>
[ApiController]
[Route("professores/{professorId:guid}/horarios")]
[Authorize(Roles = "Aluno")]
public sealed class AulasController : ControllerBase
{
    private readonly AulaService _aulaService;
    private readonly IHorarioRepository _horarios;
    private readonly ILogger<AulasController> _logger;

    public AulasController(AulaService aulaService, IHorarioRepository horarios, ILogger<AulasController> logger)
    {
        _aulaService = aulaService;
        _horarios = horarios;
        _logger = logger;
    }

    [HttpGet("proximas-aulas")]
    public async Task<IActionResult> ListarProximasAulas(
        Guid professorId, [FromQuery] Guid matriculaId, CancellationToken cancellationToken)
    {
        try
        {
            var proximas = await _aulaService.ListarProximasAsync(professorId, matriculaId, cancellationToken);
            return Ok(proximas.Select(ParaResponse));
        }
        catch (MatriculaNaoVinculadaAoProfessorException ex)
        {
            return BadRequest(new AulaErrorResponse(ex.Message));
        }
    }

    [HttpPost("{horarioId:guid}/aulas/{data}/cancelamentos")]
    public async Task<IActionResult> Cancelar(
        Guid professorId, Guid horarioId, DateOnly data, [FromBody] CancelarAulaRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        try
        {
            var cancelamento = await _aulaService.CancelarAsync(
                professorId, horarioId, data, request.MatriculaId, cancellationToken);
            await LogAulaCanceladaAsync(trackId, horarioId, data, cancelamento, cancellationToken);
            return Ok(ParaResponse(cancelamento));
        }
        catch (HorarioNaoEncontradoException)
        {
            return NotFound();
        }
        catch (PrazoCancelamentoExpiradoException ex)
        {
            LogCancelamentoRejeitadoPorPrazo(trackId, request.MatriculaId, ex);
            return BadRequest(new AulaErrorResponse(ex.Message));
        }
        catch (AulaRejeitadaException ex)
        {
            return BadRequest(new AulaErrorResponse(ex.Message));
        }
    }

    private static AulaProximaResponse ParaResponse(AulaProxima aulaProxima)
    {
        return new AulaProximaResponse(
            aulaProxima.HorarioId,
            aulaProxima.Data.ToString("yyyy-MM-dd"),
            (int)aulaProxima.DiaSemana,
            aulaProxima.HoraInicio,
            aulaProxima.DuracaoMinutos,
            aulaProxima.PodeCancelar,
            aulaProxima.CancelavelAte,
            aulaProxima.PrazoCancelamentoMinutos);
    }

    private static CancelamentoAulaResponse ParaResponse(CancelamentoAula cancelamento)
    {
        return new CancelamentoAulaResponse(cancelamento.Id, cancelamento.AulaId, cancelamento.MatriculaId, cancelamento.CanceladoEm);
    }

    /// <summary>
    /// Evento <c>AulaCancelada</c> (Information) — inclui os minutos de
    /// antecedência efetivos entre o cancelamento e o início da aula (ver
    /// docs/specs/10-cancelamento-aula/task.md#logs).
    /// </summary>
    private async Task LogAulaCanceladaAsync(
        string trackId, Guid horarioId, DateOnly data, CancelamentoAula cancelamento, CancellationToken cancellationToken)
    {
        var horario = await _horarios.BuscarPorIdAsync(horarioId, cancellationToken);
        var inicioAula = new DateTimeOffset(data.ToDateTime(horario!.HoraInicio), TimeSpan.Zero);
        var antecedenciaEfetivaMinutos = (inicioAula - cancelamento.CanceladoEm).TotalMinutes;

        _logger.LogInformation(
            "AulaCancelada {TrackId} {MatriculaId} {AulaId} {AntecedenciaEfetivaMinutos}",
            trackId, cancelamento.MatriculaId, cancelamento.AulaId, antecedenciaEfetivaMinutos);
    }

    /// <summary>
    /// Evento <c>CancelamentoRejeitadoPorPrazo</c> (Warning) — prazo
    /// configurado vs. antecedência tentada.
    /// </summary>
    private void LogCancelamentoRejeitadoPorPrazo(string trackId, Guid matriculaId, PrazoCancelamentoExpiradoException ex)
    {
        var antecedenciaTentadaMinutos = (ex.Limite.AddMinutes(ex.PrazoCancelamentoMinutos) - DateTimeOffset.UtcNow).TotalMinutes;

        _logger.LogWarning(
            "CancelamentoRejeitadoPorPrazo {TrackId} {MatriculaId} {AulaId} {PrazoCancelamentoMinutos} {AntecedenciaTentadaMinutos}",
            trackId, matriculaId, ex.AulaId, ex.PrazoCancelamentoMinutos, antecedenciaTentadaMinutos);
    }
}

public sealed record CancelarAulaRequest(Guid MatriculaId);

public sealed record AulaProximaResponse(
    Guid HorarioId,
    string Data,
    int DiaSemana,
    TimeOnly HoraInicio,
    int DuracaoMinutos,
    bool PodeCancelar,
    DateTimeOffset CancelavelAte,
    int PrazoCancelamentoMinutos);

public sealed record CancelamentoAulaResponse(Guid Id, Guid AulaId, Guid MatriculaId, DateTimeOffset CanceladoEm);

public sealed record AulaErrorResponse(string Mensagem);
