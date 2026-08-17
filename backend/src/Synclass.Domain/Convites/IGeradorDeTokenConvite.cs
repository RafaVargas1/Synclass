namespace Synclass.Domain.Convites;

/// <summary>
/// Gera o token de alta entropia usado no link público de aceite do
/// convite. Envolve a fonte de aleatoriedade real (Synclass.Infrastructure)
/// atrás de uma interface fina, conforme docs/spec/code-style.md#dependências
/// — mesmo racional de <c>IGeradorDeCodigoOtp</c> (issue #18), mas com muito
/// mais entropia: o token vira parte de uma URL pública, não um código
/// digitado.
/// </summary>
public interface IGeradorDeTokenConvite
{
    string Gerar();
}
