using Synclass.Domain.Common;

namespace Synclass.Domain.Tests.Fakes;

/// <summary>
/// Relógio fixo para testes de unidade: sempre devolve o instante informado
/// na construção, eliminando flakiness por tempo real decorrido.
/// </summary>
public sealed class FixedClock : IClock
{
    public FixedClock(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; }
}
