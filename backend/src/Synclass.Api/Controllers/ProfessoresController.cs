using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Usuarios;

namespace Synclass.Api.Controllers;

[ApiController]
[Route("professores")]
public sealed class ProfessoresController : ControllerBase
{
    private readonly CadastroProfessorService _cadastroProfessor;
    private readonly IUsuarioRepository _usuarios;
    private readonly ILogger<ProfessoresController> _logger;

    public ProfessoresController(
        CadastroProfessorService cadastroProfessor, IUsuarioRepository usuarios, ILogger<ProfessoresController> logger)
    {
        _cadastroProfessor = cadastroProfessor;
        _usuarios = usuarios;
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
        Usuario? usuarioExistente;
        try
        {
            var contatoNormalizado = Contato.Normalizar(contato ?? string.Empty);
            usuarioExistente = await _usuarios.BuscarPorContatoAsync(contatoNormalizado, cancellationToken);
        }
        catch (ContatoInvalidoException)
        {
            usuarioExistente = null;
        }

        return Ok(usuarioExistente is null
            ? new VerificarContatoResponse(false, null)
            : new VerificarContatoResponse(true, usuarioExistente.Nome));
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

public sealed record VerificarContatoResponse(bool IdentidadeExistente, string? Nome);
