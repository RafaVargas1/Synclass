using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Configuracoes;
using Synclass.Domain.Horarios;

namespace Synclass.Api.Controllers;

[ApiController]
[Route("professores/{professorId:guid}/horarios")]
public sealed class HorariosController : ControllerBase
{
    private readonly HorarioService _horarioService;
    private readonly ILogger<HorariosController> _logger;

    public HorariosController(HorarioService horarioService, ILogger<HorariosController> logger)
    {
        _horarioService = horarioService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Criar(Guid professorId, [FromBody] CriarHorarioRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        try
        {
            var horario = await _horarioService.CadastrarAsync(
                professorId, (DiaSemana)request.DiaSemana, request.HoraInicio, request.DuracaoMinutos, cancellationToken);
            LogHorarioCriado(trackId, horario);
            return Ok(ParaResponse(horario));
        }
        catch (HorarioConflitanteException ex)
        {
            LogRejeicaoPorConflito(trackId, professorId, ex);
            return BadRequest(new HorarioErrorResponse(ex.Message));
        }
        catch (HorarioRejeitadoException ex)
        {
            return BadRequest(new HorarioErrorResponse(ex.Message));
        }
        catch (ModeloAgendamentoNaoDefinidoException ex)
        {
            return BadRequest(new HorarioErrorResponse(ex.Message));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Listar(Guid professorId, CancellationToken cancellationToken)
    {
        var horarios = await _horarioService.ListarAsync(professorId, cancellationToken);
        return Ok(horarios.Select(ParaResponse));
    }

    [HttpDelete("{horarioId:guid}")]
    public async Task<IActionResult> Remover(Guid professorId, Guid horarioId, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        try
        {
            await _horarioService.RemoverAsync(professorId, horarioId, cancellationToken);
            return NoContent();
        }
        catch (HorarioNaoEncontradoException)
        {
            return NotFound();
        }
        catch (HorarioComAlunosAlocadosException ex)
        {
            LogRejeicaoPorAlunosAlocados(trackId, horarioId);
            return Conflict(new HorarioErrorResponse(ex.Message));
        }
    }

    private static HorarioResponse ParaResponse(Horario horario)
    {
        return new HorarioResponse(horario.Id, (int)horario.DiaSemana, horario.HoraInicio, horario.DuracaoMinutos);
    }

    private void LogHorarioCriado(string trackId, Horario horario)
    {
        _logger.LogInformation(
            "HorarioCriado {TrackId} {ProfessorId} {HorarioId}",
            trackId, horario.ProfessorId, horario.Id);
    }

    private void LogRejeicaoPorConflito(string trackId, Guid professorId, HorarioConflitanteException ex)
    {
        _logger.LogWarning(
            "HorarioRejeitadoPorConflito {TrackId} {ProfessorId} {HorarioConflitanteId}",
            trackId, professorId, ex.HorarioConflitanteId);
    }

    private void LogRejeicaoPorAlunosAlocados(string trackId, Guid horarioId)
    {
        _logger.LogWarning(
            "HorarioRejeitadoPorAlunosAlocados {TrackId} {HorarioId}",
            trackId, horarioId);
    }
}

public sealed record CriarHorarioRequest(int DiaSemana, TimeOnly HoraInicio, int DuracaoMinutos);

public sealed record HorarioResponse(Guid Id, int DiaSemana, TimeOnly HoraInicio, int DuracaoMinutos);

public sealed record HorarioErrorResponse(string Mensagem);
