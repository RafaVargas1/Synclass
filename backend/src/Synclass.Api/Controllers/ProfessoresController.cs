using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Usuarios;

namespace Synclass.Api.Controllers;

[ApiController]
[Route("professores")]
public sealed class ProfessoresController : ControllerBase
{
    private readonly CadastroProfessorService _cadastroProfessor;
    private readonly ILogger<ProfessoresController> _logger;

    public ProfessoresController(CadastroProfessorService cadastroProfessor, ILogger<ProfessoresController> logger)
    {
        _cadastroProfessor = cadastroProfessor;
        _logger = logger;
    }

    [HttpPost("cadastro")]
    public async Task<IActionResult> Cadastrar([FromBody] CadastroProfessorRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();

        try
        {
            var resultado = await _cadastroProfessor.CadastrarProfessorAsync(request.Nome, request.Contato, cancellationToken);
            LogCadastroSucesso(trackId, resultado);
            return Ok(new CadastroProfessorResponse(resultado.Usuario.Id, resultado.Usuario.Nome));
        }
        catch (CadastroProfessorRejeitadoException ex)
        {
            return RejeitarCadastro(trackId, request.Contato, ex);
        }
    }

    private void LogCadastroSucesso(string trackId, ResultadoCadastroProfessor resultado)
    {
        if (resultado.UsuarioReaproveitado)
        {
            _logger.LogInformation(
                "PapelAdicionado {TrackId} {UsuarioId} {Papel}",
                trackId, resultado.Usuario.Id, PapelUsuario.Professor);
            return;
        }

        _logger.LogInformation(
            "UsuarioCadastrado {TrackId} {UsuarioId} {Papel}",
            trackId, resultado.Usuario.Id, PapelUsuario.Professor);
    }

    private IActionResult RejeitarCadastro(string trackId, string? contatoBruto, CadastroProfessorRejeitadoException ex)
    {
        var contatoMascarado = MascaradorDeContato.Mascarar(contatoBruto ?? string.Empty);
        _logger.LogWarning(
            "CadastroRejeitado {TrackId} {Motivo} {ContatoMascarado}",
            trackId, ex.GetType().Name, contatoMascarado);
        return BadRequest(new CadastroProfessorErrorResponse(ex.Message));
    }
}

public sealed record CadastroProfessorRequest(string Nome, string Contato);

public sealed record CadastroProfessorResponse(Guid UsuarioId, string Nome);

public sealed record CadastroProfessorErrorResponse(string Mensagem);
