using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.CodigosEntradaTurma;
using Synclass.Domain.Matriculas;

namespace Synclass.Api.Controllers;

/// <summary>
/// Código de entrada de turma: o Professor gera um código curto, válido por
/// poucos minutos, que qualquer Aluno autenticado pode usar pra entrar na
/// turma — sem vínculo a um contato específico e sem uso único, diferente
/// de <see cref="ConvitesController"/>.
/// </summary>
[ApiController]
public sealed class CodigosEntradaTurmaController : ControllerBase
{
    private readonly CodigoEntradaTurmaService _codigosEntrada;
    private readonly ILogger<CodigosEntradaTurmaController> _logger;

    public CodigosEntradaTurmaController(CodigoEntradaTurmaService codigosEntrada, ILogger<CodigosEntradaTurmaController> logger)
    {
        _codigosEntrada = codigosEntrada;
        _logger = logger;
    }

    [Authorize(Roles = "Professor")]
    [HttpPost("professores/{professorId:guid}/codigos-entrada")]
    public async Task<IActionResult> Gerar(Guid professorId, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();

        try
        {
            var codigoEntrada = await _codigosEntrada.GerarAsync(professorId, cancellationToken);
            _logger.LogInformation(
                "CodigoEntradaGerado {TrackId} {ProfessorId} {ExpiraEm}", trackId, professorId, codigoEntrada.ExpiraEm);
            return Ok(new CodigoEntradaResponse(codigoEntrada.Codigo, codigoEntrada.ExpiraEm));
        }
        catch (ProfessorNaoEncontradoException ex)
        {
            _logger.LogWarning("CodigoEntradaRejeitado {TrackId} {ProfessorId} {Motivo}", trackId, professorId, ex.GetType().Name);
            return NotFound(new CodigoEntradaErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Aceita o código em nome do Aluno autenticado (resolvido do token, não
    /// do corpo da requisição) — diferente do aceite de <c>Convite</c>, que é
    /// anônimo e recebe nome/contato no corpo.
    /// </summary>
    [Authorize(Roles = "Aluno")]
    [HttpPost("alunos/codigos-entrada/{codigo}/aceite")]
    public async Task<IActionResult> Aceitar(string codigo, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        var alunoUsuarioId = User.GetUsuarioId();

        try
        {
            var resultado = await _codigosEntrada.AceitarAsync(codigo, alunoUsuarioId, cancellationToken);
            _logger.LogInformation(
                "CodigoEntradaAceito {TrackId} {ProfessorId} {AlunoUsuarioId} {VinculoCriado}",
                trackId, resultado.ProfessorId, alunoUsuarioId, resultado.VinculoCriado);
            return Ok(new AceiteCodigoEntradaResponse(resultado.ProfessorId, resultado.ProfessorNome));
        }
        catch (CodigoEntradaInvalidoException ex)
        {
            _logger.LogWarning("CodigoEntradaRejeitado {TrackId} {AlunoUsuarioId} {Motivo}", trackId, alunoUsuarioId, ex.GetType().Name);
            return BadRequest(new CodigoEntradaErrorResponse(ex.Message));
        }
    }
}

public sealed record CodigoEntradaResponse(string Codigo, DateTimeOffset ExpiraEm);

public sealed record AceiteCodigoEntradaResponse(Guid ProfessorId, string ProfessorNome);

public sealed record CodigoEntradaErrorResponse(string Mensagem);
