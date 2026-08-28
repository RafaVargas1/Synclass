using Microsoft.Extensions.Logging;
using Synclass.Domain.Autenticacao;

namespace Synclass.Infrastructure.Autenticacao;

/// <summary>
/// Implementação de <see cref="INotificador"/> que loga o código em vez de
/// integrar WhatsApp/SMS/e-mail real (issue #18). A partir da issue #193 este
/// registro depende só de <c>AssinaturaDigital:ModoDev=true</c> em
/// <c>Program.cs</c> (DI condicional) — em produção o <c>WhatsAppNotificador</c>
/// é o registrado, nunca este. Evento próprio
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
