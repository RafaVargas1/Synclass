namespace Synclass.Domain.Matriculas;

/// <summary>
/// Base para toda rejeição de cadastro/promoção de Aluno provisório (nome
/// inválido, identificador inválido/duplicado, professor inexistente, ou
/// promoção inválida). Permite à Api tratar qualquer rejeição sem conhecer
/// cada subtipo individualmente para fins de log
/// (<c>CadastroAlunoProvisorioRejeitado</c>); o HTTP mapeado varia por
/// subtipo — ver <see cref="ProfessorNaoEncontradoException"/> (404) vs. as
/// demais (400) — mesmo papel de
/// <c>Synclass.Domain.Usuarios.CadastroRejeitadoException</c> na
/// issue #1.
/// </summary>
public abstract class MatriculaRejeitadaException : Exception
{
    protected MatriculaRejeitadaException(string message)
        : base(message)
    {
    }

    protected MatriculaRejeitadaException(string message, Exception causaRaiz)
        : base(message, causaRaiz)
    {
    }
}
