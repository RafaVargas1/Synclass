using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Configuracoes;

namespace Synclass.Api.Controllers;

[Authorize(Roles = "Professor")]
[ApiController]
[Route("professores/{professorId:guid}/configuracao")]
public sealed class ConfiguracoesController : ControllerBase
{
    private readonly ConfiguracaoProfessorService _configuracaoService;
    private readonly IConfiguracaoProfessorRepository _configuracoes;
    private readonly ILogger<ConfiguracoesController> _logger;

    public ConfiguracoesController(
        ConfiguracaoProfessorService configuracaoService,
        IConfiguracaoProfessorRepository configuracoes,
        ILogger<ConfiguracoesController> logger)
    {
        _configuracaoService = configuracaoService;
        _configuracoes = configuracoes;
        _logger = logger;
    }

    [HttpPut("modelo-agendamento")]
    public async Task<IActionResult> DefinirModeloAgendamento(
        Guid professorId, [FromBody] DefinirModeloAgendamentoRequest request, CancellationToken cancellationToken)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        var modeloAnterior = await _configuracoes.BuscarPorProfessorAsync(professorId, cancellationToken);

        try
        {
            var configuracao = await _configuracaoService.DefinirModeloAsync(
                professorId, (ModeloAgendamento)request.ModeloAgendamento, cancellationToken);

            LogModeloAgendamentoDefinido(trackId, professorId, modeloAnterior?.ModeloAgendamento, configuracao.ModeloAgendamento);
            return Ok(ParaResponse(configuracao));
        }
        catch (ModeloAgendamentoInvalidoException ex)
        {
            return BadRequest(new ConfiguracaoErrorResponse(ex.Message));
        }
    }

    [HttpGet]
    public async Task<IActionResult> ConsultarConfiguracao(Guid professorId, CancellationToken cancellationToken)
    {
        var configuracao = await _configuracoes.BuscarPorProfessorAsync(professorId, cancellationToken);
        if (configuracao is null)
        {
            return NotFound();
        }

        return Ok(ParaResponse(configuracao));
    }

    private static ConfiguracaoResponse ParaResponse(ConfiguracaoProfessor configuracao)
    {
        return new ConfiguracaoResponse((int)configuracao.ModeloAgendamento);
    }

    private void LogModeloAgendamentoDefinido(
        string trackId, Guid professorId, ModeloAgendamento? modeloAnterior, ModeloAgendamento modeloNovo)
    {
        _logger.LogInformation(
            "ModeloAgendamentoDefinido {TrackId} {ProfessorId} {ModeloAnterior} {ModeloNovo}",
            trackId, professorId, modeloAnterior, modeloNovo);
    }
}

public sealed record DefinirModeloAgendamentoRequest(int ModeloAgendamento);

public sealed record ConfiguracaoResponse(int ModeloAgendamento);

public sealed record ConfiguracaoErrorResponse(string Mensagem);
