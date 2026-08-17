using Synclass.Domain.Common;

namespace Synclass.Api.Tests.Fakes;

/// <summary>
/// Relógio com <see cref="UtcNow"/> ajustável em tempo de teste — permite a
/// <see cref="ConvitesEndpointTests"/> simular a passagem do tempo entre a
/// geração e o aceite de um convite (ex: expiração) sem depender do relógio
/// real da máquina.
/// </summary>
public sealed class AdvanceableClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
}
