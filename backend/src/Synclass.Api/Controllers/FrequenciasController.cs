using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Aulas;
using Synclass.Domain.Frequencias;
using Synclass.Domain.Horarios;

namespace Synclass.Api.Controllers;

/// <summary>
/// Registro de frequência pelo Professor (issue #14) — presença/ausência em
/// lote dos Alunos alocados num horário/data. <c>[Authorize(Roles =
/// "Professor")]</c> contrasta com <see cref="AulasController"/> (Aluno);
/// <c>professorId</c> vem do token, não da rota — mesmo padrão de
/// <see cref="AlocacoesHorarioController"/> (issue #23): é o Professor
/// agindo sobre os próprios horários, não um Aluno navegando outro
/// Professor (esse é o caso que mantém <c>professorId</c> na rota, ver
/// <see cref="AulasController"/>/<see cref="MarcacoesHorarioController"/>).
/// </summary>
[ApiController]
[Route("professores/horarios")]
[Authorize(Roles = "Professor")]
public sealed class FrequenciasController : ControllerBase
{
    private readonly FrequenciaService _frequenciaService;
    private readonly ILogger<FrequenciasController> _logger;

    public FrequenciasController(FrequenciaService frequenciaService, ILogger<FrequenciasController> logger)
    {
        _frequenciaService = frequenciaService;
        _logger = logger;
    }

    [HttpPost("{horarioId:guid}/aulas/{data}/frequencias")]
    public async Task<IActionResult> Registrar(
        Guid horarioId, DateOnly data, [FromBody] RegistrarFrequenciaRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        var professorId = User.GetUsuarioId();
        try
        {
            var statusPorMatricula = request.Registros.ToDictionary(
                item => item.MatriculaId,
                item => item.Presente ? StatusFrequencia.Presente : StatusFrequencia.Ausente);
            var registros = await _frequenciaService.RegistrarAsync(
                professorId, horarioId, data, statusPorMatricula, cancellationToken);

            LogFrequenciaRegistrada(trackId, professorId, horarioId, registros);
            foreach (var registro in registros)
            {
                LogSeDivergente(trackId, registro);
            }

            return Ok(registros.Select(ParaResponse));
        }
        catch (HorarioNaoEncontradoException)
        {
            return NotFound();
        }
        catch (AulaRejeitadaException ex)
        {
            return BadRequest(new FrequenciaErrorResponse(ex.Message));
        }
        catch (ArgumentException)
        {
            return BadRequest(new FrequenciaErrorResponse("Cada matriculaId deve aparecer no máximo uma vez em registros."));
        }
    }

    private static RegistroFrequenciaResponse ParaResponse(RegistroFrequencia registro)
    {
        return new RegistroFrequenciaResponse(
            registro.MatriculaId, registro.StatusProfessor == StatusFrequencia.Presente, registro.ConfirmadoPeloAluno);
    }

    /// <summary>
    /// Evento <c>FrequenciaRegistrada</c> (Information) — contagem de
    /// presentes/ausentes do lote (ver
    /// docs/specs/14-registro-frequencia/task.md#logs).
    /// </summary>
    private void LogFrequenciaRegistrada(
        string trackId, Guid professorId, Guid horarioId, IReadOnlyCollection<RegistroFrequencia> registros)
    {
        var quantidadePresentes = registros.Count(r => r.StatusProfessor == StatusFrequencia.Presente);
        var quantidadeAusentes = registros.Count - quantidadePresentes;
        var aulaId = registros.Select(r => r.AulaId).FirstOrDefault();

        _logger.LogInformation(
            "FrequenciaRegistrada {TrackId} {ProfessorId} {HorarioId} {AulaId} {QuantidadePresentes} {QuantidadeAusentes}",
            trackId, professorId, horarioId, aulaId, quantidadePresentes, quantidadeAusentes);
    }

    /// <summary>
    /// Evento <c>FrequenciaDivergente</c> (Warning) — o registro do
    /// Professor contradiz a confirmação prévia do Aluno (issue #15).
    /// <c>ConfirmadoPeloAluno == null</c> (Aluno nunca confirmou) nunca é
    /// divergente.
    /// </summary>
    private void LogSeDivergente(string trackId, RegistroFrequencia registro)
    {
        if (registro.ConfirmadoPeloAluno is not { } confirmadoPeloAluno)
        {
            return;
        }

        var presenteSegundoOProfessor = registro.StatusProfessor == StatusFrequencia.Presente;
        if (presenteSegundoOProfessor == confirmadoPeloAluno)
        {
            return;
        }

        _logger.LogWarning(
            "FrequenciaDivergente {TrackId} {AulaId} {MatriculaId}",
            trackId, registro.AulaId, registro.MatriculaId);
    }
}

public sealed record RegistrarFrequenciaRequest(IReadOnlyList<RegistroFrequenciaItemRequest> Registros);

public sealed record RegistroFrequenciaItemRequest(Guid MatriculaId, bool Presente);

public sealed record RegistroFrequenciaResponse(Guid MatriculaId, bool Presente, bool? ConfirmadoPeloAluno);

public sealed record FrequenciaErrorResponse(string Mensagem);
