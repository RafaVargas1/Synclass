namespace Synclass.Domain.Convites;

/// <summary>
/// Lançada quando <see cref="ConviteService"/> não consegue achar um código
/// de convite livre entre os convites ativos após
/// <see cref="ConviteService.LimiteDeTentativasDeCodigo"/> tentativas — teto
/// de segurança contra loop indefinido em caso de saturação do espaço de
/// códigos (edge point de docs/specs/62-codigo-convite-curto/implementation.md).
/// Não é uma rejeição de entrada do usuário (não estende
/// <see cref="ConviteRejeitadoException"/>): é uma falha de capacidade do
/// sistema, tratada como erro 500 pela Api.
/// </summary>
public sealed class LimiteDeTentativasDeCodigoConviteExcedidoException : Exception
{
    public LimiteDeTentativasDeCodigoConviteExcedidoException(int limite)
        : base($"Não foi possível gerar um código de convite único após {limite} tentativas. Tente novamente.")
    {
    }
}
