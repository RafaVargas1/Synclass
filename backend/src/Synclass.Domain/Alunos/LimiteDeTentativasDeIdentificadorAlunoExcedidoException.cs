namespace Synclass.Domain.Alunos;

/// <summary>
/// Lançada quando <see cref="IdentificadorAlunoService"/> não consegue achar
/// um identificador de Aluno livre após o teto de tentativas — guardrail
/// contra loop indefinido caso o espaço de identificadores sature (issue
/// #70, mesmo padrão de
/// <see cref="Synclass.Domain.Convites.LimiteDeTentativasDeCodigoConviteExcedidoException"/>).
/// Não é uma rejeição de entrada do usuário: é uma falha de capacidade do
/// sistema, tratada como erro 500 pela Api.
/// </summary>
public sealed class LimiteDeTentativasDeIdentificadorAlunoExcedidoException : Exception
{
    public LimiteDeTentativasDeIdentificadorAlunoExcedidoException(int limite)
        : base($"Não foi possível gerar um identificador de Aluno único após {limite} tentativas. Tente novamente.")
    {
    }
}
