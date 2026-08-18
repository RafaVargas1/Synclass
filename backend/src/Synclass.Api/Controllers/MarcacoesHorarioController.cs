using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Alocacoes;
using Synclass.Domain.Horarios;

namespace Synclass.Api.Controllers;

/// <summary>
/// Marcação livre de Aluno em horário vago (issue #9) — o Aluno consulta os
/// horários que pode marcar agora e confirma a marcação, sem aprovação do
/// Professor. Controller separado de <see cref="AlocacoesHorarioController"/>
/// (issue #8) — rotas e semântica de autorização diferentes (ali é o
/// Professor agindo sobre seus Alunos, aqui é o Aluno agindo sobre si
/// mesmo), mesmo padrão já usado entre <c>HorariosController</c> e este.
/// <c>professorId</c> continua vindo da rota (issue #23) — diferente de
/// <see cref="AlunosProvisoriosController"/>/<see cref="AlocacoesHorarioController"/>,
/// aqui ele identifica o Professor sendo navegado pelo Aluno, não a
/// identidade de quem chama; não há "sessão de qual Professor" para ler.
/// O que de fato precisava de sessão era <c>matriculaId</c> — removido dos
/// parâmetros do cliente e resolvido a partir do Aluno autenticado
/// (<see cref="AlocacaoHorarioService.ResolverMatriculaDoAlunoAsync"/>), já
/// que antes qualquer Aluno autenticado podia informar a <c>matriculaId</c>
/// de outro Aluno sem checagem de posse.
/// </summary>
[ApiController]
[Route("professores/{professorId:guid}/horarios")]
[Authorize(Roles = "Aluno")]
public sealed class MarcacoesHorarioController : ControllerBase
{
    private readonly AlocacaoHorarioService _alocacaoHorarioService;
    private readonly ILogger<MarcacoesHorarioController> _logger;

    public MarcacoesHorarioController(AlocacaoHorarioService alocacaoHorarioService, ILogger<MarcacoesHorarioController> logger)
    {
        _alocacaoHorarioService = alocacaoHorarioService;
        _logger = logger;
    }

    [HttpGet("vagos")]
    public async Task<IActionResult> ListarVagos(Guid professorId, CancellationToken cancellationToken)
    {
        try
        {
            var matriculaId = await ResolverMatriculaAsync(professorId, cancellationToken);
            var vagos = await _alocacaoHorarioService.ListarVagosAsync(professorId, matriculaId, cancellationToken);
            return Ok(vagos.Select(ParaResponse));
        }
        catch (AlunoNaoVinculadoAoProfessorException)
        {
            return NotFound();
        }
        catch (AlocacaoRejeitadaException ex)
        {
            return BadRequest(new AlocacaoHorarioErrorResponse(ex.Message));
        }
    }

    [HttpPost("{horarioId:guid}/marcacoes")]
    public async Task<IActionResult> Marcar(Guid professorId, Guid horarioId, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        try
        {
            var matriculaId = await ResolverMatriculaAsync(professorId, cancellationToken);
            var alocacao = await _alocacaoHorarioService.MarcarAsync(
                professorId, horarioId, matriculaId, cancellationToken);
            LogAlunoAlocado(trackId, alocacao);
            return Ok(ParaResponse(alocacao));
        }
        catch (AlunoNaoVinculadoAoProfessorException)
        {
            return NotFound();
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

    private Task<Guid> ResolverMatriculaAsync(Guid professorId, CancellationToken cancellationToken)
    {
        var alunoUsuarioId = User.GetUsuarioId();
        return _alocacaoHorarioService.ResolverMatriculaDoAlunoAsync(professorId, alunoUsuarioId, cancellationToken);
    }

    private static AlocacaoHorarioResponse ParaResponse(AlocacaoHorario alocacao)
    {
        return new AlocacaoHorarioResponse(alocacao.Id, alocacao.HorarioId, alocacao.MatriculaId, alocacao.CreatedAt);
    }

    private static HorarioVagoResponse ParaResponse(HorarioVago horarioVago)
    {
        var horario = horarioVago.Horario;
        return new HorarioVagoResponse(
            horario.Id, (int)horario.DiaSemana, horario.HoraInicio, horario.DuracaoMinutos, horarioVago.VagasRestantes);
    }

    private void LogAlunoAlocado(string trackId, AlocacaoHorario alocacao)
    {
        _logger.LogInformation(
            "AlunoAlocadoEmHorario {TrackId} {HorarioId} {MatriculaId} {AlocacaoId} {OrigemAlocacao}",
            trackId, alocacao.HorarioId, alocacao.MatriculaId, alocacao.Id, alocacao.OrigemAlocacao);
    }

    private void LogAlocacaoRejeitada(string trackId, Guid professorId, Guid horarioId, AlocacaoRejeitadaException ex)
    {
        _logger.LogWarning(
            "AlocacaoRejeitada {TrackId} {ProfessorId} {HorarioId} {Motivo}",
            trackId, professorId, horarioId, ex.GetType().Name);
    }
}

public sealed record HorarioVagoResponse(Guid Id, int DiaSemana, TimeOnly HoraInicio, int DuracaoMinutos, int VagasRestantes);
