using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Convites;
using Synclass.Domain.Matriculas;
using Synclass.Domain.Usuarios;

namespace Synclass.Api.Controllers;

/// <summary>
/// Convite de Aluno via WhatsApp (issue #2): gerar o link de convite e
/// aceitá-lo. O <c>professorId</c> é recebido na rota, não de uma sessão —
/// mesma decisão e mesma justificativa de <see cref="AlunosProvisoriosController"/>
/// (ver docs/specs/2-convite-whatsapp/implementation.md).
/// </summary>
[ApiController]
public sealed class ConvitesController : ControllerBase
{
    private readonly ConviteService _convites;
    private readonly ILogger<ConvitesController> _logger;

    public ConvitesController(ConviteService convites, ILogger<ConvitesController> logger)
    {
        _convites = convites;
        _logger = logger;
    }

    [HttpPost("professores/{professorId:guid}/convites")]
    public async Task<IActionResult> Gerar(Guid professorId, [FromBody] GerarConviteRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();

        try
        {
            var convite = await _convites.GerarAsync(professorId, request.Contato, request.MatriculaId, cancellationToken);
            LogConviteGerado(trackId, convite);
            return Ok(new GerarConviteResponse(convite.Id, convite.Token, convite.ExpiraEm));
        }
        catch (ProfessorNaoEncontradoException ex)
        {
            return RejeitarProfessorNaoEncontrado(trackId, professorId, ex);
        }
        catch (Exception ex) when (ex is ConviteRejeitadoException or ContatoInvalidoException)
        {
            return Rejeitar(trackId, ex);
        }
    }

    [HttpPost("convites/{token}/aceite")]
    public async Task<IActionResult> Aceitar(string token, [FromBody] AceitarConviteRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();

        try
        {
            var resultado = await _convites.AceitarAsync(token, request.Nome, request.Contato, cancellationToken);
            LogConviteAceito(trackId, resultado);
            return Ok(ParaResponse(resultado.Usuario));
        }
        catch (Exception ex) when (ex is ConviteRejeitadoException or ContatoInvalidoException or NomeInvalidoException)
        {
            return Rejeitar(trackId, ex);
        }
    }

    private static AceitarConviteResponse ParaResponse(Usuario usuario)
    {
        var papeis = usuario.Papeis.Select(p => p.Papel.ToString()).ToArray();
        return new AceitarConviteResponse(usuario.Id, usuario.Nome, papeis);
    }

    /// <summary>
    /// Inclui <c>ExpiraEm</c> e o contato mascarado (nunca em texto puro em
    /// log) — Critérios técnicos da issue #2.
    /// </summary>
    private void LogConviteGerado(string trackId, Convite convite)
    {
        var contatoMascarado = MascaradorDeContato.Mascarar(convite.Contato);
        _logger.LogInformation(
            "ConviteGerado {TrackId} {ProfessorId} {ExpiraEm} {ContatoMascarado}",
            trackId, convite.ProfessorId, convite.ExpiraEm, contatoMascarado);
    }

    /// <summary>
    /// Indica, via <see cref="ResultadoAceiteConvite.MatriculaPromovida"/>,
    /// se o aceite promoveu uma matrícula já existente (origem específica ou
    /// vínculo prévio) ou criou uma nova — Critérios técnicos da issue #2.
    /// Também loga <c>PapelAdicionado</c> quando o papel Aluno foi de fato
    /// anexado a uma identidade já existente (issue #4).
    /// </summary>
    private void LogConviteAceito(string trackId, ResultadoAceiteConvite resultado)
    {
        _logger.LogInformation(
            "ConviteAceito {TrackId} {ConviteId} {UsuarioId} {MatriculaPromovida}",
            trackId, resultado.ConviteId, resultado.Usuario.Id, resultado.MatriculaPromovida);

        if (resultado.PapelAdicionado)
        {
            _logger.LogInformation(
                "PapelAdicionado {TrackId} {UsuarioId} {Papel}",
                trackId, resultado.Usuario.Id, PapelUsuario.Aluno);
        }
    }

    private IActionResult RejeitarProfessorNaoEncontrado(string trackId, Guid professorId, ProfessorNaoEncontradoException ex)
    {
        _logger.LogWarning("ConviteRejeitado {TrackId} {ProfessorId} {Motivo}", trackId, professorId, ex.GetType().Name);
        return NotFound(new ConviteErrorResponse(ex.Message));
    }

    private IActionResult Rejeitar(string trackId, Exception ex)
    {
        _logger.LogWarning("ConviteRejeitado {TrackId} {Motivo}", trackId, ex.GetType().Name);
        return BadRequest(new ConviteErrorResponse(ex.Message));
    }
}

public sealed record GerarConviteRequest(string Contato, Guid? MatriculaId);

public sealed record GerarConviteResponse(Guid ConviteId, string Token, DateTimeOffset ExpiraEm);

public sealed record AceitarConviteRequest(string Nome, string Contato);

public sealed record AceitarConviteResponse(Guid UsuarioId, string Nome, string[] Papeis);

public sealed record ConviteErrorResponse(string Mensagem);
