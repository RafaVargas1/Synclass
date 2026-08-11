using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Matriculas;

namespace Synclass.Api.Controllers;

/// <summary>
/// Cadastro de Aluno provisório (issue #3) — sem contato, e-mail, telefone
/// ou login, diferente do cadastro pleno (issue #1). O <c>professorId</c> é
/// recebido na rota, não de uma sessão: ainda não há login (issue #18, em
/// paralelo) de onde derivar o Professor autenticado. Decisão documentada em
/// docs/specs/3-aluno-provisorio/implementation.md — trocar por sessão real
/// é o objeto da issue de acompanhamento aberta junto deste PR.
/// </summary>
[ApiController]
[Route("professores/{professorId:guid}/alunos-provisorios")]
public sealed class AlunosProvisoriosController : ControllerBase
{
    private readonly CadastroAlunoProvisorioService _cadastroAlunoProvisorio;
    private readonly ILogger<AlunosProvisoriosController> _logger;

    public AlunosProvisoriosController(
        CadastroAlunoProvisorioService cadastroAlunoProvisorio, ILogger<AlunosProvisoriosController> logger)
    {
        _cadastroAlunoProvisorio = cadastroAlunoProvisorio;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Cadastrar(
        Guid professorId, [FromBody] CadastroAlunoProvisorioRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();

        try
        {
            var matricula = await _cadastroAlunoProvisorio.CadastrarAsync(
                professorId, request.Nome, request.Identificador, cancellationToken);
            LogCadastroSucesso(trackId, matricula);
            return Ok(new CadastroAlunoProvisorioResponse(matricula.Id, matricula.NomeProvisorio!, matricula.IdentificadorProvisorio!));
        }
        catch (MatriculaRejeitadaException ex)
        {
            return RejeitarCadastro(trackId, professorId, ex);
        }
    }

    private void LogCadastroSucesso(string trackId, Matricula matricula)
    {
        _logger.LogInformation(
            "AlunoProvisorioCadastrado {TrackId} {ProfessorId} {MatriculaId}",
            trackId, matricula.ProfessorId, matricula.Id);
    }

    private IActionResult RejeitarCadastro(string trackId, Guid professorId, MatriculaRejeitadaException ex)
    {
        _logger.LogWarning(
            "CadastroAlunoProvisorioRejeitado {TrackId} {ProfessorId} {Motivo}",
            trackId, professorId, ex.GetType().Name);
        return BadRequest(new CadastroAlunoProvisorioErrorResponse(ex.Message));
    }
}

public sealed record CadastroAlunoProvisorioRequest(string Nome, string Identificador);

public sealed record CadastroAlunoProvisorioResponse(Guid MatriculaId, string Nome, string Identificador);

public sealed record CadastroAlunoProvisorioErrorResponse(string Mensagem);
