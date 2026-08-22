using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Matriculas;

namespace Synclass.Api.Controllers;

/// <summary>
/// Cadastro (issue #3) e listagem (issue #8, para o seletor de Aluno da
/// alocação a horário) de Aluno provisório — sem contato, e-mail, telefone
/// ou login, diferente do cadastro pleno (issue #1). <c>professorId</c> é
/// sempre a identidade de quem chama (um Professor só gerencia os próprios
/// Alunos) — lido do token via <see cref="ClaimsPrincipalExtensions.GetUsuarioId"/>,
/// não mais de parâmetro de rota (issue #23, débito documentado em
/// docs/specs/3-aluno-provisorio/implementation.md).
/// </summary>
[Authorize(Roles = "Professor")]
[ApiController]
[Route("professores/alunos-provisorios")]
public sealed class AlunosProvisoriosController : ControllerBase
{
    private readonly CadastroAlunoProvisorioService _cadastroAlunoProvisorio;
    private readonly IMatriculaRepository _matriculas;
    private readonly ILogger<AlunosProvisoriosController> _logger;

    public AlunosProvisoriosController(
        CadastroAlunoProvisorioService cadastroAlunoProvisorio,
        IMatriculaRepository matriculas,
        ILogger<AlunosProvisoriosController> logger)
    {
        _cadastroAlunoProvisorio = cadastroAlunoProvisorio;
        _matriculas = matriculas;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Cadastrar(
        [FromBody] CadastroAlunoProvisorioRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        var professorId = User.GetUsuarioId();

        try
        {
            var matricula = await _cadastroAlunoProvisorio.CadastrarAsync(
                professorId, request.Nome, cancellationToken);
            LogCadastroSucesso(trackId, matricula);
            return Ok(new CadastroAlunoProvisorioResponse(matricula.Id, matricula.NomeProvisorio!, matricula.IdentificadorProvisorio!));
        }
        catch (ProfessorNaoEncontradoException ex)
        {
            return RejeitarProfessorNaoEncontrado(trackId, professorId, ex);
        }
        catch (MatriculaRejeitadaException ex)
        {
            return RejeitarCadastro(trackId, professorId, ex);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        var professorId = User.GetUsuarioId();
        var matriculas = await _matriculas.ListarPorProfessorAsync(professorId, cancellationToken);
        return Ok(matriculas.Select(ParaResponse));
    }

    /// <summary>
    /// Provisórias e plenas são listadas igualmente (issue #8 — a
    /// distinção não importa para o seletor de alocação). Matrículas plenas
    /// não têm <see cref="Matricula.NomeProvisorio"/>/<see cref="Matricula.IdentificadorProvisorio"/>
    /// (nasceram do aceite de convite, issue #2) — <c>string.Empty</c> é o
    /// melhor-esforço até um endpoint dedicado buscar o nome do Usuario
    /// vinculado, fora do escopo desta issue.
    /// </summary>
    private static AlunoProvisorioResponse ParaResponse(Matricula matricula)
    {
        return new AlunoProvisorioResponse(matricula.Id, matricula.NomeProvisorio ?? string.Empty, matricula.IdentificadorProvisorio ?? string.Empty);
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

    /// <summary>
    /// Mapeia <see cref="ProfessorNaoEncontradoException"/> para 404, distinto
    /// do 400 usado pelas demais <see cref="MatriculaRejeitadaException"/> —
    /// professorId inexistente não é um cadastro corrigível reenviando os
    /// mesmos dados, ao contrário de nome/identificador inválidos (achado de
    /// dev-review no PR #22).
    /// </summary>
    private IActionResult RejeitarProfessorNaoEncontrado(string trackId, Guid professorId, ProfessorNaoEncontradoException ex)
    {
        _logger.LogWarning(
            "CadastroAlunoProvisorioRejeitado {TrackId} {ProfessorId} {Motivo}",
            trackId, professorId, ex.GetType().Name);
        return NotFound(new CadastroAlunoProvisorioErrorResponse(ex.Message));
    }
}

public sealed record CadastroAlunoProvisorioRequest(string Nome);

public sealed record CadastroAlunoProvisorioResponse(Guid MatriculaId, string Nome, string Identificador);

public sealed record CadastroAlunoProvisorioErrorResponse(string Mensagem);

public sealed record AlunoProvisorioResponse(Guid MatriculaId, string Nome, string Identificador);
