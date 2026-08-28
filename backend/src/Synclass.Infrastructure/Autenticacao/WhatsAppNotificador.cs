using Microsoft.Extensions.Logging;
using Synclass.Domain.Autenticacao;
using Synclass.Domain.Usuarios;

namespace Synclass.Infrastructure.Autenticacao;

/// <summary>
/// Implementação de <see cref="INotificador"/> para o canal WhatsApp (issue
/// #193): envia o código OTP ao provedor (hoje Twilio, via
/// <see cref="IWhatsAppHttpClient"/>) e loga o evento de envio com o contato
/// mascarado — nunca o código em texto puro. Registrado por padrão em
/// <c>Program.cs</c>; <c>NotificadorDeLog</c> (que loga o código) só quando
/// <c>AssinaturaDigital:ModoDev=true</c>.
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
        if (Contato.IdentificarTipo(contatoNormalizado) != TipoContato.Telefone)
        {
            // Contato válido (ex: e-mail), mas o único canal implementado até
            // aqui é WhatsApp (issue #193) — OtpEnvioException (502), não
            // ContatoInvalidoException (400): o contato em si está correto,
            // só não há canal de envio disponível para o tipo dele
            // (dev-review do PR #206: sem essa distinção, TelefoneUtils
            // lançava ContatoInvalidoException e o usuário via "contato
            // inválido" para um contato que na verdade era válido).
            throw new OtpEnvioException("Login por WhatsApp não está disponível para contas cadastradas com e-mail.");
        }

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
            throw; // preserva stack trace original — capturado pelo controller (502)
        }
    }
}
