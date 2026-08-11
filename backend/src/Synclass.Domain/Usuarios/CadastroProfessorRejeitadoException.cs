namespace Synclass.Domain.Usuarios;

/// <summary>
/// Base para toda rejeição de cadastro de Professor (contato inválido, nome
/// inválido ou papel já atribuído). Permite à Api tratar qualquer rejeição
/// de forma uniforme (HTTP 400 + log de <c>CadastroRejeitado</c>) sem
/// conhecer cada subtipo individualmente.
/// </summary>
public abstract class CadastroProfessorRejeitadoException : Exception
{
    protected CadastroProfessorRejeitadoException(string message)
        : base(message)
    {
    }

    protected CadastroProfessorRejeitadoException(string message, Exception causaRaiz)
        : base(message, causaRaiz)
    {
    }
}
