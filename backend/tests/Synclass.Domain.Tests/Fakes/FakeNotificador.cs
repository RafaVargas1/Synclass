using Synclass.Domain.Autenticacao;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Substitui o envio real de código (WhatsApp/SMS/e-mail, fora de escopo da
/// issue #18) em testes de unidade: só registra o que teria sido enviado.
/// </summary>
public sealed class FakeNotificador : INotificador
{
    public List<(string Contato, string Codigo)> CodigosEnviados { get; } = new();

    public Task EnviarCodigoOtpAsync(string contatoNormalizado, string codigo, CancellationToken cancellationToken)
    {
        CodigosEnviados.Add((contatoNormalizado, codigo));
        return Task.CompletedTask;
    }
}
