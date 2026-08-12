using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.Usuarios;

namespace Synclass.Api.Controllers;

/// <summary>
/// Login por código de uso único (issue #18): solicitar código e confirmar
/// para obter a sessão (token JWT stateless).
/// </summary>
[ApiController]
[Route("auth")]
public sealed class AutenticacaoController : ControllerBase
{
    private readonly LoginService _login;
    private readonly ILogger<AutenticacaoController> _logger;

    public AutenticacaoController(LoginService login, ILogger<AutenticacaoController> logger)
    {
        _login = login;
        _logger = logger;
    }

    [HttpPost("codigo")]
    public async Task<IActionResult> SolicitarCodigo([FromBody] SolicitarCodigoRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();

        try
        {
            await _login.SolicitarCodigoAsync(request.Contato, cancellationToken);
            LogCodigoSolicitado(trackId, request.Contato);
            return Ok(new SolicitarCodigoResponse(true));
        }
        catch (Exception ex) when (ex is LoginRejeitadoException or ContatoInvalidoException)
        {
            return Rejeitar(trackId, request.Contato, ex);
        }
    }

    [HttpPost("confirmacao")]
    public async Task<IActionResult> ConfirmarCodigo([FromBody] ConfirmarCodigoRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();

        try
        {
            var resultado = await _login.ConfirmarCodigoAsync(request.Contato, request.Codigo, cancellationToken);
            LogLoginConfirmado(trackId, resultado.Usuario);
            return Ok(ParaResponse(resultado));
        }
        catch (Exception ex) when (ex is LoginRejeitadoException or ContatoInvalidoException)
        {
            return Rejeitar(trackId, request.Contato, ex);
        }
    }

    private static ConfirmarCodigoResponse ParaResponse(ResultadoLogin resultado)
    {
        var papeis = resultado.Usuario.Papeis.Select(p => p.Papel.ToString()).ToArray();
        return new ConfirmarCodigoResponse(resultado.Token, resultado.Usuario.Id, resultado.Usuario.Nome, papeis);
    }

    private void LogCodigoSolicitado(string trackId, string contatoBruto)
    {
        var contatoMascarado = MascaradorDeContato.Mascarar(contatoBruto);
        _logger.LogInformation("CodigoOtpSolicitado {TrackId} {ContatoMascarado}", trackId, contatoMascarado);
    }

    private void LogLoginConfirmado(string trackId, Usuario usuario)
    {
        var papeis = string.Join(",", usuario.Papeis.Select(p => p.Papel));
        _logger.LogInformation("LoginConfirmado {TrackId} {UsuarioId} {Papeis}", trackId, usuario.Id, papeis);
    }

    private IActionResult Rejeitar(string trackId, string contatoBruto, Exception ex)
    {
        var contatoMascarado = MascaradorDeContato.Mascarar(contatoBruto ?? string.Empty);
        _logger.LogWarning("LoginRejeitado {TrackId} {Motivo} {ContatoMascarado}", trackId, ex.GetType().Name, contatoMascarado);
        return BadRequest(new AutenticacaoErrorResponse(ex.Message));
    }
}

public sealed record SolicitarCodigoRequest(string Contato);

public sealed record SolicitarCodigoResponse(bool Enviado);

public sealed record ConfirmarCodigoRequest(string Contato, string Codigo);

public sealed record ConfirmarCodigoResponse(string Token, Guid UsuarioId, string Nome, string[] Papeis);

public sealed record AutenticacaoErrorResponse(string Mensagem);
