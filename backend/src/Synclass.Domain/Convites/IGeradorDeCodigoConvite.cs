namespace Synclass.Domain.Convites;

/// <summary>
/// Gera o código curto de 5 dígitos numéricos usado como alternativa ao
/// link de aceite (issue #62): o Aluno pode digitar o código em vez de
/// abrir a URL. Envolve a fonte de aleatoriedade real (Synclass.Infrastructure)
/// atrás de uma interface fina, mesmo racional de
/// <see cref="IGeradorDeCodigoOtp"/> — mas é uma interface própria porque a
/// regra de unicidade (contra convites ativos) e o consumidor
/// (<see cref="ConviteService"/>) são diferentes de
/// <c>IGeradorDeCodigoOtp</c>.
/// </summary>
public interface IGeradorDeCodigoConvite
{
    string Gerar();
}
