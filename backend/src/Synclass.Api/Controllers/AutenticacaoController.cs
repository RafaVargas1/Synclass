using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.Usuarios;

namespace Synclass.Api.Controllers;

/// <summary>
/// Login por código de uso único (issue #18): solicitar código e confirmar
/// para obter a sessão (token JWT stateless). Também expõe o login via
/// idToken do Google (issue #65) e da Apple (issue #212), fluxos paralelos
/// ao OTP.
/// </summary>
[ApiController]
[Route("auth")]
public sealed class AutenticacaoController : ControllerBase
{
    private readonly LoginService _login;
    private readonly LoginComGoogleService _loginGoogle;
    private readonly LoginComAppleService _loginApple;
    private readonly ILogger<AutenticacaoController> _logger;

    public AutenticacaoController(
        LoginService login,
        LoginComGoogleService loginGoogle,
        LoginComAppleService loginApple,
        ILogger<AutenticacaoController> logger)
    {
        _login = login;
        _loginGoogle = loginGoogle;
        _loginApple = loginApple;
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
        catch (OtpEnvioException ex)
        {
            // Falha de envio do código pelo provedor de WhatsApp (issue #193):
            // 502 sem detalhe técnico do provedor — só o motivo amigável, que
            // já vem sem Twilio/HttpRequest etc (ver OtpEnvioException.Motivo).
            return StatusCode(StatusCodes.Status502BadGateway, new AutenticacaoErrorResponse(ex.Motivo));
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

    [HttpPost("google")]
    public async Task<IActionResult> EntrarComGoogle([FromBody] LoginGoogleRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();

        try
        {
            var resultado = await _loginGoogle.AutenticarAsync(request.IdToken, cancellationToken);
            if (resultado.CadastroPendente)
            {
                LogCadastroPendente(trackId, resultado.EmailNormalizado);
                return Ok(new LoginGoogleResponse(
                    Token: null,
                    UsuarioId: null,
                    Nome: null,
                    Papeis: null,
                    CadastroPendente: true,
                    Email: resultado.EmailNormalizado));
            }

            LogLoginGoogleConfirmado(trackId, resultado.Login!.Usuario);
            return Ok(new LoginGoogleResponse(
                Token: resultado.Login.Token,
                UsuarioId: resultado.Login.Usuario.Id,
                Nome: resultado.Login.Usuario.Nome,
                Papeis: resultado.Login.Usuario.Papeis.Select(p => p.Papel.ToString()).ToArray(),
                CadastroPendente: false,
                Email: resultado.EmailNormalizado));
        }
        catch (LoginRejeitadoException ex)
        {
            return RejeitarLoginGoogle(trackId, ex);
        }
    }

    [HttpPost("apple")]
    public async Task<IActionResult> EntrarComApple([FromBody] LoginAppleRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();

        try
        {
            var resultado = await _loginApple.AutenticarAsync(request.IdToken, cancellationToken);
            if (resultado.CadastroPendente)
            {
                LogCadastroPendenteApple(trackId, resultado.EmailNormalizado);
                return Ok(new LoginAppleResponse(
                    Token: null,
                    UsuarioId: null,
                    Nome: null,
                    Papeis: null,
                    CadastroPendente: true,
                    Email: resultado.EmailNormalizado));
            }

            LogLoginAppleConfirmado(trackId, resultado.Login!.Usuario);
            return Ok(new LoginAppleResponse(
                Token: resultado.Login.Token,
                UsuarioId: resultado.Login.Usuario.Id,
                Nome: resultado.Login.Usuario.Nome,
                Papeis: resultado.Login.Usuario.Papeis.Select(p => p.Papel.ToString()).ToArray(),
                CadastroPendente: false,
                Email: resultado.EmailNormalizado));
        }
        catch (LoginRejeitadoException ex)
        {
            return RejeitarLoginApple(trackId, ex);
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

    private void LogLoginGoogleConfirmado(string trackId, Usuario usuario)
    {
        var papeis = string.Join(",", usuario.Papeis.Select(p => p.Papel));
        _logger.LogInformation("LoginGoogleConfirmado {TrackId} {UsuarioId} {Papeis}", trackId, usuario.Id, papeis);
    }

    private void LogLoginAppleConfirmado(string trackId, Usuario usuario)
    {
        var papeis = string.Join(",", usuario.Papeis.Select(p => p.Papel));
        _logger.LogInformation("LoginAppleConfirmado {TrackId} {UsuarioId} {Papeis}", trackId, usuario.Id, papeis);
    }

    private void LogCadastroPendente(string trackId, string emailNormalizado)
    {
        var emailMascarado = MascaradorDeContato.Mascarar(emailNormalizado);
        _logger.LogInformation("LoginGoogleCadastroPendente {TrackId} {EmailMascarado}", trackId, emailMascarado);
    }

    private void LogCadastroPendenteApple(string trackId, string emailNormalizado)
    {
        var emailMascarado = MascaradorDeContato.Mascarar(emailNormalizado);
        _logger.LogInformation("LoginAppleCadastroPendente {TrackId} {EmailMascarado}", trackId, emailMascarado);
    }

    private IActionResult Rejeitar(string trackId, string contatoBruto, Exception ex)
    {
        var contatoMascarado = MascaradorDeContato.Mascarar(contatoBruto ?? string.Empty);
        _logger.LogWarning("LoginRejeitado {TrackId} {Motivo} {ContatoMascarado}", trackId, ex.GetType().Name, contatoMascarado);
        return BadRequest(new AutenticacaoErrorResponse(ex.Message));
    }

    private IActionResult RejeitarLoginGoogle(string trackId, LoginRejeitadoException ex)
    {
        // Contato mascarado no log de rejeição (mesmo padrão de Rejeitar):
        // e-mail normalizado quando a exceção carrega um (e-mail não
        // verificado), vazio para token inválido (sem contato conhecido).
        var contatoMascarado = MascaradorDeContato.Mascarar(
            ex is EmailGoogleNaoVerificadoException emailNaoVerificado
                ? emailNaoVerificado.EmailNormalizado
                : string.Empty);
        _logger.LogWarning("LoginGoogleRejeitado {TrackId} {Motivo} {ContatoMascarado}", trackId, ex.GetType().Name, contatoMascarado);
        return BadRequest(new AutenticacaoErrorResponse(ex.Message));
    }

    private IActionResult RejeitarLoginApple(string trackId, LoginRejeitadoException ex)
    {
        // Contato mascarado no log de rejeição (mesmo padrão de Rejeitar):
        // e-mail normalizado quando a exceção carrega um (e-mail não
        // verificado), vazio para token inválido (sem contato conhecido).
        var contatoMascarado = MascaradorDeContato.Mascarar(
            ex is EmailAppleNaoVerificadoException emailNaoVerificado
                ? emailNaoVerificado.EmailNormalizado
                : string.Empty);
        _logger.LogWarning("LoginAppleRejeitado {TrackId} {Motivo} {ContatoMascarado}", trackId, ex.GetType().Name, contatoMascarado);
        return BadRequest(new AutenticacaoErrorResponse(ex.Message));
    }
}

public sealed record SolicitarCodigoRequest(string Contato);

public sealed record SolicitarCodigoResponse(bool Enviado);

public sealed record ConfirmarCodigoRequest(string Contato, string Codigo);

public sealed record ConfirmarCodigoResponse(string Token, Guid UsuarioId, string Nome, string[] Papeis);

public sealed record LoginGoogleRequest(string IdToken);

public sealed record LoginGoogleResponse(
    string? Token,
    Guid? UsuarioId,
    string? Nome,
    string[]? Papeis,
    bool CadastroPendente,
    string? Email);

public sealed record LoginAppleRequest(string IdToken);

public sealed record LoginAppleResponse(
    string? Token,
    Guid? UsuarioId,
    string? Nome,
    string[]? Papeis,
    bool CadastroPendente,
    string? Email);

public sealed record AutenticacaoErrorResponse(string Mensagem);
