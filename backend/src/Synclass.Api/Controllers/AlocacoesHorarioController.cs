using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Alocacoes;
using Synclass.Domain.Horarios;

namespace Synclass.Api.Controllers;

/// <summary>
/// Alocação de Alunos a horários específicos (issue #8) — o Professor
/// atribui, desfaz e lista os Alunos alocados a um horário disponível
/// (template recorrente, ver <c>HorariosController</c>).
/// </summary>
[ApiController]
[Route("professores/{professorId:guid}/horarios/{horarioId:guid}/alocacoes")]
public sealed class AlocacoesHorarioController : ControllerBase
{
    private readonly AlocacaoHorarioService _alocacaoHorarioService;
    private readonly ILogger<AlocacoesHorarioController> _logger;

    public AlocacoesHorarioController(AlocacaoHorarioService alocacaoHorarioService, ILogger<AlocacoesHorarioController> logger)
    {
        _alocacaoHorarioService = alocacaoHorarioService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Alocar(
        Guid professorId, Guid horarioId, [FromBody] CriarAlocacaoHorarioRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        try
        {
            var alocacao = await _alocacaoHorarioService.AlocarAsync(
                professorId, horarioId, request.MatriculaId, cancellationToken);
            LogAlunoAlocado(trackId, alocacao);
            return Ok(ParaResponse(alocacao));
        }
        catch (HorarioNaoEncontradoException)
        {
            return NotFound();
        }
        catch (AlocacaoRejeitadaException ex)
        {
            LogAlocacaoRejeitada(trackId, professorId, horarioId, ex);
            return BadRequest(new AlocacaoHorarioErrorResponse(ex.Message));
        }
    }

    private static AlocacaoHorarioResponse ParaResponse(AlocacaoHorario alocacao)
    {
        return new AlocacaoHorarioResponse(alocacao.Id, alocacao.HorarioId, alocacao.MatriculaId, alocacao.CreatedAt);
    }

    private void LogAlunoAlocado(string trackId, AlocacaoHorario alocacao)
    {
        _logger.LogInformation(
            "AlunoAlocadoEmHorario {TrackId} {HorarioId} {MatriculaId} {AlocacaoId}",
            trackId, alocacao.HorarioId, alocacao.MatriculaId, alocacao.Id);
    }

    private void LogAlocacaoRejeitada(string trackId, Guid professorId, Guid horarioId, AlocacaoRejeitadaException ex)
    {
        _logger.LogWarning(
            "AlocacaoRejeitada {TrackId} {ProfessorId} {HorarioId} {Motivo}",
            trackId, professorId, horarioId, ex.GetType().Name);
    }
}

public sealed record CriarAlocacaoHorarioRequest(Guid MatriculaId);

public sealed record AlocacaoHorarioResponse(Guid Id, Guid HorarioId, Guid MatriculaId, DateTimeOffset CreatedAt);

public sealed record AlocacaoHorarioErrorResponse(string Mensagem);
