namespace Synclass.Domain.CodigosEntradaTurma;

/// <summary>
/// Lançada quando <see cref="CodigoEntradaTurmaService"/> não consegue achar
/// um código livre entre os códigos ativos após o limite de tentativas —
/// teto de segurança contra loop indefinido em caso de saturação do espaço
/// de códigos, mesmo racional de
/// <c>Synclass.Domain.Convites.LimiteDeTentativasDeCodigoConviteExcedidoException</c>.
/// Não é uma rejeição de entrada do usuário: é uma falha de capacidade do
/// sistema, tratada como erro 500 pela Api.
/// </summary>
public sealed class LimiteDeTentativasDeCodigoEntradaExcedidoException : Exception
{
    public LimiteDeTentativasDeCodigoEntradaExcedidoException(int limite)
        : base($"Não foi possível gerar um código de entrada único após {limite} tentativas. Tente novamente.")
    {
    }
}
