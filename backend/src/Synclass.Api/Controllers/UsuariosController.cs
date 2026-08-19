using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Usuarios;

namespace Synclass.Api.Controllers;

/// <summary>
/// Perfil do usuário autenticado (issue #27): consulta e correção do
/// próprio nome — canal explícito para quem cadastrou o nome errado, já que
/// um segundo cadastro com o mesmo contato não sobrescreve o nome existente
/// (RN da issue #20). Sem restrição de <c>Roles</c>: o nome pertence ao
/// <c>Usuario</c>, não a um papel específico (Professor ou Aluno) — ambos
/// editam o próprio nome igualmente.
/// </summary>
[Authorize]
[ApiController]
[Route("usuarios")]
public sealed class UsuariosController : ControllerBase
{
    private readonly IUsuarioRepository _usuarios;
    private readonly AtualizacaoNomeUsuarioService _atualizacaoNome;
    private readonly ILogger<UsuariosController> _logger;

    public UsuariosController(
        IUsuarioRepository usuarios, AtualizacaoNomeUsuarioService atualizacaoNome, ILogger<UsuariosController> logger)
    {
        _usuarios = usuarios;
        _atualizacaoNome = atualizacaoNome;
        _logger = logger;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var usuarioId = User.GetUsuarioId();
        var usuario = await _usuarios.BuscarPorIdAsync(usuarioId, cancellationToken);
        if (usuario is null)
        {
            return NotFound(new UsuarioPerfilErrorResponse($"Usuario não encontrado: {usuarioId}."));
        }

        return Ok(new UsuarioPerfilResponse(usuario.Id, usuario.Nome));
    }

    [HttpPut("me/nome")]
    public async Task<IActionResult> AtualizarNome([FromBody] AtualizarNomeRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        var usuarioId = User.GetUsuarioId();

        try
        {
            var usuario = await _atualizacaoNome.AtualizarNomeAsync(usuarioId, request.Nome, cancellationToken);
            LogNomeAtualizado(trackId, usuarioId);
            return Ok(new UsuarioPerfilResponse(usuario.Id, usuario.Nome));
        }
        catch (NomeInvalidoException ex)
        {
            return RejeitarNomeInvalido(trackId, usuarioId, ex);
        }
        catch (UsuarioNaoEncontradoException ex)
        {
            return NotFound(new UsuarioPerfilErrorResponse(ex.Message));
        }
    }

    private void LogNomeAtualizado(string trackId, Guid usuarioId)
    {
        _logger.LogInformation("NomeAtualizado {TrackId} {UsuarioId}", trackId, usuarioId);
    }

    private IActionResult RejeitarNomeInvalido(string trackId, Guid usuarioId, NomeInvalidoException ex)
    {
        _logger.LogWarning(
            "AtualizacaoNomeRejeitada {TrackId} {UsuarioId} {Motivo}", trackId, usuarioId, ex.GetType().Name);
        return BadRequest(new UsuarioPerfilErrorResponse(ex.Message));
    }
}

public sealed record UsuarioPerfilResponse(Guid UsuarioId, string Nome);

public sealed record UsuarioPerfilErrorResponse(string Mensagem);

public sealed record AtualizarNomeRequest(string Nome);
