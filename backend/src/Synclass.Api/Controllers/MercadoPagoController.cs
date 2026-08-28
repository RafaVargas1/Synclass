using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Pagamentos;
using Synclass.Domain.Usuarios;
using Synclass.Infrastructure.Http;

namespace Synclass.Api.Controllers;

/// <summary>
/// Integração do Professor com o Mercado Pago (issue #203). O endpoint de
/// conexão deriva o <c>professorId</c> do token autenticado (não de um
/// parâmetro de rota) — ver implementation.md#decisão-de-design-autorização-e-vínculo-com-o-usuário-autenticado.
/// O callback não leva <c>[Authorize]</c> de propósito (redirect do
/// navegador do Professor, sem sessão do Synclass); segue o padrão thin de
/// <c>ConfiguracoesController</c>: orquestra o service e devolve o status
/// certo, sem regra de negócio aqui.
/// </summary>
[ApiController]
[Route("professores/mercado-pago")]
public sealed class MercadoPagoController : ControllerBase
{
    private readonly ConexaoMercadoPagoService _conexaoService;
    private readonly ILogger<MercadoPagoController> _logger;

    public MercadoPagoController(
        ConexaoMercadoPagoService conexaoService,
        ILogger<MercadoPagoController> logger)
    {
        _conexaoService = conexaoService;
        _logger = logger;
    }

    /// <summary>
    /// Inicia o fluxo OAuth do Professor logado e devolve a URL de
    /// autorização pra ele abrir no navegador. Nenhum <c>professorId</c> na
    /// rota: a identidade vem do token (<see
    /// cref="ClaimsPrincipalExtensions.GetUsuarioId"/>), eliminando a
    /// ambiguidade rota-vs-claim desta feature (ver
    /// implementation.md#decisão-de-design-autorização-e-vínculo-com-o-usuário-autenticado).
    /// </summary>
    [Authorize(Roles = "Professor")]
    [HttpGet("conectar")]
    public async Task<IActionResult> Conectar(CancellationToken cancellationToken)
    {
        var professorId = User.GetUsuarioId();
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();

        try
        {
            var url = await _conexaoService.ConectarAsync(professorId, cancellationToken);
            _logger.LogInformation(
                "FluxoMercadoPagoIniciado {TrackId} {ProfessorId}", trackId, professorId);
            return Ok(new MercadoPagoConectarResponse(url));
        }
        catch (UsuarioNaoEncontradoException)
        {
            return NotFound();
        }
        catch (MercadoPagoApiException ex)
        {
            _logger.LogError(ex, "Falha ao iniciar conexão com o Mercado Pago {TrackId} {ProfessorId}", trackId, professorId);
            return StatusCode(StatusCodes.Status502BadGateway);
        }
    }

    /// <summary>
    /// Recebe o redirect do Mercado Pago de volta pra cá após o Professor
    /// autorizar a conta. Não leva <c>[Authorize]</c> de propósito: o
    /// redirect vem do navegador do Professor, que pode não ter sessão do
    /// Synclass naquele momento — a prova de autenticidade é o
    /// <c>state</c> (valor aleatório validado pelo service antes de
    /// qualquer troca de <c>code</c>). Devolve uma página HTML mínima de
    /// confirmação (único endpoint de HTML do repo — renderização de página
    /// pública só existe aqui; ver implementation.md#resposta-do-callback).
    /// </summary>
    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string code, [FromQuery] string state, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();

        try
        {
            // O controller é quem loga o evento com os dados reais do retorno:
            // o Domain não injeta ILogger e o endpoint é anônimo (sem claim
            // pra obter ProfessorId aqui) — o service resolve o Professor
            // internamente pelo state e devolve ProfessorId/CollectorId (ver
            // implementation.md#decisão-de-design-logging-de-professorconectoumercadopago-sem-violar-camadas).
            var (professorId, collectorId) = await _conexaoService.ProcessarCallbackAsync(code, state, cancellationToken);
            _logger.LogInformation(
                "ProfessorConectouMercadoPago {TrackId} {ProfessorId} {CollectorId}",
                trackId, professorId, collectorId);
            return Content(PaginaContaConectada, "text/html");
        }
        catch (StateInvalidoException ex)
        {
            return BadRequest(new MercadoPagoErrorResponse(ex.Message));
        }
        catch (MercadoPagoApiException ex)
        {
            _logger.LogError(ex, "Falha ao trocar code por token no Mercado Pago {TrackId}", trackId);
            return StatusCode(StatusCodes.Status502BadGateway);
        }
    }

    private const string PaginaContaConectada = """
        <!DOCTYPE html>
        <html lang="pt-BR">
          <head><meta charset="utf-8"><title>Conta conectada</title></head>
          <body>
            <h1>Conta conectada</h1>
            <p>Sua conta do Mercado Pago foi conectada com sucesso. Você já pode fechar esta aba e voltar ao Synclass.</p>
          </body>
        </html>
        """;
}

public sealed record MercadoPagoConectarResponse(string Url);

public sealed record MercadoPagoErrorResponse(string Mensagem);
