namespace Synclass.Domain.Usuarios;

/// <summary>
/// Base para toda rejeição de cadastro de usuário (contato inválido, nome
/// inválido ou papel já atribuído) — cadastro de Professor (issue #1) e de
/// Aluno (issue #61) reaproveitam a mesma hierarquia, já que ambos passam
/// pelo mesmo <see cref="CadastroUsuarioService"/> parametrizado por
/// <see cref="PapelUsuario"/>. Permite à Api tratar qualquer rejeição de
/// forma uniforme (HTTP 400 + log de <c>CadastroRejeitado</c>) sem conhecer
/// cada subtipo individualmente.
/// </summary>
public abstract class CadastroRejeitadoException : Exception
{
    protected CadastroRejeitadoException(string message)
        : base(message)
    {
    }

    protected CadastroRejeitadoException(string message, Exception causaRaiz)
        : base(message, causaRaiz)
    {
    }
}
