using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Synclass.Api.Middleware;
using Synclass.Domain.Pagamentos;

namespace Synclass.Api.Controllers;

/// <summary>
/// Iniciação de pagamento do valor devido de uma Matrícula do Aluno
/// autenticado (issue #199): <c>POST
/// /alunos/matriculas/{matriculaId}/pagamentos</c>. O <c>alunoUsuarioId</c>
/// vem do token da sessão (<see cref="ClaimsPrincipalExtensions.GetUsuarioId"/>),
/// nunca de parâmetro de rota — não existe "iniciar pagamento de outro
/// Aluno" a proteger. As exceções de domínio de
/// <see cref="PagamentoService.IniciarAsync"/> são mapeadas aqui pra 404
/// (Matrícula de outro Aluno, mesmo padrão de
/// <see cref="MarcacoesHorarioController"/>: não distingue "não é sua" de
/// "não existe" pra não vazar existência do recurso) e 400 (sem valor
/// devido; Professor sem conta conectada), com <c>tipo</c> distintos por
/// causa (ver implementation.md#contrato-de-api).
/// </summary>
[Authorize(Roles = "Aluno")]
[ApiController]
[Route("alunos/matriculas/{matriculaId:guid}/pagamentos")]
public sealed class PagamentosController : ControllerBase
{
    private readonly PagamentoService _pagamentoService;
    private readonly ILogger<PagamentosController> _logger;

    public PagamentosController(PagamentoService pagamentoService, ILogger<PagamentosController> logger)
    {
        _pagamentoService = pagamentoService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Iniciar(Guid matriculaId, [FromBody] IniciarPagamentoRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var alunoUsuarioId = User.GetUsuarioId();
            var resultado = await _pagamentoService.IniciarAsync(
                matriculaId, alunoUsuarioId, request.Inicio, request.FimExclusivo, cancellationToken);
            LogarPagamentoIniciado(alunoUsuarioId, matriculaId, request.Inicio, request.FimExclusivo, resultado);
            return Created(string.Empty, new PagamentoIniciadoResponse(
                resultado.PagamentoId, resultado.UrlCheckout, resultado.Valor));
        }
        catch (MatriculaNaoPertenceAoAlunoException)
        {
            return NotFound();
        }
        catch (ProfessorSemContaConectadaException ex)
        {
            return BadRequest(new PagamentoErrorResponse("professor-sem-conta-conectada", ex.Message));
        }
        catch (SemValorDevidoException ex)
        {
            return BadRequest(new PagamentoErrorResponse("sem-valor-devido", ex.Message));
        }
    }

    /// <summary>
    /// Log estruturado do evento <c>PagamentoIniciado</c> (issue #199), com o
    /// <c>TrackId</c> do middleware (architecture.md) e os campos do
    /// implementation.md#logs-estruturados — nunca dado de cartão (o Checkout
    /// Pro é redirecionamento hospedado, o Synclass nunca vê cartão) nem
    /// payload bruto do MP. <c>PagamentoConfirmado</c>/<c>PagamentoFalhou</c>
    /// ficam para #200.
    /// </summary>
    private void LogarPagamentoIniciado(
        Guid alunoUsuarioId, Guid matriculaId, DateOnly inicio, DateOnly fimExclusivo, ResultadoInicioPagamento resultado)
    {
        var trackId = Response.Headers[TrackIdMiddleware.HeaderName].ToString();
        _logger.LogInformation(
            "PagamentoIniciado {TrackId} {PagamentoId} {MatriculaId} {ProfessorId} {AlunoUsuarioId} {Valor} {PeriodoInicio} {PeriodoFimExclusivo} {ReferenciaExterna}",
            trackId, resultado.PagamentoId, matriculaId, resultado.ProfessorId, alunoUsuarioId,
            resultado.Valor, inicio, fimExclusivo, resultado.ReferenciaExterna);
    }
}

/// <summary>
/// Request de iniciação de pagamento (issue #199) — mesmo shape de período
/// de <c>GET /alunos/valor-devido</c>: <c>inicio</c> e <c>fimExclusivo</c>
/// obrigatórios, validados pelo <see cref="PagamentoService"/> como
/// <see cref="Cobrancas.PeriodoConsulta"/> (<c>inicio &lt; fim</c>).
/// </summary>
public sealed record IniciarPagamentoRequest(DateOnly Inicio, DateOnly FimExclusivo);

/// <summary>
/// Resposta 201 de <c>POST /alunos/matriculas/{matriculaId}/pagamentos</c>:
/// id do pagamento criado/reaproveitado, a URL do checkout pra onde o Aluno
/// navega e o valor congelado na criação.
/// </summary>
public sealed record PagamentoIniciadoResponse(Guid PagamentoId, string UrlCheckout, decimal Valor);

/// <summary>
/// Resposta de erro de <c>POST /alunos/matriculas/{matriculaId}/pagamentos</c>:
/// <c>tipo</c> distingue "sem-valor-devido" de "professor-sem-conta-conectada"
/// (ver implementation.md#contrato-de-api).
/// </summary>
public sealed record PagamentoErrorResponse(string Tipo, string Mensagem);
