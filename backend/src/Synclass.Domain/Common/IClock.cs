namespace Synclass.Domain.Common;

/// <summary>
/// Abstrai a hora atual do sistema (envolve <c>DateTimeOffset.UtcNow</c>),
/// seguindo docs/spec/code-style.md#dependências, para permitir controlar o
/// tempo em testes de unidade sem depender do relógio real da máquina.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
