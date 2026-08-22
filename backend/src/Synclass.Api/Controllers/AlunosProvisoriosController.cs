using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Usuarios;

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
    private readonly IUsuarioRepository _usuarios;
    private readonly ILogger<AlunosProvisoriosController> _logger;

    public AlunosProvisoriosController(
        CadastroAlunoProvisorioService cadastroAlunoProvisorio,
        IMatriculaRepository matriculas,
        IUsuarioRepository usuarios,
        ILogger<AlunosProvisoriosController> logger)
    {
        _cadastroAlunoProvisorio = cadastroAlunoProvisorio;
        _matriculas = matriculas;
        _usuarios = usuarios;
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
        var respostas = new List<AlunoProvisorioResponse>();
        foreach (var matricula in matriculas)
        {
            respostas.Add(await ParaResponseAsync(matricula, cancellationToken));
        }

        return Ok(respostas);
    }

    /// <summary>
    /// Provisórias e plenas são listadas igualmente (issue #8 — a
    /// distinção não importa para o seletor de alocação). Matrículas plenas
    /// não têm <see cref="Matricula.NomeProvisorio"/>/<see cref="Matricula.IdentificadorProvisorio"/>
    /// (nasceram do aceite de convite, issue #2) — nesse caso o nome vem do
    /// <see cref="Usuario"/> vinculado (<see cref="Matricula.AlunoUsuarioId"/>),
    /// senão a linha aparecia com nome em branco em "Meus Alunos" mesmo com
    /// o vínculo criado corretamente (achado de bug reportado pelo usuário).
    /// N+1 em <see cref="IUsuarioRepository.BuscarPorIdAsync"/>: mesmo
    /// "melhor esforço aceitável na escala atual" já usado em
    /// <c>ConsultaCobrancaService.ResolverNomeProfessorAsync</c>.
    /// </summary>
    private async Task<AlunoProvisorioResponse> ParaResponseAsync(Matricula matricula, CancellationToken cancellationToken)
    {
        if (matricula.NomeProvisorio is not null)
        {
            return new AlunoProvisorioResponse(matricula.Id, matricula.NomeProvisorio, matricula.IdentificadorProvisorio ?? string.Empty);
        }

        var nome = string.Empty;
        if (matricula.AlunoUsuarioId is Guid alunoUsuarioId)
        {
            var usuario = await _usuarios.BuscarPorIdAsync(alunoUsuarioId, cancellationToken);
            nome = usuario?.Nome ?? string.Empty;
        }

        return new AlunoProvisorioResponse(matricula.Id, nome, matricula.IdentificadorProvisorio ?? string.Empty);
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
