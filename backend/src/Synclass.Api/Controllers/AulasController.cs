using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Alocacoes;
using Synclass.Domain.Aulas;
using Synclass.Domain.Common;
using Synclass.Domain.Frequencias;
using Synclass.Domain.Horarios;

namespace Synclass.Api.Controllers;

/// <summary>
/// Cancelamento de aula pelo Aluno, com antecedência mínima configurável
/// pelo Professor (issue #10), e confirmação de presença pelo Aluno (issue
/// #15) — o Aluno consulta as próximas aulas em que está alocado e cancela
/// ou confirma presença numa delas. Mesmo padrão de rota/autorização de
/// <see cref="MarcacoesHorarioController"/> (issue #9): <c>professorId</c>
/// na rota identifica o Professor sendo navegado pelo Aluno, mas
/// <c>matriculaId</c> nunca vem de query/body do cliente — é resolvida do
/// Aluno autenticado via
/// <see cref="AlocacaoHorarioService.ResolverMatriculaDoAlunoAsync"/> (issue
/// #23), fechando a mesma lacuna que existiria aqui se o Aluno pudesse
/// informar a matrícula de outro Aluno.
/// </summary>
[ApiController]
[Route("professores/{professorId:guid}/horarios")]
[Authorize(Roles = "Aluno")]
public sealed class AulasController : ControllerBase
{
    private readonly AulaService _aulaService;
    private readonly AlocacaoHorarioService _alocacaoHorarioService;
    private readonly FrequenciaService _frequenciaService;
    private readonly IHorarioRepository _horarios;
    private readonly IClock _clock;
    private readonly ILogger<AulasController> _logger;

    public AulasController(
        AulaService aulaService,
        AlocacaoHorarioService alocacaoHorarioService,
        FrequenciaService frequenciaService,
        IHorarioRepository horarios,
        IClock clock,
        ILogger<AulasController> logger)
    {
        _aulaService = aulaService;
        _alocacaoHorarioService = alocacaoHorarioService;
        _frequenciaService = frequenciaService;
        _horarios = horarios;
        _clock = clock;
        _logger = logger;
    }

    [HttpGet("proximas-aulas")]
    public async Task<IActionResult> ListarProximasAulas(Guid professorId, CancellationToken cancellationToken)
    {
        try
        {
            var matriculaId = await ResolverMatriculaAsync(professorId, cancellationToken);
            var proximas = await _aulaService.ListarProximasAsync(professorId, matriculaId, cancellationToken);
            return Ok(proximas.Select(ParaResponse));
        }
        catch (AlunoNaoVinculadoAoProfessorException)
        {
            return NotFound();
        }
        catch (MatriculaNaoVinculadaAoProfessorException ex)
        {
            return BadRequest(new AulaErrorResponse(ex.Message));
        }
    }

    [HttpPost("{horarioId:guid}/aulas/{data}/cancelamentos")]
    public async Task<IActionResult> Cancelar(
        Guid professorId, Guid horarioId, DateOnly data, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        try
        {
            var matriculaId = await ResolverMatriculaAsync(professorId, cancellationToken);
            var cancelamento = await _aulaService.CancelarAsync(
                professorId, horarioId, data, matriculaId, cancellationToken);
            await LogAulaCanceladaAsync(trackId, horarioId, data, cancelamento, cancellationToken);
            return Ok(ParaResponse(cancelamento));
        }
        catch (AlunoNaoVinculadoAoProfessorException)
        {
            return NotFound();
        }
        catch (HorarioNaoEncontradoException)
        {
            return NotFound();
        }
        catch (PrazoCancelamentoExpiradoException ex)
        {
            LogCancelamentoRejeitadoPorPrazo(trackId, ex);
            return BadRequest(new AulaErrorResponse(ex.Message));
        }
        catch (AulaRejeitadaException ex)
        {
            return BadRequest(new AulaErrorResponse(ex.Message));
        }
    }

    [HttpPost("{horarioId:guid}/aulas/{data}/confirmacao-presenca")]
    public async Task<IActionResult> ConfirmarPresenca(
        Guid professorId, Guid horarioId, DateOnly data, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        try
        {
            var alunoUsuarioId = User.GetUsuarioId();
            var registro = await _frequenciaService.ConfirmarPresencaAsync(
                professorId, horarioId, data, alunoUsuarioId, cancellationToken);
            LogPresencaConfirmadaPeloAluno(trackId, registro);
            return Ok(ParaResponse(registro));
        }
        catch (AlunoNaoVinculadoAoProfessorException)
        {
            return NotFound();
        }
        catch (HorarioNaoEncontradoException)
        {
            return NotFound();
        }
        catch (AulaRejeitadaException ex)
        {
            return BadRequest(new AulaErrorResponse(ex.Message));
        }
    }

    private Task<Guid> ResolverMatriculaAsync(Guid professorId, CancellationToken cancellationToken)
    {
        var alunoUsuarioId = User.GetUsuarioId();
        return _alocacaoHorarioService.ResolverMatriculaDoAlunoAsync(professorId, alunoUsuarioId, cancellationToken);
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

    private static ConfirmacaoPresencaResponse ParaResponse(RegistroFrequencia registro)
    {
        return new ConfirmacaoPresencaResponse(registro.AulaId, registro.MatriculaId, registro.ConfirmadoPeloAluno == true);
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
    private void LogCancelamentoRejeitadoPorPrazo(string trackId, PrazoCancelamentoExpiradoException ex)
    {
        var antecedenciaTentadaMinutos = (ex.Limite.AddMinutes(ex.PrazoCancelamentoMinutos) - _clock.UtcNow).TotalMinutes;

        _logger.LogWarning(
            "CancelamentoRejeitadoPorPrazo {TrackId} {AulaId} {PrazoCancelamentoMinutos} {AntecedenciaTentadaMinutos}",
            trackId, ex.AulaId, ex.PrazoCancelamentoMinutos, antecedenciaTentadaMinutos);
    }

    /// <summary>
    /// Evento <c>PresencaConfirmadaPeloAluno</c> (Information) — issue #15,
    /// ver docs/specs/15-aluno-confirma-presenca/task.md#logs.
    /// </summary>
    private void LogPresencaConfirmadaPeloAluno(string trackId, RegistroFrequencia registro)
    {
        _logger.LogInformation(
            "PresencaConfirmadaPeloAluno {TrackId} {MatriculaId} {AulaId}",
            trackId, registro.MatriculaId, registro.AulaId);
    }
}

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

public sealed record ConfirmacaoPresencaResponse(Guid AulaId, Guid MatriculaId, bool ConfirmadoPeloAluno);

public sealed record AulaErrorResponse(string Mensagem);
