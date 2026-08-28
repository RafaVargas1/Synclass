using Microsoft.Extensions.Logging;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Captura as entradas de log estruturado gravadas por um
/// <c>ILogger&lt;T&gt;</c> (issue #193): guarda a linha formatada (nome do
/// evento de negócio como prefixo, ex: <c>OtpEnviado</c>) e os argumentos
/// estruturados, permitindo ao teste afirmar que o código OTP não aparece em
/// texto puro.
/// </summary>
public sealed class FakeLogger<T> : ILogger<T>
{
    public List<(LogLevel Nivel, string Linha, object?[] Argumentos)> Eventos { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // A linha formatada substitui os placeholders do template pelos
        // valores ("OtpEnviado +55****4321") — preserva o nome do evento e o
        // conteúdo resolvido, e nos permite afirmar que o código não foi
        // gravado.
        var linha = formatter(state, exception);

        var argumentos = state is IEnumerable<KeyValuePair<string, object?>> pares
            ? pares.Where(p => p.Key != "{OriginalFormat}").Select(p => p.Value).ToArray()
            : Array.Empty<object?>();

        Eventos.Add((logLevel, linha, argumentos));
    }
}
