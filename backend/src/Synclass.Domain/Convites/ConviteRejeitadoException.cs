namespace Synclass.Domain.Convites;

/// <summary>
/// Base para toda rejeição de geração ou aceite de convite (expirado,
/// inválido, contato divergente, Aluno já vinculado, matrícula de origem
/// inválida). Permite à Api tratar qualquer rejeição de forma uniforme
/// (HTTP 400 + log de <c>ConviteRejeitado</c>) sem conhecer cada subtipo
/// individualmente — mesmo padrão de
/// <c>Synclass.Domain.Usuarios.CadastroRejeitadoException</c>.
/// </summary>
public abstract class ConviteRejeitadoException : Exception
{
    protected ConviteRejeitadoException(string message)
        : base(message)
    {
    }
}
