using Synclass.Domain.Common;

namespace Synclass.Infrastructure.Common;

/// <summary>
/// Implementação real de <see cref="IClock"/>, envolvendo
/// <c>DateTimeOffset.UtcNow</c> (ver docs/spec/code-style.md#dependências).
/// </summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
