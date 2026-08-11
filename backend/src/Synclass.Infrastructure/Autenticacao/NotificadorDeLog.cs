using Microsoft.Extensions.Logging;
using Synclass.Domain.Autenticacao;

namespace Synclass.Infrastructure.Autenticacao;

/// <summary>
/// Implementação inicial de <see cref="INotificador"/>: loga o código em vez
/// de integrar WhatsApp/SMS/e-mail real, conforme autorizado explicitamente
/// pelos Critérios técnicos da issue #18. Evento próprio
/// (<c>OtpEnviadoParaDesenvolvimento</c>), deliberadamente separado do
/// evento de negócio <c>CodigoOtpSolicitado</c> emitido pela Api — aquele
/// nunca inclui o código, este é o "canal de envio" substituto de dev.
/// </summary>
public sealed class NotificadorDeLog : INotificador
{
    private readonly ILogger<NotificadorDeLog> _logger;

    public NotificadorDeLog(ILogger<NotificadorDeLog> logger)
    {
        _logger = logger;
    }

    public Task EnviarCodigoOtpAsync(string contatoNormalizado, string codigo, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "OtpEnviadoParaDesenvolvimento {Contato} {Codigo}",
            contatoNormalizado, codigo);
        return Task.CompletedTask;
    }
}
