using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Horarios;

namespace Synclass.Api.Controllers;

[Authorize(Roles = "Professor")]
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
                professorId,
                (DiaSemana)request.DiaSemana,
                request.HoraInicio,
                request.DuracaoMinutos,
                (TipoMarcacao)request.TipoMarcacao,
                cancellationToken,
                request.LimiteAlunos,
                request.PrazoCancelamentoMinutos);
            LogHorarioCriado(trackId, horario);
            LogLimiteAlunosAlterado(trackId, horario);
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
    }

    [HttpGet]
    public async Task<IActionResult> Listar(Guid professorId, CancellationToken cancellationToken)
    {
        var horarios = await _horarioService.ListarAsync(professorId, cancellationToken);
        return Ok(horarios.Select(ParaResponse));
    }

    /// <summary>
    /// Altera a política de marcação (<c>tipoMarcacao</c>) de um horário já
    /// cadastrado (issue #71). Lê o valor anterior primeiro para registrar no
    /// log estruturado — mesmo padrão de "ler antes para logar o antes" de
    /// <see cref="ConfiguracoesController"/> (<c>modeloAnterior</c>). Rejeita
    /// com 404 quando o horário não existe ou é de outro Professor e com 400
    /// quando o valor está fora do enum.
    /// </summary>
    [HttpPatch("{horarioId:guid}")]
    public async Task<IActionResult> AlterarPolitica(Guid professorId, Guid horarioId, [FromBody] AlterarPoliticaHorarioRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        try
        {
            var tipoAnterior = await _horarioService.BuscarTipoMarcacaoAsync(professorId, horarioId, cancellationToken);
            var horario = await _horarioService.AlterarPoliticaAsync(professorId, horarioId, (TipoMarcacao)request.TipoMarcacao, cancellationToken);
            LogHorarioTipoMarcacaoAlterado(trackId, horario, tipoAnterior);
            return Ok(ParaResponse(horario));
        }
        catch (HorarioNaoEncontradoException)
        {
            return NotFound();
        }
        catch (HorarioRejeitadoException ex)
        {
            return BadRequest(new HorarioErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Altera o prazo de cancelamento (<c>prazoCancelamentoMinutos</c>) de um
    /// horário já cadastrado (issue #187) — mesmo padrão de
    /// <see cref="AlterarPolitica"/>.
    /// </summary>
    [HttpPatch("{horarioId:guid}/prazo-cancelamento")]
    public async Task<IActionResult> AlterarPrazoCancelamento(
        Guid professorId, Guid horarioId, [FromBody] AlterarPrazoCancelamentoHorarioRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        try
        {
            var prazoAnterior = await _horarioService.BuscarPrazoCancelamentoAsync(professorId, horarioId, cancellationToken);
            var horario = await _horarioService.AlterarPrazoCancelamentoAsync(
                professorId, horarioId, request.PrazoCancelamentoMinutos, cancellationToken);
            LogHorarioPrazoCancelamentoAlterado(trackId, horario, prazoAnterior);
            return Ok(ParaResponse(horario));
        }
        catch (HorarioNaoEncontradoException)
        {
            return NotFound();
        }
        catch (HorarioRejeitadoException ex)
        {
            return BadRequest(new HorarioErrorResponse(ex.Message));
        }
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
        return new HorarioResponse(
            horario.Id, (int)horario.DiaSemana, horario.HoraInicio, horario.DuracaoMinutos, (int)horario.TipoMarcacao,
            horario.LimiteAlunos, horario.PrazoCancelamentoMinutos);
    }

    private void LogHorarioCriado(string trackId, Horario horario)
    {
        _logger.LogInformation(
            "HorarioCriado {TrackId} {ProfessorId} {HorarioId}",
            trackId, horario.ProfessorId, horario.Id);
    }

    /// <summary>
    /// Cobre a definição inicial do limite (não há endpoint de edição de
    /// horário ainda — <c>LimiteAnterior</c> é sempre <c>null</c> nesta
    /// issue). Ver docs/specs/17-limite-alunos-horario/implementation.md#decisão-documentada-limitealunosalterado-também-cobre-a-definição-inicial.
    /// </summary>
    private void LogLimiteAlunosAlterado(string trackId, Horario horario)
    {
        _logger.LogInformation(
            "LimiteAlunosAlterado {TrackId} {ProfessorId} {HorarioId} {LimiteAnterior} {LimiteNovo}",
            trackId, horario.ProfessorId, horario.Id, null, horario.LimiteAlunos);
    }

    private void LogHorarioTipoMarcacaoAlterado(string trackId, Horario horario, TipoMarcacao tipoAnterior)
    {
        _logger.LogInformation(
            "HorarioTipoMarcacaoAlterado {TrackId} {ProfessorId} {HorarioId} {TipoMarcacaoAnterior} {TipoMarcacaoNovo}",
            trackId, horario.ProfessorId, horario.Id, (int)tipoAnterior, (int)horario.TipoMarcacao);
    }

    private void LogHorarioPrazoCancelamentoAlterado(string trackId, Horario horario, int prazoAnterior)
    {
        _logger.LogInformation(
            "HorarioPrazoCancelamentoAlterado {TrackId} {ProfessorId} {HorarioId} {PrazoAnterior} {PrazoNovo}",
            trackId, horario.ProfessorId, horario.Id, prazoAnterior, horario.PrazoCancelamentoMinutos);
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

public sealed record CriarHorarioRequest(
    int DiaSemana, TimeOnly HoraInicio, int DuracaoMinutos, int TipoMarcacao, int? LimiteAlunos = null, int? PrazoCancelamentoMinutos = null);

public sealed record AlterarPoliticaHorarioRequest(int TipoMarcacao);

public sealed record AlterarPrazoCancelamentoHorarioRequest(int PrazoCancelamentoMinutos);

public sealed record HorarioResponse(
    Guid Id, int DiaSemana, TimeOnly HoraInicio, int DuracaoMinutos, int TipoMarcacao, int LimiteAlunos, int PrazoCancelamentoMinutos);

public sealed record HorarioErrorResponse(string Mensagem);
