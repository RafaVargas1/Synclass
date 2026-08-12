using Synclass.Domain.Autenticacao;

namespace Synclass.Api.Tests.Fakes;

/// <summary>
/// Substitui <see cref="GeradorDeCodigoOtp"/> nos testes de fumaça: um
/// código aleatório real não seria conhecido pelo teste para confirmar o
/// login, então o pipeline de testes injeta um valor fixo (ver
/// docs/spec/code-style.md#testes — mocke I/O externo/não-determinístico com
/// classes fake nomeadas).
/// </summary>
public sealed class CodigoFixoGeradorDeCodigoOtp : IGeradorDeCodigoOtp
{
    public const string Codigo = "123456";

    public string Gerar()
    {
        return Codigo;
    }
}
