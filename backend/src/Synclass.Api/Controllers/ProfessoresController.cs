using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Usuarios;

namespace Synclass.Api.Controllers;

[ApiController]
[Route("professores")]
public sealed class ProfessoresController : ControllerBase
{
    private readonly CadastroUsuarioService _cadastroUsuario;
    private readonly ILogger<ProfessoresController> _logger;

    public ProfessoresController(CadastroUsuarioService cadastroUsuario, ILogger<ProfessoresController> logger)
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
            var resultado = await _cadastroUsuario.CadastrarAsync(PapelUsuario.Professor, request.Nome, request.Contato, cancellationToken);
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
    /// Alimenta o campo Nome readonly do formulário de cadastro (issue #27)
    /// quando o contato já pertence a uma identidade existente — sem isso, o
    /// Professor preencheria um nome que a Api descarta silenciosamente ao
    /// reaproveitar a identidade (RN da issue #20). Público (roda antes de
    /// existir sessão) e nunca rejeita: um contato ainda incompleto enquanto
    /// o usuário digita deve devolver "não existe" silenciosamente, não um
    /// 400 a cada tecla.
    /// </summary>
    [HttpGet("verificar-contato")]
    public async Task<IActionResult> VerificarContato([FromQuery] string contato, CancellationToken cancellationToken)
    {
        var resultado = await _cadastroUsuario.VerificarContatoAsync(contato ?? string.Empty, cancellationToken);
        return Ok(new VerificarContatoResponse(resultado.IdentidadeExistente, resultado.Nome));
    }
}
