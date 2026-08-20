using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Usuarios;

namespace Synclass.Api.Controllers;

/// <summary>
/// Cadastro independente de Aluno (issue #61) — espelha
/// <see cref="ProfessoresController"/> trocando o papel atribuído. O Aluno
/// nasce sem nenhum vínculo com um Professor; o vínculo é criado depois, no
/// fluxo de código de convite (próxima Task do épico #60).
/// </summary>
[ApiController]
[Route("alunos")]
public sealed class AlunosController : ControllerBase
{
    private readonly CadastroUsuarioService _cadastroUsuario;
    private readonly ILogger<AlunosController> _logger;

    public AlunosController(CadastroUsuarioService cadastroUsuario, ILogger<AlunosController> logger)
    {
        _cadastroUsuario = cadastroUsuario;
        _logger = logger;
    }

    [HttpPost("cadastro")]
    public async Task<IActionResult> Cadastrar([FromBody] CadastroUsuarioRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();

        try
        {
            var resultado = await _cadastroUsuario.CadastrarAsync(PapelUsuario.Aluno, request.Nome, request.Contato, cancellationToken);
            CadastroUsuarioLogging.LogCadastroSucesso(_logger, trackId, resultado);
            return Ok(new CadastroUsuarioResponse(resultado.Usuario.Id, resultado.Usuario.Nome));
        }
        catch (CadastroRejeitadoException ex)
        {
            CadastroUsuarioLogging.LogCadastroRejeitado(_logger, trackId, request.Contato, ex);
            return BadRequest(new CadastroUsuarioErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Mesmo propósito de <see cref="ProfessoresController.VerificarContato"/>
    /// (issue #27), reaproveitado pelo formulário de cadastro de Aluno.
    /// </summary>
    [HttpGet("verificar-contato")]
    public async Task<IActionResult> VerificarContato([FromQuery] string contato, CancellationToken cancellationToken)
    {
        var resultado = await _cadastroUsuario.VerificarContatoAsync(contato ?? string.Empty, cancellationToken);
        return Ok(new VerificarContatoResponse(resultado.IdentidadeExistente, resultado.Nome));
    }
}
