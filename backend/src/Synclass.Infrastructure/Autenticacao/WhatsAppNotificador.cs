using Microsoft.Extensions.Logging;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.Usuarios;

namespace Synclass.Infrastructure.Autenticacao;

/// <summary>
/// Implementação real de <see cref="INotificador"/> (issue #193): normaliza
/// o contato para E.164 e envia o código via <see cref="IWhatsAppHttpClient"/>
/// (provedor Twilio por padrão). Eventos de log nunca incluem o código OTP em
/// texto puro — sempre <see cref="MascaradorDeContato"/> e <see cref="OtpEnvioException.Motivo"/>.
/// Sem <c>TrackId</c> explícito por rodar na camada de Infrastructure, sem
/// acesso ao <c>HttpContext</c>; correlação por contato mascarado + timestamp
/// (ver docs/specs/193-login-whatsapp-real/implementation.md).
/// </summary>
public sealed class WhatsAppNotificador : INotificador
{
    private readonly IWhatsAppHttpClient _httpClient;
    private readonly ILogger<WhatsAppNotificador> _logger;

    public WhatsAppNotificador(IWhatsAppHttpClient httpClient, ILogger<WhatsAppNotificador> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task EnviarCodigoOtpAsync(string contatoNormalizado, string codigo, CancellationToken cancellationToken)
    {
        var numeroE164 = TelefoneUtils.NormalizarParaE164(contatoNormalizado);
        var mensagem = $"Seu código de acesso ao Synclass é: {codigo}. Ele expira em 10 minutos.";
        var contatoMascarado = MascaradorDeContato.Mascarar(numeroE164);

        try
        {
            await _httpClient.EnviarMensagemAsync(numeroE164, mensagem, cancellationToken);
            _logger.LogInformation("OtpEnviado {ContatoMascarado}", contatoMascarado);
        }
        catch (OtpEnvioException ex)
        {
            _logger.LogError("OtpEnvioFalhou {ContatoMascarado} {Motivo}", contatoMascarado, ex.Motivo);
            throw; // preserva a stack trace original — capturado pelo controller (502)
        }
    }
}
